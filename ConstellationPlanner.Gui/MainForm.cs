using ConstellationPlanner.Cli;
using System.Globalization;

namespace ConstellationPlanner.Gui;

public partial class MainForm : Form
{
    readonly PlannerInput _cfg = new();
    readonly System.Windows.Forms.Timer _debounce = new() { Interval = 200 };
    readonly System.Windows.Forms.Timer _playTimer = new();
    bool _renderInFlight;
    bool _renderQueued;

    bool _suppressIslMode;

    enum AnimState { Idle, Rendering, Stopped, Playing }
    AnimState _animState = AnimState.Idle;
    byte[][]? _frames;
    double[][,]? _frameData;
    int _currentFrame;
    bool _autoResume;
    /// <summary>Cancels the currently-running animation render (visual frames + stats sweep).
    /// Cancellation is observed by the Parallel.For loops via ParallelOptions.CancellationToken,
    /// so a config change mid-render bails out of the in-progress sweep instead of waiting for
    /// it to finish.</summary>
    System.Threading.CancellationTokenSource? _bgRenderCts;
    /// <summary>Cancels the in-flight "live preview" render that paints the currently-shown
    /// animation frame's time point with new config so the user gets instant feedback before
    /// the full background re-render finishes.</summary>
    System.Threading.CancellationTokenSource? _liveFrameCts;

    GuiSettings _settings = new();

    // Latest render's heatmap data + image layout, used by the hover-value readout.
    double[,]? _hoverData;
    int _hoverMapW;
    int _hoverMapH;
    int _hoverImgW;
    int _hoverImgH;
    Func<double, string>? _hoverFmt;

    public MainForm()
    {
        InitializeComponent();

        // Catalog-driven items can't go in the designer file (they require LINQ / runtime
        // catalog enumeration); populate them here, before we apply persisted settings.
        PopulateCatalogs();

        _settings = GuiSettings.Load();
        ApplyWindowSettings(_settings);
        KeyDown += OnKeyDown;
        FormClosing += OnFormClosing;
        Load += OnFormLoad;

        ApplySettingsToControls(_settings);
        UpdateOrbitTypeUi();
        UpdateAnimDurationCap();
        WireEvents();
        // Stretch the left-panel group boxes to fill the splitter width whenever it changes.
        // FlowLayoutPanel doesn't auto-stretch its children, so we resize them by hand.
        _leftFlow.SizeChanged += (s, e) => StretchLeftFlowChildren();
        StretchLeftFlowChildren();
        _debounce.Tick += OnDebounceTick;
        ScheduleRender();

        if (Environment.GetEnvironmentVariable("CONSTELLATIONPLANNER_NO_AUTOSCREENSHOT") != "1")
        {
            var oneShot = new System.Windows.Forms.Timer { Interval = 1500 };
            oneShot.Tick += (s, e) => { oneShot.Stop(); SaveScreenshot("gui_screenshot.png"); };
            oneShot.Start();
        }
    }

    void OnFormLoad(object? sender, EventArgs e)
    {
        // SplitterDistance has to be applied after the form has actually been sized,
        // otherwise the value silently clamps to ~50% of an unsized panel.
        try { _split.SplitterDistance = 590; } catch { }
        // Bottom controls panel: ~250 px tall, leaving the rest for the map. FixedPanel=Panel2
        // means resizing the form keeps the bottom panel at its current height; the splitter
        // distance is the *top* panel's height since orientation is Horizontal.
        try { _rightSplit.SplitterDistance = Math.Max(200, _rightSplit.Height - 250); } catch { }
    }

    void ApplyWindowSettings(GuiSettings s)
    {
        Width = s.FormWidth;
        Height = s.FormHeight;
        if (s.FormLeft >= 0 && s.FormTop >= 0)
        {
            StartPosition = FormStartPosition.Manual;
            Location = new Point(s.FormLeft, s.FormTop);
        }
        else
        {
            StartPosition = FormStartPosition.CenterScreen;
        }
        if (s.FormMaximized) WindowState = FormWindowState.Maximized;
    }

    void PopulateCatalogs()
    {
        foreach (var a in Catalogs.Antennas)
        {
            string label = $"{a.Name} ({a.DiameterM:F2}m)";
            _groundAnt.Items.Add(label);
        }
        if (_groundAnt.Items.Count > 0) _groundAnt.SelectedIndex = 0;
        // ISL list depends on mode — populated by RepopulateIslAntennas() once mode is known.
        RepopulateIslAntennas();

        foreach (var b in Catalogs.Bands)
        {
            string label = $"{b.Name} ({b.FrequencyGHz:F2} GHz, {b.BandwidthMHz:F0} MHz)";
            _groundBand.Items.Add(label);
            _islBand.Items.Add(label);
        }
        if (_groundBand.Items.Count > 0) _groundBand.SelectedIndex = 0;
        if (_islBand.Items.Count > 0)    _islBand.SelectedIndex    = 0;

        try
        {
            // Items are populated band-aware in RepopulateStationDropdowns; here we just seed
            // the bare names so a settings load before the first band change still works.
            foreach (var st in Planner.LoadStations())
            {
                _pathFrom.Items.Add(st.Name);
                _pathTo.Items.Add(st.Name);
            }

            // Populate Skopos connection dropdown — one entry per (connection × rx) pair so
            // multi-rx connections (e.g. l0_andover_europe with rx=goonhilly + rx=pleumeur)
            // show as two selectable rows the user can independently test.
            _skoposConnection.Items.Add("(manual selection)");
            foreach (var conn in Planner.SkoposConnections)
                foreach (var rx in conn.RxStations)
                    _skoposConnection.Items.Add(new ConnectionEntry { Conn = conn, Rx = rx });
            _skoposConnection.SelectedIndex = 0;
        }
        catch { /* never let station load failures crash the GUI */ }

        // Defaults (overridden by settings load)
        _islMode.SelectedIndex      = 0;
        _metric.SelectedIndex       = 0;
        _coverageMode.SelectedIndex = 0;
    }

    /// <summary>Refresh From/To station dropdown items so each entry shows the station's gain
    /// at the active band+TL ("andover (58.0 dBi)" / "casey (no UHF)" / "custom_site"). Same
    /// snapshot/restore pattern as RepopulateIslAntennas: rebuilding Items synchronously fires
    /// SelectedIndexChanged on every other ComboBox in the form, so we save+restore around it.</summary>
    void RepopulateStationDropdowns()
    {
        int tl = (int)_techLevel.Value;
        double freqGHz = _cfg.GroundFrequencyGHz;
        string band = freqGHz < 0.3 ? "VHF" : freqGHz < 1.0 ? "UHF" : freqGHz < 2.0 ? "L"
                    : freqGHz < 4.0 ? "S" : freqGHz < 8.0 ? "C" : freqGHz < 12.0 ? "X"
                    : freqGHz < 18.0 ? "Ku" : "Ka";
        var cat = Planner.StationAntennas;
        string FormatItem(string name)
        {
            if (!cat.Contains(name)) return name;
            var ant = Planner.GetStationAntenna(name, freqGHz, tl);
            return ant.HasValue ? $"{name} ({ant.Value.GainDbi:F1} dBi)" : $"{name} (no {band})";
        }
        string fromName = ParseLeadingName(_pathFrom.SelectedItem?.ToString());
        string toName   = ParseLeadingName(_pathTo.SelectedItem?.ToString());
        var snap = SnapshotComboSelections();
        try
        {
            _pathFrom.Items.Clear();
            _pathTo.Items.Clear();
            foreach (var st in Planner.LoadStations())
            {
                string label = FormatItem(st.Name);
                _pathFrom.Items.Add(label);
                _pathTo.Items.Add(label);
            }
        }
        catch { /* never crash on station load */ }
        SelectByPrefix(_pathFrom, fromName);
        SelectByPrefix(_pathTo,   toName);
        RestoreComboSelections(snap, exclude: null);
    }

    /// <summary>Swap the ISL antenna dropdown between dish entries (Directional mode) and
    /// omni entries (Omni mode). Called from PopulateCatalogs and on IslMode change.</summary>
    void RepopulateIslAntennas() => RepopulateIslAntennas(_islMode.SelectedIndex == 1);

    Dictionary<ComboBox, int> SnapshotComboSelections()
    {
        var dict = new Dictionary<ComboBox, int>();
        foreach (var cb in new[] { _islMode, _islAnt, _islBand, _groundAnt, _groundBand, _pathFrom, _pathTo, _metric, _coverageMode })
            dict[cb] = cb.SelectedIndex;
        return dict;
    }

    void RestoreComboSelections(Dictionary<ComboBox, int> snap, ComboBox? exclude = null)
    {
        foreach (var (cb, idx) in snap)
        {
            if (cb == exclude) continue;
            if (cb.SelectedIndex == idx) continue;
            if (idx < 0 || idx >= cb.Items.Count) continue;
            cb.SelectedIndex = idx;
        }
    }

