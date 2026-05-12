namespace ConstellationPlanner.Cli;

/// <summary>One RA antenna part. Two flavours:
/// dishes (IsOmni=false) use DiameterM via Physics.GainFromDishDiamater for gain;
/// omnis (IsOmni=true, DiameterM=0) use GainDbi directly — RA's referenceGain field.</summary>
public sealed record AntennaModel(string Name, double DiameterM, string Description = "", bool IsOmni = false, double GainDbi = 0);

/// <summary>One band selection: centre frequency, channel bandwidth, minimum tech level.
/// Values mirror Skopos's BandInfo plus RA's typical defaults; the lower-tech bands
/// (VHF/UHF/S) are estimates.</summary>
public sealed record BandModel(string Name, double FrequencyGHz, double BandwidthMHz, int MinTechLevel);

public static class Catalogs
{
    /// <summary>Common RA dish antennas in RP-1. Diameters approximate; for precise numbers
    /// read antennaDiameter from your installed RealAntennas\Parts\*.cfg.</summary>
    public static readonly AntennaModel[] Antennas =
    {
        new("Communotron HG-5",   0.40, "small fixed dish"),
        new("Communotron HG-48",  0.85, "medium dish"),
        new("Communotron HG-55",  0.50, "Communotron HG-55"),
        new("Communotron HG-61",  1.22, "1.22m parabolic — verified from ReStock.cfg"),
        new("RA-2",               0.40, "small fixed"),
        new("RA-15",              1.50, "medium dish"),
        new("RA-15W",             1.50, "wide-angle 1.5m"),
        new("RA-50",              5.00, "5m dish"),
        new("RA-100",            10.00, "10m dish"),
    };

    /// <summary>Bands used by Skopos + RA defaults. min TL is the lowest tech level that can
    /// transmit in this band per RA's default BandInfo TechLevel field.</summary>
    public static readonly BandModel[] Bands =
    {
        new("VHF",  0.137,    20, 0),
        new("UHF",  0.430,    50, 0),
        new("L",    1.5975, 128, 3),
        new("S",    2.295,  128, 2),
        new("C",    4.768, 1024, 3),
        new("X",    8.450,  500, 5),
        new("Ku",  11.950, 1024, 6),
        new("Ka",  31.850, 1024, 8),
    };

    /// <summary>Omnidirectional vehicle antennas typical of RP-1 builds. Gain values are
    /// representative referenceGain figures from RealAntennas\Parts\*.cfg patches:
    /// whips and probe-integrated omnis cluster around 0.25-0.5 dBi; standard low-gain
    /// vehicle omnis around 1-2 dBi; the higher-end helical/cone omnis around 3-3.8 dBi.</summary>
    public static readonly AntennaModel[] OmniAntennas =
    {
        new("Probe-integrated", 0, "stock SM-25 / Mk1 cabin built-in", IsOmni: true, GainDbi: 0.25),
        new("Whip",             0, "Tantares Octans / SXTsciencenosecone class", IsOmni: true, GainDbi: 0.5),
        new("Communotron 16",   0, "stock-class low-gain omni", IsOmni: true, GainDbi: 1.0),
        new("DF-RD",            0, "AIES CommTech DF-RD / NFE PH-2", IsOmni: true, GainDbi: 2.0),
        new("DTS-R4",           0, "SXT Tube / Coatl cone / AIES ESC-EXP", IsOmni: true, GainDbi: 3.0),
        new("Helical",          0, "Coatl ca_landv_omni / Quetzal", IsOmni: true, GainDbi: 3.8),
    };

    public static AntennaModel FindAntenna(string name) =>
        Antennas.FirstOrDefault(a => a.Name == name)
        ?? OmniAntennas.FirstOrDefault(a => a.Name == name)
        ?? Antennas.First(a => a.Name == "Communotron HG-61");

    public static BandModel FindBand(string name) =>
        Bands.FirstOrDefault(b => b.Name == name) ?? Bands.First(b => b.Name == "L");
}
