using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConstellationPlanner.Cli;
using ConstellationPlanner.Core;

namespace ConstellationPlanner.Gui;

/// <summary>Search-space dialog: one row per optimizable parameter with min/max spinners (or
/// a checkbox set for orbit type). "Locked" parameters are just rows where min == max ==
/// current value. Returns the chosen <see cref="OptimizerSearchSpace"/> + trial / sample
/// budget to the caller.</summary>
public sealed class OptimizeParamsDialog : Form
{
    // Orbit type — multi-select via checkboxes. Compared to a CheckedListBox these are easier
    // to lay out inline with the rest of the rows.
    readonly CheckBox _orbWalkerCircular = new() { Text = "Walker-Δ", AutoSize = true, Margin = new Padding(0, 4, 12, 4) };
    readonly CheckBox _orbWalkerStar     = new() { Text = "Walker-★", AutoSize = true, Margin = new Padding(0, 4, 12, 4) };
    readonly CheckBox _orbMolniya        = new() { Text = "Molniya",  AutoSize = true, Margin = new Padding(0, 4, 12, 4) };
    readonly CheckBox _orbTundra         = new() { Text = "Tundra",   AutoSize = true, Margin = new Padding(0, 4, 12, 4) };
    readonly CheckBox _orbCustom         = new() { Text = "Custom",   AutoSize = true, Margin = new Padding(0, 4, 12, 4) };

    // Min/max spinner pairs for each numeric param. Made fields so the OK handler can read.
    readonly NumericUpDown _tMin, _tMax, _pMin, _pMax, _fMin, _fMax;
    readonly NumericUpDown _altMin, _altMax, _apMin, _apMax;
    readonly NumericUpDown _incMin, _incMax, _lanMin, _lanMax;
    readonly NumericUpDown _argPeMin, _argPeMax, _phaseMin, _phaseMax;

    readonly NumericUpDown _trials  = new() { Minimum = 1,  Maximum = 5000, Value = 100, Increment = 10,  DecimalPlaces = 0, Width = 90 };
    readonly NumericUpDown _samples = new() { Minimum = 60, Maximum = 5000, Value = 500, Increment = 100, DecimalPlaces = 0, Width = 90 };

    /// <summary>The cached baseline config — used by <see cref="LockAllToCurrent"/> to reset
    /// every spinner to the user's current main-GUI values.</summary>
    readonly PlannerInput _baseline;

    Button _okBtn = null!;
    public OptimizerSearchSpace SearchSpace { get; private set; } = new();
    public int Trials => (int)_trials.Value;
    public int SamplesPerTrial => (int)_samples.Value;

    public OptimizeParamsDialog(OptimizerSearchSpace? initial, PlannerInput baseline,
                                 int initialTrials = 100, int initialSamples = 500)
    {
        _baseline = baseline;
        _trials.Value  = Clamp(_trials, initialTrials);
        _samples.Value = Clamp(_samples, initialSamples);

        // If the user has never opened this dialog before, default to "every param locked to
        // baseline" — they explicitly choose which to widen. This is friendlier than blowing
        // open all 6 continuous params at first launch.
        var seed = initial ?? OptimizerSearchSpace.LockedToBaseline(baseline);

        // Hard bounds — what the spinner allows the user to type. Looser than the random-sample
        // operational ranges; the user can legitimately ask for very wide / very narrow scans.
        _tMin    = MakeIntSpinner(1, 200);  _tMax = MakeIntSpinner(1, 200);
        _pMin    = MakeIntSpinner(1, 200);  _pMax = MakeIntSpinner(1, 200);
        _fMin    = MakeIntSpinner(0, 200);  _fMax = MakeIntSpinner(0, 200);
        _altMin  = MakeDoubleSpinner(0, 100_000, 1, 100);  _altMax = MakeDoubleSpinner(0, 100_000, 1, 100);
        _apMin   = MakeDoubleSpinner(0, 200_000, 1, 100);  _apMax  = MakeDoubleSpinner(0, 200_000, 1, 100);
        _incMin  = MakeDoubleSpinner(0, 180,   1, 1);      _incMax = MakeDoubleSpinner(0, 180,   1, 1);
        _lanMin  = MakeDoubleSpinner(0, 360,   1, 5);      _lanMax = MakeDoubleSpinner(0, 360,   1, 5);
        _argPeMin = MakeDoubleSpinner(0, 360,  1, 5);      _argPeMax = MakeDoubleSpinner(0, 360, 1, 5);
        _phaseMin = MakeDoubleSpinner(0, 360,  1, 5);      _phaseMax = MakeDoubleSpinner(0, 360, 1, 5);

        // Seed spinners + checkboxes from the persisted search space.
        ApplyToControls(seed);

        Text = "Optimize — search space";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(650, 600);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(600, 520);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;

        BuildLayout();

        // Hook OK to populate SearchSpace; Cancel keeps the previous SearchSpace (empty default).
        _okBtn.Click += (s, e) => SearchSpace = ReadFromControls();
    }

