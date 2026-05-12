using System.Globalization;
using System.Reflection;

namespace ConstellationPlanner.Cli;

/// <summary>Embedded low-res world coastline (Natural Earth 1:110m, public domain).
/// Loaded lazily from the assembly's <c>coastline.txt</c> embedded resource and exposed as a
/// list of (lat, lon) polylines.</summary>
public static class Coastline
{
    static List<List<(double LatDeg, double LonDeg)>>? _cache;
    static readonly object _lock = new();

    public static List<List<(double LatDeg, double LonDeg)>> Polylines
    {
        get
        {
            if (_cache != null) return _cache;
            lock (_lock)
            {
                if (_cache != null) return _cache;
                _cache = Load();
                return _cache;
            }
        }
    }

    static List<List<(double LatDeg, double LonDeg)>> Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        // Resource name is "<DefaultNamespace>.<filename>" — DefaultNamespace defaults to the
        // assembly name when not overridden.
        string name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(".coastline.txt"))
                      ?? "ConstellationPlanner.Cli.coastline.txt";
        var result = new List<List<(double, double)>>();
        using var stream = asm.GetManifestResourceStream(name);
        if (stream == null) return result;
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var poly = new List<(double, double)>();
            foreach (var pair in line.Split(';'))
            {
                int comma = pair.IndexOf(',');
                if (comma < 0) continue;
                if (!double.TryParse(pair.AsSpan(0, comma), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon)) continue;
                if (!double.TryParse(pair.AsSpan(comma + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat)) continue;
                poly.Add((lat, lon));
            }
            if (poly.Count >= 2) result.Add(poly);
        }
        return result;
    }
}