    /// <summary>WinForms quirk: rebuilding _islAnt's Items collection triggers a layout
    /// cascade that synchronously fires SelectedIndexChanged on every ComboBox in the form,
    /// snapping each one to index 0. Snapshot every combobox's selection before the mutation
    /// and restore anything that drifted afterward.</summary>
    void RepopulateIslAntennas(bool omni)
    {
        string current = ParseLeadingName(_islAnt.SelectedItem?.ToString());
        var snap = SnapshotComboSelections();
        _islAnt.Items.Clear();
        if (omni)
        {
            foreach (var a in Catalogs.OmniAntennas)
                _islAnt.Items.Add($"{a.Name} ({a.GainDbi:F1} dBi omni)");
        }
        else
        {
            foreach (var a in Catalogs.Antennas)
                _islAnt.Items.Add($"{a.Name} ({a.DiameterM:F2}m)");
        }
        if (_islAnt.Items.Count > 0)
        {
            if (!string.IsNullOrEmpty(current))
                SelectByPrefix(_islAnt, current);
            if (_islAnt.SelectedIndex < 0) _islAnt.SelectedIndex = 0;
        }
        RestoreComboSelections(snap, exclude: _islAnt);
    }

    void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        try { CaptureSettings().Save(); } catch { /* never block close */ }
    }

    GuiSettings CaptureSettings()
    {
        var s = new GuiSettings
        {
            OrbitType      = _orbitType.SelectedIndex switch { 1 => "Molniya", 2 => "Tundra", 3 => "Custom", _ => "WalkerCircular" },
            AltitudeKm     = (double)_altitude.Value,
            ApogeeAltitudeKm = (double)_apogee.Value,
            ArgPerigeeDeg  = (double)_argPe.Value,
            LanOffsetDeg   = (double)_lanOffset.Value,
            InclinationDeg = (double)_inclination.Value,
            T              = (int)_t.Value,
            P              = (int)_p.Value,
            F              = (int)_f.Value,
            PhaseOffsetDeg = (double)_phaseOffset.Value,
            MinElevDeg     = (double)_minElev.Value,

            TechLevel      = (int)_techLevel.Value,

            GroundAntennaModel = ParseLeadingName(_groundAnt.SelectedItem?.ToString()),
            GroundBand         = ParseLeadingName(_groundBand.SelectedItem?.ToString()),
            GroundStationGainDbi = _cfg.GroundStationGainDbi,
            GroundAimList      = _groundAnts.Text,
            GroundTxPowerDbm   = (double)_groundTxPower.Value,

            IslMode            = _islMode.SelectedIndex switch { 1 => "Omni", 2 => "Directional", 3 => "Targeted", _ => "None" },
            IslAntennaModel    = ParseLeadingName(_islAnt.SelectedItem?.ToString()),
            IslBand            = ParseLeadingName(_islBand.SelectedItem?.ToString()),
            IslTxPowerDbm      = (double)_islTxPower.Value,

            PathFromName     = ParseLeadingName(_pathFrom.SelectedItem?.ToString()),
            PathToName       = ParseLeadingName(_pathTo.SelectedItem?.ToString()),
            RequiredRateMbps = (double)_requiredRate.Value,
            LatencyLimitSec  = (double)_latencyLimit.Value,

            Metric        = _metric.SelectedIndex == 1 ? "data rate (bps)" : "rx-power (dBm)",
            CoverageMode  = _coverageMode.SelectedIndex == 1 ? "Instantaneous" : "DailyAverage",
            TimeOffsetH   = (double)_timeOffset.Value,
            Upscale       = (int)_upscale.Value,

            ShowTrackingLinks = _chkTrackingLinks.Checked,
            ShowTelecomLinks  = _chkTelecomLinks.Checked,
            ShowIsls          = _chkIsls.Checked,
            ShowFootprints    = _chkFootprints.Checked,

            FrameCount    = (int)_frameCount.Value,
            AnimDurationH = (double)_animDurationH.Value,
            PlaybackFps   = (int)_playFps.Value,
        };
        if (WindowState == FormWindowState.Normal)
        {
            s.FormWidth = Width;
            s.FormHeight = Height;
            s.FormLeft = Left;
            s.FormTop = Top;
        }
        else
        {
            s.FormWidth = RestoreBounds.Width;
            s.FormHeight = RestoreBounds.Height;
            s.FormLeft = RestoreBounds.Left;
            s.FormTop = RestoreBounds.Top;
        }
        s.FormMaximized = WindowState == FormWindowState.Maximized;
        return s;
    }

    void ApplySettingsToControls(GuiSettings s)
    {
        _orbitType.SelectedIndex = s.OrbitType switch { "Molniya" => 1, "Tundra" => 2, "Custom" => 3, _ => 0 };
        SetNumeric(_altitude,    (decimal)s.AltitudeKm);
        SetNumeric(_apogee,      (decimal)Math.Max((double)_apogee.Minimum, s.ApogeeAltitudeKm));
        SetNumeric(_argPe,       (decimal)s.ArgPerigeeDeg);
        SetNumeric(_lanOffset,   (decimal)s.LanOffsetDeg);
        SetNumeric(_inclination, (decimal)s.InclinationDeg);
        SetNumeric(_t, s.T);
        SetNumeric(_p, s.P);
        SetNumeric(_f, s.F);
        SetNumeric(_phaseOffset, (decimal)s.PhaseOffsetDeg);
        SetNumeric(_minElev,     (decimal)s.MinElevDeg);
        SetNumeric(_techLevel,   s.TechLevel);

        SelectByPrefix(_groundAnt,  s.GroundAntennaModel);
        SelectByPrefix(_groundBand, s.GroundBand);
        _groundAnts.Text = s.GroundAimList;
        SetNumeric(_groundTxPower, (decimal)s.GroundTxPowerDbm);

        int targetIslMode = s.IslMode switch { "Omni" => 1, "Directional" => 2, "Targeted" => 3, _ => 0 };
        // Populate antenna list first based on intended mode, then set the mode dropdown last —
        // see comment on RepopulateIslAntennas(bool) for the WinForms quirk this works around.
        RepopulateIslAntennas(omni: targetIslMode == 1);
        SelectByPrefix(_islAnt,  s.IslAntennaModel);
        SelectByPrefix(_islBand, s.IslBand);
        SetNumeric(_islTxPower, (decimal)s.IslTxPowerDbm);
        _islMode.SelectedIndex = targetIslMode;

        SelectComboItem(_pathFrom, s.PathFromName);
        SelectComboItem(_pathTo,   s.PathToName);
        SetNumeric(_requiredRate, (decimal)s.RequiredRateMbps);
        SetNumeric(_latencyLimit, (decimal)s.LatencyLimitSec);

        _metric.SelectedIndex = s.Metric == "data rate (bps)" ? 1 : 0;
        _coverageMode.SelectedIndex = s.CoverageMode == "Instantaneous" ? 1 : 0;
        SetNumeric(_timeOffset, (decimal)s.TimeOffsetH);
        SetNumeric(_upscale,    s.Upscale);

        _chkTrackingLinks.Checked = s.ShowTrackingLinks;
        _chkTelecomLinks.Checked  = s.ShowTelecomLinks;
        _chkIsls.Checked          = s.ShowIsls;
        _chkFootprints.Checked    = s.ShowFootprints;

        SetNumeric(_frameCount,    s.FrameCount);
        SetNumeric(_animDurationH, (decimal)s.AnimDurationH);
        SetNumeric(_playFps,       s.PlaybackFps);
    }

    static void SetNumeric(NumericUpDown nud, decimal value)
    {
        if (value < nud.Minimum) value = nud.Minimum;
        else if (value > nud.Maximum) value = nud.Maximum;
        nud.Value = value;
    }

    static void SelectByPrefix(ComboBox cb, string prefix)
    {
        if (string.IsNullOrEmpty(prefix) || cb.Items.Count == 0) return;
        for (int i = 0; i < cb.Items.Count; i++)
        {
            string s = cb.Items[i]?.ToString() ?? "";
            if (s == prefix || s.StartsWith(prefix + " "))
            {
                cb.SelectedIndex = i;
                return;
            }
        }
    }

    static void SelectComboItem(ComboBox cb, string value)
    {
        if (cb.Items.Count == 0) return;
        int idx = cb.Items.IndexOf(value);
        cb.SelectedIndex = idx >= 0 ? idx : 0;
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.S)
        {
            SaveScreenshot("gui_screenshot.png");
            e.Handled = true;
        }
    }

    void SaveScreenshot(string filename)
    {
        try
        {
            using var bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
            DrawToBitmap(bmp, new Rectangle(Point.Empty, ClientSize));
            string outPath = Path.Combine(AppContext.BaseDirectory, filename);
            bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            bmp.Save(Path.Combine(root, filename), System.Drawing.Imaging.ImageFormat.Png);
        }
        catch (Exception ex)
        {
            _status.Text += $"\r\nscreenshot failed: {ex.Message}";
        }
    }

    static string ParseLeadingName(string? comboItem)
    {
        if (string.IsNullOrEmpty(comboItem)) return "";
        int paren = comboItem.IndexOf(" (");
        return paren >= 0 ? comboItem.Substring(0, paren) : comboItem;
    }

    void WireEvents()
    {
        _altitude.ValueChanged    += (s, e) => { _cfg.AltitudeKm = (double)_altitude.Value; UpdateOrbitTypeUi(); UpdateAnimDurationCap(); ScheduleRender(); };
        _apogee.ValueChanged      += (s, e) => { _cfg.ApogeeAltitudeKm = (double)_apogee.Value; UpdateOrbitTypeUi(); UpdateAnimDurationCap(); ScheduleRender(); };
        _orbitType.SelectedIndexChanged += (s, e) =>
        {
            _cfg.OrbitType = _orbitType.SelectedIndex switch
            {
                1 => ConstellationPlanner.Cli.OrbitType.Molniya,
                2 => ConstellationPlanner.Cli.OrbitType.Tundra,
                3 => ConstellationPlanner.Cli.OrbitType.Custom,
                _ => ConstellationPlanner.Cli.OrbitType.WalkerCircular,
            };
            UpdateOrbitTypeUi(); UpdateAnimDurationCap(); ScheduleRender();
        };
        _argPe.ValueChanged       += (s, e) => { _cfg.ArgPerigeeDeg = (double)_argPe.Value; ScheduleRender(); };
        _lanOffset.ValueChanged   += (s, e) => { _cfg.LanOffsetDeg = (double)_lanOffset.Value; ScheduleRender(); };
        _skoposConnection.SelectedIndexChanged += (s, e) =>
        {
            if (_skoposConnection.SelectedItem is ConnectionEntry entry)
            {
                // From/To dropdowns hold catalog-annotated labels ("andover (58.0 dBi)"); a bare
                // name doesn't match exactly. Use prefix matching so the visible selection tracks
                // the connection's tx/rx names — without this the cfg fields update silently and
                // calculations work, but the dropdowns visually point at the wrong row.
                SelectByPrefix(_pathFrom, entry.Conn.TxStation);
                SelectByPrefix(_pathTo,   entry.Rx);
                _cfg.PathFromName = entry.Conn.TxStation;
                _cfg.PathToName   = entry.Rx;
                // Skopos rate is bps; our spinner is Mbps. Latency is already seconds.
                SetNumeric(_requiredRate, (decimal)(entry.Conn.DataRateBps / 1e6));
                SetNumeric(_latencyLimit, (decimal)entry.Conn.LatencySec);
                _cfg.RequiredRateMbps = entry.Conn.DataRateBps / 1e6;
                _cfg.LatencyLimitSec  = entry.Conn.LatencySec;
                ScheduleRender();
            }
        };
        _btnTestAllConnections.Click += async (s, e) => await RunTestAllConnections();
        _inclination.ValueChanged += (s, e) => { _cfg.InclinationDeg = (double)_inclination.Value; ScheduleRender(); };
        _t.ValueChanged           += (s, e) => { _cfg.T = (int)_t.Value; ScheduleRender(); };
        _p.ValueChanged           += (s, e) => { _cfg.P = (int)_p.Value; ScheduleRender(); };
        _f.ValueChanged           += (s, e) => { _cfg.F = (int)_f.Value; ScheduleRender(); };
        _phaseOffset.ValueChanged += (s, e) => { _cfg.PhaseOffsetDeg = (double)_phaseOffset.Value; ScheduleRender(); };
        _minElev.ValueChanged     += (s, e) => { _cfg.MinElevDeg = (double)_minElev.Value; ScheduleRender(); };

        _techLevel.ValueChanged   += (s, e) => { _cfg.TechLevel = (int)_techLevel.Value; RepopulateStationDropdowns(); RefreshStationGain(); ScheduleRender(); };

        _groundAnt.SelectedIndexChanged  += (s, e) => { ApplyGroundCatalog();   UpdateInfoLabels(); RefreshStationGain(); ScheduleRender(); };
        _groundBand.SelectedIndexChanged += (s, e) => { ApplyGroundCatalog();   UpdateInfoLabels(); RepopulateStationDropdowns(); RefreshStationGain(); ScheduleRender(); };
        _islMode.SelectedIndexChanged    += (s, e) =>
        {
            if (_suppressIslMode) return;
            int newIdx = _islMode.SelectedIndex;
            _cfg.IslMode = ParseIslMode(newIdx);
            // Defer to next tick AND suppress re-entry for the entire deferred block. The
            // layout cascade triggered by mutating _islAnt.Items synchronously fires
            // SelectedIndexChanged on _islMode (snapping it to 0 mid-flight) — without the
            // guard that re-entry would queue another BeginInvoke with newIdx=0 and clobber us.
            BeginInvoke(new Action(() =>
            {
                _suppressIslMode = true;
                try
                {
                    RepopulateIslAntennas(omni: newIdx == 1);
                    ApplyIslCatalog();
                    UpdateInfoLabels();
                    UpdateIslEnable();
                    if (_islMode.SelectedIndex != newIdx)
                        _islMode.SelectedIndex = newIdx;
                }
                finally { _suppressIslMode = false; }
                ScheduleRender();
            }));
        };
        _islAnt.SelectedIndexChanged     += (s, e) => { ApplyIslCatalog();      UpdateInfoLabels(); ScheduleRender(); };
        _islBand.SelectedIndexChanged    += (s, e) => { ApplyIslCatalog();      UpdateInfoLabels(); ScheduleRender(); };
        _groundTxPower.ValueChanged      += (s, e) => { _cfg.GroundTxPowerDbm = (double)_groundTxPower.Value; ScheduleRender(); };
        _islTxPower.ValueChanged         += (s, e) => { _cfg.IslTxPowerDbm    = (double)_islTxPower.Value;    ScheduleRender(); };
        _metric.SelectedIndexChanged     += (s, e) => { _cfg.Metric = _metric.SelectedIndex == 1 ? HeatmapMetric.DataRate : HeatmapMetric.RxPower; ScheduleRender(); };
        _coverageMode.SelectedIndexChanged += (s, e) => { _cfg.CoverageMode = _coverageMode.SelectedIndex == 1 ? CoverageMode.Instantaneous : CoverageMode.DailyAverage; ScheduleRender(); };
        _techLevel.ValueChanged          += (s, e) => UpdateInfoLabels();

        _groundAnts.TextChanged   += (s, e) => { _cfg.GroundAntennas = ParseAntennas(_groundAnts.Text); ScheduleRender(); };

        _pathFrom.SelectedIndexChanged += (s, e) => { _cfg.PathFromName = ParseLeadingName(_pathFrom.SelectedItem?.ToString()); RefreshStationGain(); ScheduleRender(); };
        _pathTo.SelectedIndexChanged   += (s, e) => { _cfg.PathToName   = ParseLeadingName(_pathTo.SelectedItem?.ToString());   RefreshStationGain(); ScheduleRender(); };
        _requiredRate.ValueChanged+= (s, e) => { _cfg.RequiredRateMbps = (double)_requiredRate.Value; ScheduleRender(); };
        _latencyLimit.ValueChanged+= (s, e) => { _cfg.LatencyLimitSec  = (double)_latencyLimit.Value; ScheduleRender(); };

        _timeOffset.ValueChanged  += (s, e) => { _cfg.TimeOffsetSec = (double)_timeOffset.Value * 3600; ScheduleRender(); };
        _upscale.ValueChanged     += (s, e) => { _cfg.Upscale = (int)_upscale.Value; ScheduleRender(); };

        _chkTrackingLinks.CheckedChanged += (s, e) => { _cfg.ShowTrackingLinks = _chkTrackingLinks.Checked; ScheduleRender(); };
        _chkTelecomLinks.CheckedChanged  += (s, e) => { _cfg.ShowTelecomLinks  = _chkTelecomLinks.Checked;  ScheduleRender(); };
        _chkIsls.CheckedChanged          += (s, e) => { _cfg.ShowIsls          = _chkIsls.Checked;          ScheduleRender(); };
        _chkFootprints.CheckedChanged    += (s, e) => { _cfg.ShowFootprints    = _chkFootprints.Checked;    ScheduleRender(); };

        _animBtn.Click            += (s, e) => OnAnimButtonClick();
        _playTimer.Tick           += (s, e) => OnPlayTick();
        _playFps.ValueChanged     += (s, e) =>
        {
            int fps = Math.Max(1, (int)_playFps.Value);
            _playTimer.Interval = Math.Max(1, 1000 / fps);
        };
        _frameCount.ValueChanged  += (s, e) => RegenFramesIfPresent();

        _pic.MouseMove  += OnPicMouseMove;
        _pic.MouseLeave += OnPicMouseLeave;
        _playTimer.Interval = 100;

        // Initial config from controls.
        _cfg.OrbitType = _orbitType.SelectedIndex switch
        {
            1 => ConstellationPlanner.Cli.OrbitType.Molniya,
            2 => ConstellationPlanner.Cli.OrbitType.Tundra,
            3 => ConstellationPlanner.Cli.OrbitType.Custom,
            _ => ConstellationPlanner.Cli.OrbitType.WalkerCircular,
        };
        _cfg.AltitudeKm = (double)_altitude.Value;
        _cfg.ApogeeAltitudeKm = (double)_apogee.Value;
        _cfg.ArgPerigeeDeg = (double)_argPe.Value;
        _cfg.LanOffsetDeg = (double)_lanOffset.Value;
        _cfg.InclinationDeg = (double)_inclination.Value;
        _cfg.T = (int)_t.Value; _cfg.P = (int)_p.Value; _cfg.F = (int)_f.Value;
        _cfg.PhaseOffsetDeg = (double)_phaseOffset.Value;
        _cfg.MinElevDeg = (double)_minElev.Value;
        _cfg.TechLevel = (int)_techLevel.Value;
        _cfg.GroundTxPowerDbm = (double)_groundTxPower.Value;
        _cfg.IslTxPowerDbm    = (double)_islTxPower.Value;
        _cfg.IslMode = ParseIslMode(_islMode.SelectedIndex);
        _cfg.Metric = _metric.SelectedIndex == 1 ? HeatmapMetric.DataRate : HeatmapMetric.RxPower;
        _cfg.CoverageMode = _coverageMode.SelectedIndex == 1 ? CoverageMode.Instantaneous : CoverageMode.DailyAverage;
        ApplyGroundCatalog();
        ApplyIslCatalog();
        RefreshStationGain();
        _cfg.GroundAntennas = ParseAntennas(_groundAnts.Text);
        UpdateInfoLabels();
        UpdateIslEnable();
        _cfg.PathFromName = ParseLeadingName(_pathFrom.SelectedItem?.ToString());
        _cfg.PathToName   = ParseLeadingName(_pathTo.SelectedItem?.ToString());
        // Now that GroundFrequencyGHz / TechLevel / PathFromName / PathToName are all set,
        // refresh dropdown items with their gain annotations.
        RepopulateStationDropdowns();
        _cfg.RequiredRateMbps = (double)_requiredRate.Value;
        _cfg.LatencyLimitSec = (double)_latencyLimit.Value;
        _cfg.TimeOffsetSec = (double)_timeOffset.Value * 3600;
        _cfg.Upscale = (int)_upscale.Value;
        _cfg.ShowTrackingLinks = _chkTrackingLinks.Checked;
        _cfg.ShowTelecomLinks  = _chkTelecomLinks.Checked;
        _cfg.ShowIsls          = _chkIsls.Checked;
        _cfg.ShowFootprints    = _chkFootprints.Checked;
        _cfg.FullCaption = false;
    }

    void ApplyGroundCatalog()
    {
        var ant = Catalogs.FindAntenna(ParseLeadingName(_groundAnt.SelectedItem?.ToString()));
        var band = Catalogs.FindBand(ParseLeadingName(_groundBand.SelectedItem?.ToString()));
        _cfg.GroundAntennaDiameterM = ant.DiameterM;
        _cfg.GroundFrequencyGHz = band.FrequencyGHz;
        _cfg.GroundBandwidthMHz = band.BandwidthMHz;
    }
    void ApplyIslCatalog()
    {
        var ant = Catalogs.FindAntenna(ParseLeadingName(_islAnt.SelectedItem?.ToString()));
        var band = Catalogs.FindBand(ParseLeadingName(_islBand.SelectedItem?.ToString()));
        _cfg.IslAntennaDiameterM = ant.DiameterM;
        _cfg.IslFrequencyGHz = band.FrequencyGHz;
        _cfg.IslBandwidthMHz = band.BandwidthMHz;
        _cfg.IslGainDbiOverride = ant.IsOmni ? ant.GainDbi : 0;
    }
    /// <summary>Recompute the (read-only) Station gain label from the FROM/TO stations'
    /// catalog entries at the current ground band + tech level, and update <see cref="_cfg"/>
    /// so the path budget uses the right number on the next render.</summary>
    void RefreshStationGain()
    {
        int tl = (int)_techLevel.Value;
        double freqGHz = _cfg.GroundFrequencyGHz;
        var cat = Planner.StationAntennas;
        bool fromKnown = !string.IsNullOrEmpty(_cfg.PathFromName) && cat.Contains(_cfg.PathFromName);
        bool toKnown   = !string.IsNullOrEmpty(_cfg.PathToName)   && cat.Contains(_cfg.PathToName);
        var fromAnt = fromKnown ? Planner.GetStationAntenna(_cfg.PathFromName, freqGHz, tl) : null;
        var toAnt   = toKnown   ? Planner.GetStationAntenna(_cfg.PathToName,   freqGHz, tl) : null;

        string band = freqGHz < 0.3 ? "VHF" : freqGHz < 1.0 ? "UHF" : freqGHz < 2.0 ? "L"
                    : freqGHz < 4.0 ? "S" : freqGHz < 8.0 ? "C" : freqGHz < 12.0 ? "X"
                    : freqGHz < 18.0 ? "Ku" : "Ka";

        string Tag(string name, bool known, ConstellationPlanner.Core.StationAntennaSpec.Effective? eff)
        {
            if (eff.HasValue) return $"{name} {eff.Value.GainDbi:F1}";
            if (known) return $"{name} no {band}";
            return $"{name} not in catalog";
        }

        if (fromAnt.HasValue && toAnt.HasValue)
        {
            _cfg.GroundStationGainDbi = Math.Min(fromAnt.Value.GainDbi, toAnt.Value.GainDbi);
            _groundStationGain.Text = $"{Tag(_cfg.PathFromName, fromKnown, fromAnt)} / {Tag(_cfg.PathToName, toKnown, toAnt)} dBi (TL{tl})";
        }
        else
        {
            // At least one endpoint lacks a usable antenna. Path will go UNREACHABLE in the
            // planner; reflect that here too. cfg.GroundStationGainDbi is left at its default
            // and only used when neither endpoint is in the catalog.
            string from = Tag(_cfg.PathFromName, fromKnown, fromAnt);
            string to   = Tag(_cfg.PathToName,   toKnown,   toAnt);
            _groundStationGain.Text = $"{from} / {to} (TL{tl})";
        }
    }

    void UpdateInfoLabels()
    {
        _groundInfo.Text = ComputeAntennaInfo(_cfg.GroundAntennaDiameterM, _cfg.GroundFrequencyGHz, _cfg.GroundBandwidthMHz, gainOverride: 0)
                         + $" + station {_cfg.GroundStationGainDbi:F0} dBi";
        _islInfo.Text    = _cfg.IslMode == IslMode.None
            ? "(disabled)"
            : ComputeAntennaInfo(_cfg.IslAntennaDiameterM, _cfg.IslFrequencyGHz, _cfg.IslBandwidthMHz, gainOverride: _cfg.IslGainDbiOverride);
    }
    string ComputeAntennaInfo(double diameterM, double freqGHz, double bwMHz, double gainOverride)
    {
        int tl = (int)_techLevel.Value;
        var tlp = TechLevels.Get(tl);
        bool omni = gainOverride > 0;
        float gain = omni
            ? (float)gainOverride
            : ConstellationPlanner.Core.Physics.GainFromDishDiamater(
                (float)diameterM, (float)(freqGHz * 1e9), (float)tlp.ReflectorEff);
        string mod = tlp.ModulationBits switch { 1 => "BPSK", 2 => "QPSK", 3 => "8PSK", 4 => "16QAM", _ => $"{tlp.ModulationBits}-bit" };
        string fec = tlp.CodingRate >= 0.99 ? "no FEC" : $"rate-{tlp.CodingRate:F2}";
        if (omni)
            return $"gain {gain:F1} dBi (omni), ch {bwMHz:F0} MHz, {mod} {fec}, Eb/N0 ≥ {tlp.RequiredEbN0Db:F1} dB";
        float beamwidth = ConstellationPlanner.Core.Physics.Beamwidth(gain);
        return $"gain {gain:F1} dBi, HPBW {beamwidth:F1}°, ch {bwMHz:F0} MHz, {mod} {fec}, Eb/N0 ≥ {tlp.RequiredEbN0Db:F1} dB";
    }
    void UpdateIslEnable()
    {
        bool enabled = _cfg.IslMode != IslMode.None;
        _islAnt.Enabled = enabled;
        _islBand.Enabled = enabled;
    }

    /// <summary>Resize each top-level group box in the left flow panel to fill the flow's
    /// available width. FlowLayoutPanel + TopDown gives us vertical stacking but doesn't
    /// stretch children horizontally; we update widths manually whenever the splitter or
    /// window resizes. Each group's TLP has column 2 set to Percent(100), so the controls
    /// inside fill the new width via their Left|Right anchors.</summary>
    void StretchLeftFlowChildren()
    {
        if (_leftFlow == null) return;
        int target = _leftFlow.ClientSize.Width - _leftFlow.Padding.Horizontal;
        const int childMargin = 6;        // Margin.Horizontal default for FlowLayoutPanel children
        int w = Math.Max(200, target - childMargin);
        foreach (Control c in _leftFlow.Controls)
            if (c.Width != w) c.Width = w;
    }

    /// <summary>Cap the animation-duration spinner at the constellation's ground-track repeat
    /// period — the smallest cycle (within a 168 h window) where the body-fixed pattern lands
    /// within ~1° of its start, so playback can loop with at most that much snap. Recalculated
    /// on every altitude change since semi-major axis drives the T_orb/T_sid ratio that
    /// determines repeat. Also auto-snaps the current value to the new cap so seamless loop is
    /// the default; user can shorten the spinner manually but anything other than the cap will
    /// reintroduce the loop snap.</summary>
    void UpdateAnimDurationCap()
    {
        // Use perigee + apogee so elliptical SMA is computed correctly. For circular the two
        // are equal and this collapses to the same cap as before.
        double pe = _cfg.AltitudeKm;
        double ap = _cfg.OrbitType == ConstellationPlanner.Cli.OrbitType.WalkerCircular
                    ? pe : _cfg.ApogeeAltitudeKm;
        var repeat = Planner.GroundTrackRepeat(pe, ap);
        double cycleH = repeat.CycleSec / 3600.0;
        decimal capped = (decimal)Math.Max(0.01, Math.Min(168.0, cycleH));
        _animDurationH.Maximum = capped;
        _animDurationH.Value = capped;
    }

    // Period constants for fixed-period presets. Sidereal half-day (Molniya) and full sidereal
    // day (Tundra) — see Planner.SmaForPeriod for the SMA derivation.
    const double MolniyaPeriodSec = 86_164.0 / 2;
    const double TundraPeriodSec  = 86_164.0;

    /// <summary>Recompute derived orbital fields and enable/disable controls based on the
    /// chosen orbit type. Reads directly from the control values rather than <c>_cfg</c> so
    /// it's safe to call before WireEvents has populated <c>_cfg</c> from controls (the
    /// initial-load path). Writes back to <c>_cfg</c> at the end.
    /// <para>Cycle: orbit-type drives which fields are editable; Pe (and Ap for Custom) drives
    /// the derived eccentricity display; for Molniya/Tundra we additionally lock Ap and i to
    /// the preset values.</para></summary>
    void UpdateOrbitTypeUi()
    {
        var ot = _orbitType.SelectedIndex switch
        {
            1 => ConstellationPlanner.Cli.OrbitType.Molniya,
            2 => ConstellationPlanner.Cli.OrbitType.Tundra,
            3 => ConstellationPlanner.Cli.OrbitType.Custom,
            _ => ConstellationPlanner.Cli.OrbitType.WalkerCircular,
        };
        bool walker  = ot == ConstellationPlanner.Cli.OrbitType.WalkerCircular;
        bool molniya = ot == ConstellationPlanner.Cli.OrbitType.Molniya;
        bool tundra  = ot == ConstellationPlanner.Cli.OrbitType.Tundra;
        bool custom  = ot == ConstellationPlanner.Cli.OrbitType.Custom;

        _lblAltitude.Text = walker ? "Altitude (km)" : "Perigee (km)";
        _apogee.Enabled    = custom;
        _lanOffset.Enabled = !walker;
        _argPe.Enabled     = !walker;
        _inclination.Enabled = walker || custom;

        double peKm = (double)_altitude.Value;
        double apKm = (double)_apogee.Value;

        if (molniya || tundra)
        {
            const double criticalI = 63.4;
            if ((double)_inclination.Value != criticalI) SetNumeric(_inclination, (decimal)criticalI);

            double targetSmaM = Planner.SmaForPeriod(molniya ? MolniyaPeriodSec : TundraPeriodSec);
            const double earthR = 6_371_000.0;
            double rPe = earthR + peKm * 1000;
            double rApDerived = 2 * targetSmaM - rPe;
            apKm = (rApDerived - earthR) / 1000;
            apKm = Math.Max((double)_apogee.Minimum, Math.Min((double)_apogee.Maximum, apKm));
            if ((double)_apogee.Value != apKm) SetNumeric(_apogee, (decimal)apKm);
        }
        else if (walker)
        {
            if (_apogee.Value != _altitude.Value) SetNumeric(_apogee, _altitude.Value);
            apKm = peKm;
        }

        // Sync cfg from the (possibly just-derived) control values so downstream callers
        // (UpdateAnimDurationCap, ScheduleRender) see the right state.
        _cfg.OrbitType        = ot;
        _cfg.AltitudeKm       = peKm;
        _cfg.ApogeeAltitudeKm = apKm;
        _cfg.InclinationDeg   = (double)_inclination.Value;
        _cfg.ArgPerigeeDeg    = (double)_argPe.Value;
        _cfg.LanOffsetDeg     = (double)_lanOffset.Value;

        // Eccentricity display.
        const double earthRm = 6_371_000.0;
        double rPeM = earthRm + peKm * 1000;
        double rApM = earthRm + apKm * 1000;
        double e = rApM > rPeM ? (rApM - rPeM) / (rApM + rPeM) : 0;
        _eccentricity.Text = $"{e:F4}";
    }
    static IslMode ParseIslMode(int comboIndex) => comboIndex switch
    {
        1 => IslMode.Omni,
        2 => IslMode.Directional,
        3 => IslMode.Targeted,
        _ => IslMode.None,
    };

    /// <summary>Run every Skopos connection (one row per (connection × rx)) through the
    /// constellation's repeat cycle with a shared <see cref="ConstellationPlanner.Core.NetworkUsage"/>
    /// at each timestep so capacity contention propagates Skopos-style. Dumps a per-connection
    /// summary into the status textbox: name | uptime | met-window | avg lat | avg rate.</summary>
    async Task RunTestAllConnections()
    {
        if (Planner.SkoposConnections.Count == 0)
        {
            _status.Text = "no Skopos connections loaded — check that telecom.cfg is present and parsed.";
            return;
        }
        var connections = new List<(ConstellationPlanner.Core.SkoposConnection, int)>();
        foreach (var c in Planner.SkoposConnections)
            for (int rxIdx = 0; rxIdx < c.RxStations.Count; rxIdx++)
                connections.Add((c, rxIdx));

        // Use the same cycle length the animation uses so the multi-connection eval matches the
        // single-path animation stats (both sweep one full ground-track repeat).
        double durationSec = (double)_animDurationH.Value * 3600;
        int samples = Math.Min(2000, Math.Max(120, (int)(durationSec / 60)));   // 1/min, capped at 2000 to keep runtime bounded
        var snapshot = CloneCfg(_cfg);
        snapshot.SkipHeatmap = true;

        _btnTestAllConnections.Enabled = false;
        _btnTestAllConnections.Text = "Testing… 0%";
        _status.Text = $"running {connections.Count} connections × {samples} samples over {durationSec/3600:F1} h…";
        try
        {
            int lastPct = -1;
            var progress = new Progress<int>(pct =>
            {
                if (pct != lastPct) { lastPct = pct; _btnTestAllConnections.Text = $"Testing… {pct}%"; }
            });
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var results = await Task.Run(() =>
                Planner.EvaluateConnectionsOverCycle(snapshot, connections, durationSec, samples,
                    (done, total) => ((IProgress<int>)progress).Report(done * 100 / total)));
            sw.Stop();

            // Aggregate across all rows for the top-of-output summary. Buckets use the same
            // thresholds that Skopos contracts typically gate on (≥95% reliable, ≥50% partial).
            int evaluated = 0, atLeast95 = 0, between50and95 = 0, below50 = 0, neverUp = 0;
            int notFound = 0;
            double sumUptime = 0, sumMetWin = 0;
            int bestIdx = -1, worstIdx = -1;
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                if (r.FromIdx < 0 || r.ToIdx < 0) { notFound++; continue; }
                evaluated++;
                sumUptime += r.UptimePct;
                sumMetWin += r.MetWindowPct;
                if (r.UptimePct >= 95)      atLeast95++;
                else if (r.UptimePct >= 50) between50and95++;
                else if (r.UptimePct > 0)   below50++;
                else                        neverUp++;
                if (bestIdx  < 0 || r.UptimePct > results[bestIdx].UptimePct)   bestIdx  = i;
                if (worstIdx < 0 || r.UptimePct < results[worstIdx].UptimePct)  worstIdx = i;
            }
            double meanUptime = evaluated > 0 ? sumUptime / evaluated : 0;
            double meanMetWin = evaluated > 0 ? sumMetWin / evaluated : 0;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Multi-connection eval — {connections.Count} (conn × rx) over {samples} samples / {durationSec/3600:F1} h ({sw.ElapsedMilliseconds} ms)");
            sb.AppendLine();
            sb.AppendLine("SUMMARY");
            sb.AppendLine($"  evaluated: {evaluated} connections" + (notFound > 0 ? $" ({notFound} skipped — station name not found)" : ""));
            sb.AppendLine($"  mean uptime: {meanUptime:F1}%   ·   mean met-window: {meanMetWin:F1}%");
            sb.AppendLine($"  buckets: ≥95% uptime: {atLeast95}   ·   50–95%: {between50and95}   ·   <50%: {below50}   ·   never connects: {neverUp}");
            if (bestIdx >= 0)
            {
                var b = results[bestIdx];
                sb.AppendLine($"  best : {b.Connection.Name}:{b.ToName} — {b.UptimePct:F1}% uptime");
            }
            if (worstIdx >= 0 && worstIdx != bestIdx)
            {
                var w = results[worstIdx];
                sb.AppendLine($"  worst: {w.Connection.Name}:{w.ToName} — {w.UptimePct:F1}% uptime");
            }
            sb.AppendLine();

            // Per-connection detail table.
            sb.AppendLine($"{"connection",-32} {"uptime",10} {"met-win",10} {"avg lat",10} {"avg rate",12}");
            sb.AppendLine(new string('-', 80));
            foreach (var r in results)
            {
                if (r.FromIdx < 0 || r.ToIdx < 0)
                {
                    sb.AppendLine($"{r.Connection.Name + ":" + r.ToName,-32} {"NO STA",10}");
                    continue;
                }
                string lat = r.ConnectedCount > 0 ? $"{r.AvgLatencyMs:F1} ms" : "—";
                string rate = r.ConnectedCount > 0 ? Planner.FmtRate(r.AvgRateBps) : "—";
                string label = r.Connection.Name.Length > 28
                    ? r.Connection.Name.Substring(0, 26) + "…"
                    : r.Connection.Name;
                sb.AppendLine($"{label + ":" + r.ToName,-32} {r.UptimePct,9:F1}% {r.MetWindowPct,9:F1}% {lat,10} {rate,12}");
            }
            _status.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            _status.Text = $"connection eval failed: {ex.GetType().Name}: {ex.Message}\r\n{ex.StackTrace}";
        }
        finally
        {
            _btnTestAllConnections.Enabled = true;
            _btnTestAllConnections.Text = "Test all Skopos connections (multi-conn capacity)";
        }
    }

    /// <summary>Holds one (Skopos connection × chosen rx station) pair for the connection
    /// dropdown. The display string includes the connection's rate + latency budget so the
    /// user can tell at a glance what they're testing against.</summary>
    sealed class ConnectionEntry
    {
        public ConstellationPlanner.Core.SkoposConnection Conn = null!;
        public string Rx = "";
        public override string ToString()
        {
            string rate = Planner.FmtRate(Conn.DataRateBps);
            string lat = Conn.LatencySec >= 1
                ? $"{Conn.LatencySec:F0}s"
                : $"{Conn.LatencySec * 1000:F0}ms";
            string win = Conn.WindowSec > 0 ? $", win {Conn.WindowSec:F0}s" : "";
            return $"{Conn.Name}: {Conn.TxStation}→{Rx} ({rate}/{lat}{win})";
        }
    }

    static List<AntennaAim> ParseAntennas(string text)
    {
        var list = new List<AntennaAim>();
        foreach (var rawLine in text.Replace("\r", "").Split('\n'))
        {
            var line = rawLine.Split('#')[0].Trim();
            if (line.Length == 0) continue;
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            int nLast = parts.Length - 1;
            if (!double.TryParse(parts[nLast], NumberStyles.Float, CultureInfo.InvariantCulture, out double el)) continue;
            if (!double.TryParse(parts[nLast - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double az)) continue;
            string name = nLast >= 2 ? string.Join(' ', parts, 0, nLast - 1) : "";
            list.Add(new AntennaAim { AzimuthDeg = az, ElevationDeg = el, Name = name });
        }
        return list;
    }

    void ScheduleRender()
    {
        OnSettingsChangedForAnim();
        _debounce.Stop();
        _debounce.Start();
    }

    /// <summary>Regenerate the animation frame buffer when an animation-only control changes
    /// (e.g. FrameCount). Idle = no frames yet, leave alone; Stopped = re-render and stay
    /// paused on frame 0; Playing = re-render and resume; Rendering = cancel+restart.</summary>
    void RegenFramesIfPresent()
    {
        switch (_animState)
        {
            case AnimState.Idle: return;
            case AnimState.Rendering:
                _bgRenderCts?.Cancel();
                _ = KickRenderAsync(autoResume: _autoResume);
                break;
            case AnimState.Playing:
                _playTimer.Stop();
                _ = KickRenderAsync(autoResume: true);
                break;
            case AnimState.Stopped:
                _ = KickRenderAsync(autoResume: false);
                break;
        }
    }

    void OnSettingsChangedForAnim()
    {
        switch (_animState)
        {
            case AnimState.Playing:
                // Show the user instant feedback for the currently-shown frame, then re-render
                // the full cycle in the background. Background respects cancellation via the
                // _bgRenderCts so further config changes interrupt rather than queueing.
                _playTimer.Stop();
                _bgRenderCts?.Cancel();
                _ = RenderLiveFrameAsync();
                _ = KickRenderAsync(autoResume: true);
                break;
            case AnimState.Rendering:
                // Cancel the in-flight render and start a fresh one. The currently-shown frame
                // (left over from the previous cycle's playback or live preview) gets re-rendered
                // immediately so the user sees instant feedback while the new full sweep runs.
                _bgRenderCts?.Cancel();
                _ = RenderLiveFrameAsync();
                _ = KickRenderAsync(autoResume: _autoResume);
                break;
            case AnimState.Stopped:
                _frames = null;
                _animState = AnimState.Idle;
                _status.Text = "settings changed — click Render";
                UpdateAnimButton();
                break;
        }
    }

    /// <summary>Re-render just the currently-shown animation frame's time point using current
    /// config and replace <c>_pic.Image</c> immediately, so a config change while playing or
    /// re-rendering gives instant visual feedback rather than waiting for the full N-frame
    /// sweep. Cancellable — repeated rapid changes only finish the latest one.</summary>
    async Task RenderLiveFrameAsync()
    {
        if (_frames == null || _frames.Length == 0) return;   // no anim yet — OnDebounceTick handles preview
        _liveFrameCts?.Cancel();
        var cts = new System.Threading.CancellationTokenSource();
        _liveFrameCts = cts;
        var ct = cts.Token;

        int idx = _currentFrame;
        int N = _frames.Length;
        double durationSec = (double)_animDurationH.Value * 3600;
        var local = CloneCfg(_cfg);
        local.CoverageMode = CoverageMode.Instantaneous;
        local.TimeOffsetSec = idx * durationSec / Math.Max(1, N);
        local.FullCaption = false;
        try
        {
            var output = await Task.Run(() => Planner.Render(local), ct);
            if (ct.IsCancellationRequested || _liveFrameCts != cts) return;
            using var ms = new MemoryStream(output.PngBytes);
            var bmp = (Bitmap)Image.FromStream(ms);
            var old = _pic.Image;
            _pic.Image = bmp;
            old?.Dispose();
            _playbackStatus.Text = $"frame {idx + 1}/{N} (live preview, full re-render in progress…)";
        }
        catch (OperationCanceledException) { /* superseded by a newer call */ }
        catch (Exception ex)
        {
            _playbackStatus.Text = $"live frame failed: {ex.GetType().Name}: {ex.Message}";
        }
    }

    void OnAnimButtonClick()
    {
        switch (_animState)
        {
            case AnimState.Idle:    _ = KickRenderAsync(autoResume: true);  break;
            case AnimState.Stopped: StartPlayback();                        break;
            case AnimState.Playing: StopPlayback();                         break;
        }
    }

    async Task KickRenderAsync(bool autoResume)
    {
        // Cancel any prior render so a rapid sequence of config changes doesn't pile up
        // overlapping sweeps. The new CTS is what each Parallel.For inside this call observes.
        _bgRenderCts?.Cancel();
        var cts = new System.Threading.CancellationTokenSource();
        _bgRenderCts = cts;
        var ct = cts.Token;

        _autoResume = autoResume;
        _animState = AnimState.Rendering;
        UpdateAnimButton();

        int N = (int)_frameCount.Value;
        double durationSec = (double)_animDurationH.Value * 3600;
        var snapshot = CloneCfg(_cfg);
        snapshot.CoverageMode = CoverageMode.Instantaneous;
        snapshot.FullCaption = false;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        int done = 0;
        var frames = new byte[N][];
        var frameData = new double[N][,];
        // Per-frame relay-path outcomes — aggregated after the Parallel.For into uptime + avg
        // latency / rate over the full repeat cycle so the user can see the sustained behaviour
        // rather than just one frame's snapshot.
        var pathConnected = new bool[N];
        var pathLatencyMs = new double[N];
        var pathRateBps   = new double[N];
        int mapW = 0, mapH = 0;
        Func<double, string>? fmt = null;
        var progress = new Progress<int>(i => _status.Text = $"rendering {i}/{N}…");
        ((IProgress<int>)progress).Report(0);

        try
        {
            await Task.Run(() =>
            {
                Parallel.For(0, N, new ParallelOptions { CancellationToken = ct }, i =>
                {
                    var local = CloneCfg(snapshot);
                    local.TimeOffsetSec = i * durationSec / N;
                    var output = Planner.Render(local);
                    frames[i] = output.PngBytes;
                    frameData[i] = output.HeatmapData!;
                    pathConnected[i] = output.PathConnected;
                    pathLatencyMs[i] = output.PathLatencyMs;
                    pathRateBps[i]   = output.PathRateBps;
                    if (i == 0)
                    {
                        mapW = output.HeatmapMapPixelWidth;
                        mapH = output.HeatmapMapPixelHeight;
                        fmt = output.HeatmapValueFormatter;
                    }
                    int now = System.Threading.Interlocked.Increment(ref done);
                    if (now % Math.Max(1, N / 10) == 0)
                        ((IProgress<int>)progress).Report(now);
                });
            }, ct);
            sw.Stop();
            _frames = frames;
            _frameData = frameData;
            _hoverMapW = mapW;
            _hoverMapH = mapH;
            _hoverFmt = fmt;
            _currentFrame = 0;

            // Path-stats sweep — separate from visual rendering so we can sample the cycle at
            // higher temporal resolution than the user's frame count. Uses SkipHeatmap=true so
            // each sample is a few-millisecond Walker+Relay eval rather than a full PNG render.
            // Target ~1 sample per minute of cycle, capped at 5000, floored at the visual frame
            // count (no point sampling stats more coarsely than the animation).
            int Nstats = Math.Min(5000, Math.Max(N, (int)(durationSec / 60)));
            int connectedCount = 0, metReqCount = 0;
            double sumLat = 0, sumRate = 0;
            double requiredBpsThreshold = snapshot.RequiredRateMbps * 1e6;
            // ISL rate aggregation across the cycle. Each timestep's working ISLs each count
            // as one observation of "ISL bandwidth available right now"; we accumulate weighted
            // sums to produce a population mean, plus track the global min/max bracket.
            double islGrandSum = 0;
            long islGrandCount = 0;
            double islGlobalMin = double.PositiveInfinity, islGlobalMax = 0;
            if (Nstats > N)
            {
                var statsConnected = new bool[Nstats];
                var statsBelow = new bool[Nstats];
                var statsLat = new double[Nstats];
                var statsRate = new double[Nstats];
                var statsIslCount = new int[Nstats];
                var statsIslMin = new double[Nstats];
                var statsIslMax = new double[Nstats];
                var statsIslMean = new double[Nstats];
                await Task.Run(() =>
                {
                    Parallel.For(0, Nstats, new ParallelOptions { CancellationToken = ct }, i =>
                    {
                        var local = CloneCfg(snapshot);
                        local.TimeOffsetSec = i * durationSec / Nstats;
                        local.SkipHeatmap = true;
                        var statOut = Planner.Render(local);
                        statsConnected[i] = statOut.PathConnected;
                        statsBelow[i]     = statOut.PathBelowRequired;
                        statsLat[i] = statOut.PathLatencyMs;
                        statsRate[i] = statOut.PathRateBps;
                        statsIslCount[i] = statOut.IslCount;
                        statsIslMin[i] = statOut.IslMinRateBps;
                        statsIslMax[i] = statOut.IslMaxRateBps;
                        statsIslMean[i] = statOut.IslMeanRateBps;
                    });
                }, ct);
                for (int i = 0; i < Nstats; i++)
                {
                    if (statsConnected[i])
                    {
                        connectedCount++;
                        sumLat += statsLat[i];
                        sumRate += statsRate[i];
                        if (!statsBelow[i]) metReqCount++;
                    }
                    if (statsIslCount[i] > 0)
                    {
                        islGrandSum += statsIslMean[i] * statsIslCount[i];
                        islGrandCount += statsIslCount[i];
                        if (statsIslMin[i] < islGlobalMin) islGlobalMin = statsIslMin[i];
                        if (statsIslMax[i] > islGlobalMax) islGlobalMax = statsIslMax[i];
                    }
                }
            }
            else
            {
                Nstats = N;
                // Visual-frame fallback path doesn't capture PathBelowRequired separately — derive it
                // from the cached visual-frame outputs by comparing rate to threshold.
                for (int i = 0; i < N; i++)
                    if (pathConnected[i])
                    {
                        connectedCount++;
                        sumLat += pathLatencyMs[i];
                        sumRate += pathRateBps[i];
                        if (requiredBpsThreshold <= 0 || pathRateBps[i] >= requiredBpsThreshold)
                            metReqCount++;
                    }
            }
            double uptimePct = Nstats > 0 ? 100.0 * connectedCount / Nstats : 0;
            double metReqPct = Nstats > 0 ? 100.0 * metReqCount / Nstats : 0;
            string reqPart = requiredBpsThreshold > 0
                ? $" ({metReqPct:F1}% meets {Planner.FmtRate(requiredBpsThreshold)})"
                : "";
            string statSummary = connectedCount > 0
                ? $"uptime {uptimePct:F1}%{reqPart} · avg lat {sumLat / connectedCount:F1} ms · avg rate {Planner.FmtRate(sumRate / connectedCount)} ({Nstats} samples)"
                : $"uptime 0% (path never connected over {Nstats} samples)";
            string islLine = islGrandCount > 0
                ? $"  ISL rates: min {Planner.FmtRate(islGlobalMin)} · avg {Planner.FmtRate(islGrandSum / islGrandCount)} · max {Planner.FmtRate(islGlobalMax)} ({islGrandCount} link-samples)"
                : "  ISL rates: no working ISLs over the cycle";
            _status.Text = $"animation: {N} frames over {durationSec/3600:F2} h in {sw.ElapsedMilliseconds} ms\r\n"
                         + $"  cycle stats: {statSummary}\r\n"
                         + islLine;

            // Cancellation supersedes the old _restartRequested mechanism: when settings change
            // mid-render, OnSettingsChangedForAnim cancels this CTS and kicks a new
            // KickRenderAsync. We just bail out cleanly here without touching state — the new
            // call already set _bgRenderCts to its own CTS.
            if (ct.IsCancellationRequested) return;

            if (_autoResume)
            {
                _autoResume = false;
                StartPlayback();
            }
            else
            {
                _animState = AnimState.Stopped;
                ShowFrame(0);
                UpdateAnimButton();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Superseded by a newer KickRenderAsync — leave state alone. Either the new call has
            // already updated state (animState = Rendering with its own CTS) or the user moved on
            // to a different action; no error to display.
            return;
        }
        catch (Exception ex)
        {
            // Drill through AggregateException so we surface the underlying problem from
            // Parallel.For inside the visual or stats sweep, not just the wrapper.
            var inner = ex;
            while (inner is AggregateException agg && agg.InnerException != null)
                inner = agg.InnerException;
            _status.Text = $"render failed: {inner.GetType().Name}: {inner.Message}";
            System.Diagnostics.Debug.WriteLine(inner.ToString());
            _animState = AnimState.Idle;
            UpdateAnimButton();
        }
    }

    void StartPlayback()
    {
        if (_frames == null) return;
        _animState = AnimState.Playing;
        int fps = Math.Max(1, (int)_playFps.Value);
        _playTimer.Interval = Math.Max(1, 1000 / fps);
        _playTimer.Start();
        UpdateAnimButton();
    }

    void StopPlayback()
    {
        _playTimer.Stop();
        _animState = _frames != null ? AnimState.Stopped : AnimState.Idle;
        UpdateAnimButton();
    }

    void UpdateAnimButton()
    {
        switch (_animState)
        {
            case AnimState.Idle:      _animBtn.Text = "Play";       _animBtn.Enabled = true; break;
            case AnimState.Rendering: _animBtn.Text = "Rendering…"; _animBtn.Enabled = false; break;
            case AnimState.Stopped:   _animBtn.Text = "Play";       _animBtn.Enabled = true; break;
            case AnimState.Playing:   _animBtn.Text = "Pause";      _animBtn.Enabled = true; break;
        }
    }

    void OnPlayTick()
    {
        if (_frames == null) { StopPlayback(); return; }
        _currentFrame = (_currentFrame + 1) % _frames.Length;
        ShowFrame(_currentFrame);
    }

    void ShowFrame(int idx)
    {
        if (_frames == null || idx < 0 || idx >= _frames.Length) return;
        try
        {
            using var ms = new MemoryStream(_frames[idx]);
            var bmp = (Bitmap)Image.FromStream(ms);
            var old = _pic.Image;
            _pic.Image = bmp;
            old?.Dispose();
            // Per-frame ticker goes to the small _playbackStatus label below Display Layers
            // so it doesn't overwrite the green status textbox's cycle stats / ISL stats.
            _playbackStatus.Text = $"playing {idx + 1}/{_frames.Length}";
            if (_frameData != null && idx < _frameData.Length)
            {
                _hoverData = _frameData[idx];
                _hoverImgW = bmp.Width;
                _hoverImgH = bmp.Height;
            }
        }
        catch (Exception ex)
        {
            _playbackStatus.Text = $"frame display failed: {ex.Message}";
        }
    }

    async void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        if (_animState == AnimState.Playing
            || (_animState == AnimState.Rendering && _autoResume))
            return;
        if (_renderInFlight)
        {
            _renderQueued = true;
            return;
        }
        _renderInFlight = true;
        var snapshot = CloneCfg(_cfg);
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var output = await Task.Run(() => Planner.Render(snapshot));
            sw.Stop();
            using var ms = new MemoryStream(output.PngBytes);
            var bmp = (Bitmap)Image.FromStream(ms);
            var old = _pic.Image;
            _pic.Image = bmp;
            old?.Dispose();
            _status.Text = FormatStatus(output, sw.ElapsedMilliseconds);
            CaptureHoverData(output, bmp);
        }
        catch (Exception ex)
        {
            _status.Text = $"Render error:\r\n{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _renderInFlight = false;
            if (_renderQueued)
            {
                _renderQueued = false;
                ScheduleRender();
            }
        }
    }

    void CaptureHoverData(PlannerOutput output, Bitmap bmp)
    {
        _hoverData = output.HeatmapData;
        _hoverMapW = output.HeatmapMapPixelWidth;
        _hoverMapH = output.HeatmapMapPixelHeight;
        _hoverImgW = bmp.Width;
        _hoverImgH = bmp.Height;
        _hoverFmt = output.HeatmapValueFormatter;
    }

    void OnPicMouseMove(object? sender, MouseEventArgs e)
    {
        if (_hoverData == null || _hoverFmt == null || _hoverImgW <= 0 || _hoverImgH <= 0)
        {
            _hover.Text = "";
            return;
        }
        // PictureBox SizeMode=Zoom: image uniformly scaled to fit, letterboxed.
        var clientW = _pic.ClientSize.Width;
        var clientH = _pic.ClientSize.Height;
        if (clientW <= 0 || clientH <= 0) { _hover.Text = ""; return; }
        double scale = Math.Min((double)clientW / _hoverImgW, (double)clientH / _hoverImgH);
        double drawW = _hoverImgW * scale;
        double drawH = _hoverImgH * scale;
        double offX = (clientW - drawW) / 2.0;
        double offY = (clientH - drawH) / 2.0;
        double imgX = (e.X - offX) / scale;
        double imgY = (e.Y - offY) / scale;
        if (imgX < 0 || imgX >= _hoverMapW || imgY < 0 || imgY >= _hoverMapH)
        {
            _hover.Text = "";
            return;
        }
        int rows = _hoverData.GetLength(0);
        int cols = _hoverData.GetLength(1);
        int row = (int)(imgY / _hoverMapH * rows);
        int col = (int)(imgX / _hoverMapW * cols);
        if (row < 0) row = 0; else if (row >= rows) row = rows - 1;
        if (col < 0) col = 0; else if (col >= cols) col = cols - 1;
        double v = _hoverData[row, col];
        double lat = 90.0 - (row + 0.5) / rows * 180.0;
        double lon = -180.0 + (col + 0.5) / cols * 360.0;
        _hover.Text = $"{lat,6:F1}°, {lon,7:F1}°   {_hoverFmt(v)}";
    }

    void OnPicMouseLeave(object? sender, EventArgs e) => _hover.Text = "";

    static string FormatStatus(PlannerOutput o, long ms)
    {
        string pathLine;
        if (!o.PathConnected) pathLine = "path: UNREACHABLE (no LoS chain or latency cap exceeded)";
        else if (o.PathBelowRequired)
            pathLine = $"path: {o.PathHops} hops, {o.PathLatencyMs:F1} ms light-time, rate {Planner.FmtRate(o.PathRateBps)} — BELOW required";
        else
            pathLine = $"path: {o.PathHops} hops, {o.PathLatencyMs:F1} ms light-time, rate {Planner.FmtRate(o.PathRateBps)}";
        // Power line — radiated/DC per antenna for both roles plus the per-sat DC total.
        // Skipped silently when both roles produced 0 W (no antennas configured for either role).
        string powerLine = (o.GroundTxW > 0 || o.IslTxW > 0)
            ? $"power: ground {Planner.FmtWatts(o.GroundTxW)} tx → {Planner.FmtWatts(o.GroundDcW)} DC · ISL {Planner.FmtWatts(o.IslTxW)} tx → {Planner.FmtWatts(o.IslDcW)} DC · sat total {Planner.FmtWatts(o.SatTotalDcW)} DC\r\n"
            : "";
        return $"render: {ms} ms · gain {o.GainDbi:F1} dBi · HPBW {o.BeamwidthDeg:F1}° · footprint {o.FootprintHalfAngleDeg:F1}° GC · noise {o.NoiseFloorDbm:F1} dBm\r\n"
             + $"links: ISLs {o.IslCount}, ground {o.GroundLinkCount}\r\n"
             + powerLine
             + pathLine;
    }

    static PlannerInput CloneCfg(PlannerInput src) => new()
    {
        OrbitType = src.OrbitType,
        AltitudeKm = src.AltitudeKm, ApogeeAltitudeKm = src.ApogeeAltitudeKm,
        InclinationDeg = src.InclinationDeg, ArgPerigeeDeg = src.ArgPerigeeDeg,
        LanOffsetDeg = src.LanOffsetDeg,
        T = src.T, P = src.P, F = src.F,
        PhaseOffsetDeg = src.PhaseOffsetDeg, MinElevDeg = src.MinElevDeg,
        TechLevel = src.TechLevel,
        GroundAntennaDiameterM = src.GroundAntennaDiameterM, GroundFrequencyGHz = src.GroundFrequencyGHz, GroundBandwidthMHz = src.GroundBandwidthMHz,
        GroundStationGainDbi = src.GroundStationGainDbi, GroundStationTxPowerDbm = src.GroundStationTxPowerDbm,
        GroundTxPowerDbm = src.GroundTxPowerDbm,
        IslMode = src.IslMode,
        IslAntennaDiameterM = src.IslAntennaDiameterM, IslFrequencyGHz = src.IslFrequencyGHz, IslBandwidthMHz = src.IslBandwidthMHz,
        IslGainDbiOverride = src.IslGainDbiOverride,
        IslTxPowerDbm = src.IslTxPowerDbm,
        GroundAntennas = src.GroundAntennas.Select(a => new AntennaAim { Name = a.Name, AzimuthDeg = a.AzimuthDeg, ElevationDeg = a.ElevationDeg }).ToList(),
        Metric = src.Metric, CoverageMode = src.CoverageMode,
        PathFromName = src.PathFromName, PathToName = src.PathToName,
        RequiredRateMbps = src.RequiredRateMbps, LatencyLimitSec = src.LatencyLimitSec,
        TimeOffsetSec = src.TimeOffsetSec, Upscale = src.Upscale, FullCaption = src.FullCaption,
        ShowTrackingLinks = src.ShowTrackingLinks, ShowTelecomLinks = src.ShowTelecomLinks,
        ShowIsls = src.ShowIsls, ShowFootprints = src.ShowFootprints,
    };
}
