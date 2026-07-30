using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConstellationPlanner.Cli;
using ConstellationPlanner.Core;

namespace ConstellationPlanner.Gui;

/// <summary>Results-streaming dialog for the optimizer. Each trial result the optimizer
/// finishes appears as a new row in the listview; the progress bar advances. Sortable by any
/// column. User can cancel mid-run via the Cancel button; the in-flight trial is allowed to
/// finish but no new ones start.</summary>
public sealed class OptimizeResultsDialog : Form
{
    readonly ListView _results;
    readonly ProgressBar _progress;
    readonly Label _statusLabel = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(8, 4, 8, 4) };
    readonly Button _verifyBtn = new() { Text = "Verify selected (5000 samples)", Width = 220, Margin = new Padding(6), Enabled = false };
    readonly Button _cancelBtn = new() { Text = "Cancel", Width = 100, Margin = new Padding(6) };
    readonly Button _closeBtn = new() { Text = "Close", Width = 100, DialogResult = DialogResult.OK, Enabled = false, Margin = new Padding(6) };
    readonly CancellationTokenSource _cts = new();
    readonly List<OptimizeTrialResult> _allResults = new();
    // Cached so the Verify button can re-run with the same connection list and base settings.
    IReadOnlyList<(ConstellationPlanner.Core.SkoposConnection Conn, int RxIndex)>? _connections;

    /// <summary>Invoked when the user double-clicks a result row. Receives the row's config so
    /// the main form can apply it to its controls and re-render. Wired by the caller; null = no-op.</summary>
    public Action<PlannerInput>? OnApplyConfig { get; set; }

    /// <summary>Enables WS_EX_COMPOSITED so the form composites its child controls into one
    /// off-screen buffer before painting. Combined with ListView.DoubleBuffered (set below via
    /// reflection), this eliminates the per-row flicker as trial results stream in.</summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
            return cp;
        }
    }

    public OptimizeResultsDialog(int trialsPlanned)
    {
        Text = $"Optimizer — {trialsPlanned} trials";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(900, 600);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(700, 450);
        MinimizeBox = false;
        ShowInTaskbar = false;
        DoubleBuffered = true;

        _results = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Sorting = SortOrder.None,
            Font = new Font("Consolas", 9F),
        };
        // ListView.DoubleBuffered is protected — set via reflection. Without this the per-row
        // appends as trials stream in cause visible flicker even with the form composited.
        typeof(System.Windows.Forms.Control)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_results, true);
        _results.Columns.Add("#", 40);
        _results.Columns.Add("Phase", 70);
        _results.Columns.Add("Pass %", 70);
        _results.Columns.Add("Mean met-win %", 110);
        _results.Columns.Add("Mean uptime %", 110);
        _results.Columns.Add("Config", 440);
        _results.ColumnClick += OnColumnClick;

        _progress = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = Math.Max(1, trialsPlanned),
            Value = 0,
            Style = ProgressBarStyle.Continuous,
        };

        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            ColumnCount = 4,
            RowCount = 2,
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottom.Controls.Add(_progress, 0, 0);
        bottom.SetColumnSpan(_progress, 4);
        bottom.Controls.Add(_statusLabel, 0, 1);
        bottom.Controls.Add(_verifyBtn, 1, 1);
        bottom.Controls.Add(_cancelBtn, 2, 1);
        bottom.Controls.Add(_closeBtn, 3, 1);

        Controls.Add(_results);
        Controls.Add(bottom);

        _cancelBtn.Click += (s, e) =>
        {
            _cts.Cancel();
            _cancelBtn.Enabled = false;
            _statusLabel.Text = "Cancelling…";
        };
        _verifyBtn.Click += async (s, e) => await VerifySelectedAsync();
        _results.SelectedIndexChanged += (s, e) =>
            _verifyBtn.Enabled = _closeBtn.Enabled && _results.SelectedIndices.Count > 0;
        // Double-click any row → push that config to the main form and let it re-render.
        // Works during the run too — the main form just re-renders behind the modal dialog.
        _results.MouseDoubleClick += (s, e) =>
        {
            var hit = _results.HitTest(e.Location);
            if (hit.Item?.Tag is OptimizeTrialResult tr)
            {
                OnApplyConfig?.Invoke(tr.Config);
                _statusLabel.Text = $"Applied row '{hit.Item.SubItems[0].Text}' to main form.";
            }
        };
        FormClosing += (s, e) =>
        {
            if (!_cts.IsCancellationRequested) _cts.Cancel();
        };
    }

    /// <summary>Re-run the selected row's config at 5000 samples — full resolution, same as
    /// the animation's stats sweep — to verify whether the optimizer's lower-sample reading
    /// was accurate. Result appears as a new "verify" row in the listview so the user can
    /// compare side-by-side with the original.</summary>
    async Task VerifySelectedAsync()
    {
        if (_results.SelectedItems.Count == 0 || _connections == null) return;
        // Read the trial from the row's Tag, NOT from _allResults[index] — column sorting
        // reorders rows so the visible index doesn't match insertion order anymore.
        if (_results.SelectedItems[0].Tag is not OptimizeTrialResult seed) return;

        _verifyBtn.Enabled = false;
        string prevStatus = _statusLabel.Text;
        _statusLabel.Text = $"Verifying trial #{seed.TrialNumber} at 5000 samples…";
        try
        {
            // Pin the verification to the trial's exact config; no perturbation.
            bool isCircular = seed.Config.OrbitType == ConstellationPlanner.Cli.OrbitType.WalkerCircular
                            || seed.Config.OrbitType == ConstellationPlanner.Cli.OrbitType.WalkerStar;
            double ap = isCircular ? seed.Config.AltitudeKm : seed.Config.ApogeeAltitudeKm;
            var repeat = ConstellationPlanner.Cli.Planner.GroundTrackRepeat(seed.Config.AltitudeKm, ap);
            var connList = _connections as IList<(ConstellationPlanner.Core.SkoposConnection, int)>
                        ?? _connections.ToList();
            var results = await Task.Run(() =>
                ConstellationPlanner.Cli.Planner.EvaluateConnectionsOverCycle(
                    seed.Config, connList, repeat.CycleSec, 5000));
            int total = results.Count(r => r.FromIdx >= 0 && r.ToIdx >= 0);
            int metCount = results.Count(r => r.MetWindowPct >= 90.0);
            double meanMet = total > 0 ? results.Where(r => r.FromIdx >= 0 && r.ToIdx >= 0).Average(r => r.MetWindowPct) : 0;
            double meanUp  = total > 0 ? results.Where(r => r.FromIdx >= 0 && r.ToIdx >= 0).Average(r => r.UptimePct)    : 0;
            double passPct = total > 0 ? 100.0 * metCount / total : 0;
            double passDelta = passPct - seed.PassPct;

            var row = new ListViewItem($"V{seed.TrialNumber}");
            row.SubItems.Add("verify");
            row.SubItems.Add($"{passPct:F1}");
            row.SubItems.Add($"{meanMet:F2}");
            row.SubItems.Add($"{meanUp:F2}");
            row.SubItems.Add($"Δ pass = {passDelta:+0.0;-0.0;0.0}% vs trial #{seed.TrialNumber}");
            row.BackColor = Math.Abs(passDelta) < 1.0 ? Color.LightGreen
                          : Math.Abs(passDelta) < 3.0 ? Color.LightYellow
                          : Color.LightCoral;
            row.Tag = seed; // same source result; double-click applies its config, verify re-runs it
            _results.Items.Add(row);
            _results.EnsureVisible(_results.Items.Count - 1);
            _statusLabel.Text = $"Verified trial #{seed.TrialNumber}: 500-sample pass = {seed.PassPct:F1}%, 5000-sample pass = {passPct:F1}% (Δ {passDelta:+0.0;-0.0;0.0}%)";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Verify failed: {ex.GetType().Name}: {ex.Message}";
        }
        finally { _verifyBtn.Enabled = true; }
    }

    /// <summary>Kick off the optimizer and stream results into the listview. The task completes
    /// when all trials finish (or cancellation propagates). Caller should await this to know
    /// when the dialog can be closed.</summary>
    public async Task RunAsync(PlannerInput baseCfg,
                                IReadOnlyList<(SkoposConnection Conn, int RxIndex)> connections,
                                OptimizerSearchSpace space,
                                int trials,
                                int samplesPerTrial)
    {
        _connections = connections;
        _statusLabel.Text = $"Running… 0 / {trials}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await Optimizer.RunAsync(baseCfg, connections, space, trials, samplesPerTrial,
                OnTrialResult, _cts.Token);
            _statusLabel.Text = $"Done — {_allResults.Count} trials in {sw.Elapsed.TotalSeconds:F1} s · best pass {(_allResults.Count > 0 ? _allResults.Max(r => r.PassPct) : 0):F1}%";
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = $"Cancelled — {_allResults.Count} of {trials} trials completed in {sw.Elapsed.TotalSeconds:F1} s";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Failed: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _cancelBtn.Enabled = false;
            _closeBtn.Enabled = true;
            _verifyBtn.Enabled = _results.SelectedIndices.Count > 0;
            AcceptButton = _closeBtn;
        }
    }

    void OnTrialResult(OptimizeTrialResult r)
    {
        _allResults.Add(r);
        var row = new ListViewItem(r.TrialNumber.ToString());
        row.SubItems.Add(r.Phase == OptimizerPhase.Refinement ? "refine" : "explore");
        row.SubItems.Add($"{r.PassPct:F1}");
        row.SubItems.Add($"{r.MeanMetWindowPct:F2}");
        row.SubItems.Add($"{r.MeanUptimePct:F2}");
        row.SubItems.Add(FormatConfig(r.Config));
        // Tag holds the full trial result so both Verify and the double-click "apply" handler
        // read from the row itself — column-sorting reorders rows but the Tag travels with them.
        row.Tag = r;
        _results.Items.Add(row);
        _results.EnsureVisible(_results.Items.Count - 1);
        _progress.Value = Math.Min(_progress.Maximum, r.TrialNumber);
        _statusLabel.Text = $"Running… {r.TrialNumber} / {_progress.Maximum} · best pass so far {_allResults.Max(x => x.PassPct):F1}%";
    }

    static string FormatConfig(PlannerInput c)
    {
        string orbit = c.OrbitType switch
        {
            OrbitType.WalkerCircular => "Walker-Δ",
            OrbitType.WalkerStar     => "Walker-★",
            OrbitType.Molniya        => "Molniya",
            OrbitType.Tundra         => "Tundra",
            OrbitType.Custom         => "Custom",
            _                        => c.OrbitType.ToString(),
        };
        bool circular = c.OrbitType == OrbitType.WalkerCircular || c.OrbitType == OrbitType.WalkerStar;
        string altStr = circular
            ? $"alt={c.AltitudeKm:F0}"
            : $"Pe={c.AltitudeKm:F0}/Ap={c.ApogeeAltitudeKm:F0}";
        return $"{orbit} {c.T}/{c.P}/{c.F}  {altStr}  i={c.InclinationDeg:F1}°  LAN={c.LanOffsetDeg:F0}°  ω={c.ArgPerigeeDeg:F0}°  phase={c.PhaseOffsetDeg:F0}°";
    }

    // Click-to-sort on any column. Stores the sort direction per-column so re-clicking flips it.
    SortOrder _sortOrder = SortOrder.Descending;
    int _sortColumn = 2; // start with Pass% (col 2 after Phase moved to col 1)
    void OnColumnClick(object? sender, ColumnClickEventArgs e)
    {
        if (e.Column == _sortColumn)
            _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        else
        {
            _sortColumn = e.Column;
            _sortOrder = SortOrder.Descending;
        }
        _results.ListViewItemSorter = new TrialColumnSorter(_sortColumn, _sortOrder);
        _results.Sort();
    }

    sealed class TrialColumnSorter : System.Collections.IComparer
    {
        readonly int _col;
        readonly SortOrder _order;
        public TrialColumnSorter(int col, SortOrder order) { _col = col; _order = order; }
        public int Compare(object? x, object? y)
        {
            if (x is not ListViewItem a || y is not ListViewItem b) return 0;
            string sa = a.SubItems.Count > _col ? a.SubItems[_col].Text : "";
            string sb = b.SubItems.Count > _col ? b.SubItems[_col].Text : "";
            int cmp;
            // Numeric for # / Pass% / Mean met-win % / Mean uptime % (cols 0, 2, 3, 4);
            // text for Phase (col 1) and Config (col 5).
            bool numericCol = _col == 0 || _col == 2 || _col == 3 || _col == 4;
            if (numericCol && double.TryParse(sa, out double da) && double.TryParse(sb, out double db))
                cmp = da.CompareTo(db);
            else
                cmp = string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
            return _order == SortOrder.Ascending ? cmp : -cmp;
        }
    }
}