    static decimal Clamp(NumericUpDown box, double value)
        => (decimal)Math.Max((double)box.Minimum, Math.Min((double)box.Maximum, value));

    static NumericUpDown MakeIntSpinner(int min, int max) => new()
    {
        Minimum = min, Maximum = max, DecimalPlaces = 0, Increment = 1, Width = 80,
    };
    static NumericUpDown MakeDoubleSpinner(double min, double max, int decimals, double inc) => new()
    {
        Minimum = (decimal)min, Maximum = (decimal)max,
        DecimalPlaces = decimals, Increment = (decimal)inc, Width = 90,
    };

    void BuildLayout()
    {
        // Outer grid: a single column with each row holding either a logical row of the form
        // (orbit type + integer params + continuous params) or the action buttons.
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            AutoSize = false,
            Padding = new Padding(10),
        };
        // Columns: label | min spinner | "to" | max spinner | unit
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  30));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // Orbit type row spans all 5 columns
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(MakeNameLabel("Orbit type:"), 0, row);
        // AutoSizeMode is mandatory on every AutoSize panel here: FlowLayoutPanel inherits
        // Panel's GrowOnly default, which floors it at Panel.DefaultSize (200x100) — the
        // AutoSize row would then be 100px tall regardless of the controls inside.
        var orbPanel = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        orbPanel.Controls.AddRange(new Control[] { _orbWalkerCircular, _orbWalkerStar, _orbMolniya, _orbTundra, _orbCustom });
        grid.Controls.Add(orbPanel, 1, row);
        grid.SetColumnSpan(orbPanel, 4);
        row++;

        // Visual separator + integer params (T, P, F)
        AddRow(grid, ref row, "T (sats):",     _tMin,   _tMax,   "");
        AddRow(grid, ref row, "P (planes):",   _pMin,   _pMax,   "");
        AddRow(grid, ref row, "F (phasing):",  _fMin,   _fMax,   "");

        // Separator spacer
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        var spacer = new Panel { Height = 8, Dock = DockStyle.Fill };
        grid.Controls.Add(spacer, 0, row);
        grid.SetColumnSpan(spacer, 5);
        row++;

        // Continuous params
        AddRow(grid, ref row, "Altitude/Pe:",  _altMin,  _altMax,  "km");
        AddRow(grid, ref row, "Apogee:",       _apMin,   _apMax,   "km");
        AddRow(grid, ref row, "Inclination:",  _incMin,  _incMax,  "°");
        AddRow(grid, ref row, "LAN offset:",   _lanMin,  _lanMax,  "°");
        AddRow(grid, ref row, "Arg. perigee:", _argPeMin, _argPeMax, "°");
        AddRow(grid, ref row, "Phase offset:", _phaseMin, _phaseMax, "°");

        // Quick-set buttons
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var quickPanel = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 0) };
        var btnLockAll = new Button { Text = "Lock all to current", AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        var btnOpenAll = new Button { Text = "Open all ranges",     AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        btnLockAll.Click += (s, e) => ApplyToControls(OptimizerSearchSpace.LockedToBaseline(_baseline));
        btnOpenAll.Click += (s, e) => ApplyToControls(WideOpenSpace());
        quickPanel.Controls.Add(btnLockAll);
        quickPanel.Controls.Add(btnOpenAll);
        grid.Controls.Add(quickPanel, 0, row);
        grid.SetColumnSpan(quickPanel, 5);
        row++;

        // Trial / samples row
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var budgetPanel = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 0) };
        budgetPanel.Controls.Add(new Label { Text = "Trials:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        budgetPanel.Controls.Add(_trials);
        budgetPanel.Controls.Add(new Label { Text = "Samples/trial:", AutoSize = true, Margin = new Padding(16, 8, 4, 0) });
        budgetPanel.Controls.Add(_samples);
        grid.Controls.Add(budgetPanel, 0, row);
        grid.SetColumnSpan(budgetPanel, 5);
        row++;

        // OK / Cancel
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        // Dock=Fill is deliberate here — RightToLeft flow needs the full row width to push the
        // buttons to the right edge. GrowAndShrink keeps the row from inheriting the 100px floor.
        var okPanel = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        _okBtn        = new Button { Text = "OK",     Width = 90, DialogResult = DialogResult.OK,     Margin = new Padding(6) };
        var cancelBtn = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel, Margin = new Padding(6) };
        okPanel.Controls.Add(_okBtn);
        okPanel.Controls.Add(cancelBtn);
        grid.Controls.Add(okPanel, 0, row);
        grid.SetColumnSpan(okPanel, 5);
        AcceptButton = _okBtn;
        CancelButton = cancelBtn;

        Controls.Add(grid);
    }

    static Label MakeNameLabel(string text) => new()
    {
        Text = text, AutoSize = false, Width = 130, Height = 22,
        TextAlign = ContentAlignment.MiddleRight,
        Margin = new Padding(0, 4, 6, 0),
    };

    void AddRow(TableLayoutPanel tlp, ref int row, string name, NumericUpDown min, NumericUpDown max, string unit)
    {
        tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlp.Controls.Add(MakeNameLabel(name), 0, row);
        tlp.Controls.Add(min, 1, row);
        tlp.Controls.Add(new Label { Text = "to", AutoSize = true, Margin = new Padding(0, 6, 0, 0), TextAlign = ContentAlignment.MiddleCenter }, 2, row);
        tlp.Controls.Add(max, 3, row);
        if (!string.IsNullOrEmpty(unit))
            tlp.Controls.Add(new Label { Text = unit, AutoSize = true, Margin = new Padding(6, 6, 0, 0) }, 4, row);
        row++;
    }

    /// <summary>Wide-open ranges — every continuous param at its operational extreme, every
    /// orbit type checked, T in [3, 64], P in [1, 64], F in [0, 32]. The "let the optimizer
    /// try everything" preset.</summary>
    static OptimizerSearchSpace WideOpenSpace() => new()
    {
        AllowedOrbitTypes = new HashSet<OrbitType>((OrbitType[])Enum.GetValues(typeof(OrbitType))),
        TMin = 3, TMax = 64,
        PMin = 1, PMax = 64,
        FMin = 0, FMax = 32,
        AltitudeKmMin = 200,  AltitudeKmMax = 45_000,
        ApogeeAltitudeKmMin = 200, ApogeeAltitudeKmMax = 80_000,
        InclinationDegMin = 0, InclinationDegMax = 180,
        LanOffsetDegMin = 0,   LanOffsetDegMax = 360,
        ArgPerigeeDegMin = 0,  ArgPerigeeDegMax = 360,
        PhaseOffsetDegMin = 0, PhaseOffsetDegMax = 360,
    };

    /// <summary>Push a search space into the dialog's controls. Each value is clamped to the
    /// spinner's allowed range, so a saved range slightly outside the current bounds doesn't
    /// throw.</summary>
    void ApplyToControls(OptimizerSearchSpace s)
    {
        _orbWalkerCircular.Checked = s.AllowedOrbitTypes.Contains(OrbitType.WalkerCircular);
        _orbWalkerStar.Checked     = s.AllowedOrbitTypes.Contains(OrbitType.WalkerStar);
        _orbMolniya.Checked        = s.AllowedOrbitTypes.Contains(OrbitType.Molniya);
        _orbTundra.Checked         = s.AllowedOrbitTypes.Contains(OrbitType.Tundra);
        _orbCustom.Checked         = s.AllowedOrbitTypes.Contains(OrbitType.Custom);
        _tMin.Value = Clamp(_tMin, s.TMin);   _tMax.Value = Clamp(_tMax, s.TMax);
        _pMin.Value = Clamp(_pMin, s.PMin);   _pMax.Value = Clamp(_pMax, s.PMax);
        _fMin.Value = Clamp(_fMin, s.FMin);   _fMax.Value = Clamp(_fMax, s.FMax);
        _altMin.Value = Clamp(_altMin, s.AltitudeKmMin);   _altMax.Value = Clamp(_altMax, s.AltitudeKmMax);
        _apMin.Value  = Clamp(_apMin,  s.ApogeeAltitudeKmMin); _apMax.Value  = Clamp(_apMax,  s.ApogeeAltitudeKmMax);
        _incMin.Value = Clamp(_incMin, s.InclinationDegMin); _incMax.Value = Clamp(_incMax, s.InclinationDegMax);
        _lanMin.Value = Clamp(_lanMin, s.LanOffsetDegMin);   _lanMax.Value = Clamp(_lanMax, s.LanOffsetDegMax);
        _argPeMin.Value = Clamp(_argPeMin, s.ArgPerigeeDegMin); _argPeMax.Value = Clamp(_argPeMax, s.ArgPerigeeDegMax);
        _phaseMin.Value = Clamp(_phaseMin, s.PhaseOffsetDegMin); _phaseMax.Value = Clamp(_phaseMax, s.PhaseOffsetDegMax);
    }

    /// <summary>Pull the current control state back out as a fresh OptimizerSearchSpace.
    /// Min/max are normalized: if the user set min &gt; max we swap them so the optimizer
    /// doesn't get confused.</summary>
    OptimizerSearchSpace ReadFromControls()
    {
        var allowed = new HashSet<OrbitType>();
        if (_orbWalkerCircular.Checked) allowed.Add(OrbitType.WalkerCircular);
        if (_orbWalkerStar.Checked)     allowed.Add(OrbitType.WalkerStar);
        if (_orbMolniya.Checked)        allowed.Add(OrbitType.Molniya);
        if (_orbTundra.Checked)         allowed.Add(OrbitType.Tundra);
        if (_orbCustom.Checked)         allowed.Add(OrbitType.Custom);
        // Fallback: if nothing is checked, lock to baseline's orbit type so the run still
        // produces meaningful trials.
        if (allowed.Count == 0) allowed.Add(_baseline.OrbitType);

        int iMin, iMax;
        (iMin, iMax) = OrderInt(_tMin.Value, _tMax.Value);
        var s = new OptimizerSearchSpace { TMin = iMin, TMax = iMax };
        (iMin, iMax) = OrderInt(_pMin.Value, _pMax.Value); s.PMin = iMin; s.PMax = iMax;
        (iMin, iMax) = OrderInt(_fMin.Value, _fMax.Value); s.FMin = iMin; s.FMax = iMax;
        s.AllowedOrbitTypes = allowed;
        (s.AltitudeKmMin,       s.AltitudeKmMax)       = Order(_altMin.Value,    _altMax.Value);
        (s.ApogeeAltitudeKmMin, s.ApogeeAltitudeKmMax) = Order(_apMin.Value,     _apMax.Value);
        (s.InclinationDegMin,   s.InclinationDegMax)   = Order(_incMin.Value,    _incMax.Value);
        (s.LanOffsetDegMin,     s.LanOffsetDegMax)     = Order(_lanMin.Value,    _lanMax.Value);
        (s.ArgPerigeeDegMin,    s.ArgPerigeeDegMax)    = Order(_argPeMin.Value,  _argPeMax.Value);
        (s.PhaseOffsetDegMin,   s.PhaseOffsetDegMax)   = Order(_phaseMin.Value,  _phaseMax.Value);
        return s;
    }

    static (double, double) Order(decimal a, decimal b) =>
        a <= b ? ((double)a, (double)b) : ((double)b, (double)a);
    static (int, int) OrderInt(decimal a, decimal b) =>
        a <= b ? ((int)a, (int)b) : ((int)b, (int)a);
}
