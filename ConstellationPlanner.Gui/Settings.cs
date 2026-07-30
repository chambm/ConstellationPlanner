using System.Text.Json;

namespace ConstellationPlanner.Gui;

/// <summary>POCO that mirrors every persisted GUI control + window state. Saved as JSON to
/// <c>%LOCALAPPDATA%\ConstellationPlanner\settings.json</c> on close, reloaded on launch.</summary>
public sealed class GuiSettings
{
    /// <summary>Full path to Skopos's telecom.cfg if the default Steam location doesn't apply
    /// (non-default install, Linux distro install, etc.). Empty string = use Steam default.
    /// Set by the GUI's "browse for telecom.cfg" flow in the connection dropdown.</summary>
    public string SkoposCfgPath { get; set; } = "";

    /// <summary>Absolute path to the user's KSP <c>GameData</c> directory. When set, on
    /// launch the GUI scans <c>RealAntennas/Parts/*.cfg</c> for an up-to-date antenna list
    /// (every part RA patches, including modded antennas like NFE/RT-Redev). Empty string
    /// keeps the hardcoded fallback catalog. Set by the GUI's "load antennas from GameData…"
    /// dropdown entry.</summary>
    public string GameDataPath { get; set; } = "";

    // Constellation
    public string OrbitType { get; set; } = "WalkerCircular"; // WalkerCircular / Molniya / Tundra / Custom
    /// <summary>Perigee altitude (km) — used as plain "altitude" in WalkerCircular mode.</summary>
    public double AltitudeKm { get; set; } = 35786;
    /// <summary>Apogee altitude (km). Equals AltitudeKm for circular; differs for elliptical.</summary>
    public double ApogeeAltitudeKm { get; set; } = 35786;
    public double InclinationDeg { get; set; } = 0;
    public double ArgPerigeeDeg { get; set; } = 270;
    public double LanOffsetDeg { get; set; } = 0;
    public int T { get; set; } = 4;
    public int P { get; set; } = 1;
    public int F { get; set; } = 0;
    public double PhaseOffsetDeg { get; set; } = 45;
    public double MinElevDeg { get; set; } = 10;

    // Sat hardware
    public int TechLevel { get; set; } = 3;

    // Ground antennas — the dropdown selections below are just the "Add antenna" template
    // (used to populate gain/freq when the user clicks the Add button). The authoritative
    // configured antennas live in `GroundAntennas` (one entry per dish).
    public string GroundAntennaModel { get; set; } = "Communotron HG-61";
    public string GroundBand { get; set; } = "L";
    public double GroundStationGainDbi { get; set; } = 50;
    /// <summary>Structured per-antenna list. Each entry is one dish with its own band, gain,
    /// and aim direction. Replaces the legacy <see cref="GroundAimList"/> text field; the old
    /// field is parsed on first load only when this list is empty.</summary>
    public List<ConstellationPlanner.Cli.AntennaAim> GroundAntennas { get; set; } = new();
    /// <summary>Legacy multi-line aim list ("name az el" or "az el band gain" per line). Kept
    /// for backward-compatible loading of pre-listbox settings files; once loaded it migrates
    /// into <see cref="GroundAntennas"/> and is never written again.</summary>
    public string GroundAimList { get; set; } = "";
    /// <summary>Sat-side TX power for ground-link antennas (dBm). 0 = use TL.MaxPowerDbm.</summary>
    public double GroundTxPowerDbm { get; set; } = 0;

    // ISL antennas
    public string IslMode { get; set; } = "None"; // None / Omni / Directional / Targeted
    public string IslAntennaModel { get; set; } = "Communotron HG-61";
    public string IslBand { get; set; } = "L";
    /// <summary>Sat-side TX power for ISL antennas (dBm). 0 = use TL.MaxPowerDbm.</summary>
    public double IslTxPowerDbm { get; set; } = 0;

    // Relay path
    public string PathFromName { get; set; } = "andover";
    public string PathToName { get; set; } = "goonhilly_downs";
    public double RequiredRateMbps { get; set; } = 0;
    public double LatencyLimitSec { get; set; } = 30;

    // Render
    public string Metric { get; set; } = "rx-power (dBm)";
    public string CoverageMode { get; set; } = "DailyAverage"; // DailyAverage | Instantaneous
    public double TimeOffsetH { get; set; } = 0;
    public int Upscale { get; set; } = 3;

    // Display layers
    public bool ShowTrackingLinks { get; set; } = true;
    public bool ShowTelecomLinks { get; set; } = true;
    public bool ShowIsls { get; set; } = true;
    public bool ShowFootprints { get; set; } = true;

    // Animation
    public int FrameCount { get; set; } = 24;
    public double AnimDurationH { get; set; } = 24;
    public int PlaybackFps { get; set; } = 10;

    // Optimizer — persisted across runs so the user doesn't have to re-define the same
    // search space every time. Null on first run; the dialog falls back to a baseline-locked
    // default (everything fixed to current GUI values) and lets the user widen ranges.
    public ConstellationPlanner.Cli.OptimizerSearchSpace? OptimizerSearchSpace { get; set; }
    public int OptimizerTrials { get; set; } = 100;
    public int OptimizerSamplesPerTrial { get; set; } = 500;

    // Window
    public int FormWidth { get; set; } = 1700;
    public int FormHeight { get; set; } = 1080;
    public bool FormMaximized { get; set; } = true;
    public int FormLeft { get; set; } = -1; // -1 = center
    public int FormTop { get; set; } = -1;

    public static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ConstellationPlanner");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static GuiSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new GuiSettings();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<GuiSettings>(json) ?? new GuiSettings();
        }
        catch
        {
            // Corrupt or unreadable — start fresh; never crash on bad settings.
            return new GuiSettings();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, _opts));
        }
        catch
        {
            // Don't fail close if we can't write.
        }
    }
}
