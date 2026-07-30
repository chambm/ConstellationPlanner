using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConstellationPlanner.Core;

namespace ConstellationPlanner.Cli;

/// <summary>Result bucket from <see cref="AntennaCatalogLoader.LoadFromGameData"/>: a
/// dish list (antennaDiameter-based) and an omni list (referenceGain-based), both
/// already de-duped by part name and sorted for the GUI dropdown.</summary>
public sealed class LoadedAntennaCatalog
{
    public List<AntennaModel> Dishes { get; } = new();
    public List<AntennaModel> Omnis { get; } = new();
    public int FilesScanned { get; set; }
    public int PartsFound { get; set; }
    public int TitlesFound { get; set; }
}

/// <summary>Scans a KSP <c>GameData</c> tree for RealAntennas part patches and produces an
/// up-to-date antenna catalog. Source of truth: <c>GameData/RealAntennas/Parts/*.cfg</c>
/// (RA's own ModuleManager patches against stock + every supported mod). A second pass over
/// the whole tree gathers <c>PART { name title }</c> entries so each antenna gets its in-
/// editor display name rather than its bare part name.
///
/// Limitations: only <c>:NEEDS[...]</c> and the <c>@PART[a|b|...]</c> name selector are
/// honoured. <c>:FOR / :FIRST / :BEFORE / :AFTER / :HAS</c> are ignored — the patch is
/// applied if <c>:NEEDS</c> resolves; later patches overwrite earlier ones (last-wins).
/// Math operators (<c>*=</c>, <c>+=</c>) are not interpreted; the loader takes the literal
/// value following <c>=</c>.</summary>
public static class AntennaCatalogLoader
{
    /// <summary>Entry point. <paramref name="gameDataPath"/> is the absolute path to the
    /// KSP <c>GameData</c> directory. Returns an empty catalog if RA isn't installed there.
    /// Safe to call on a missing/wrong path — won't throw.</summary>
    public static LoadedAntennaCatalog LoadFromGameData(string gameDataPath)
    {
        var result = new LoadedAntennaCatalog();
        if (string.IsNullOrEmpty(gameDataPath) || !Directory.Exists(gameDataPath)) return result;
        string partsDir = Path.Combine(gameDataPath, "RealAntennas", "Parts");
        if (!Directory.Exists(partsDir)) return result;

        var hasMod = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        bool ModInstalled(string mod)
        {
            if (hasMod.TryGetValue(mod, out var v)) return v;
            v = Directory.Exists(Path.Combine(gameDataPath, mod));
            hasMod[mod] = v;
            return v;
        }

        // Title-merge + localization pass. One walk of GameData/**/*.cfg gathers both
        // PART { name title } maps AND Localization { en-us { #LOC_X = Y } } maps so each
        // antenna ends up with its in-editor display name (resolved through localization
        // when the title field is a #LOC_X key).
        BuildTitleAndLocMaps(gameDataPath, out var titles, out var loc);
        result.TitlesFound = titles.Count;
        // After loc is known, resolve any title that's a #LOC_* key. Falls back to the raw
        // key if unresolved (the loc file might be missing or use a different language).
        var resolvedTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in titles)
        {
            string t = kv.Value;
            if (t.StartsWith("#LOC_", StringComparison.OrdinalIgnoreCase) &&
                loc.TryGetValue(t, out var resolved))
                t = resolved;
            resolvedTitles[kv.Key] = t;
        }
        titles = resolvedTitles;

        // De-dupe by part name; last patch to declare a given part wins. Two patches with
        // mutually-exclusive :NEEDS (e.g. RTGigaDish2 with !RealismOverhaul vs RealismOverhaul)
        // collapse correctly because only one passes ModInstalled().
        var dishes = new Dictionary<string, AntennaModel>(StringComparer.OrdinalIgnoreCase);
        var omnis = new Dictionary<string, AntennaModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(partsDir, "*.cfg", SearchOption.TopDirectoryOnly))
        {
            CfgNode root;
            try { root = KspCfgReader.ParseFile(path); }
            catch { continue; }
            result.FilesScanned++;

            foreach (var c in root.Children)
            {
                if (!TryParsePartPatchHeader(c.Name, out var partNames, out var needs)) continue;
                if (!CheckNeeds(needs, ModInstalled)) continue;

                var raMod = FindRealAntennaModule(c);
                if (raMod == null) continue;

                double? diameter = ParseValStripMm(raMod, "antennaDiameter");
                double? refGain  = ParseValStripMm(raMod, "referenceGain");

                foreach (var pn in partNames)
                {
                    if (pn.Contains('*')) continue; // skip wildcard patches; titles unresolvable
                    string title = titles.TryGetValue(pn, out var t) ? t : pn;
                    result.PartsFound++;
                    if (diameter.HasValue)
                        dishes[pn] = new AntennaModel(title, diameter.Value, pn);
                    else if (refGain.HasValue)
                        omnis[pn] = new AntennaModel(title, 0, pn, IsOmni: true, GainDbi: refGain.Value);
                }
            }
        }

        result.Dishes.AddRange(dishes.Values.OrderBy(a => a.DiameterM));
        result.Omnis.AddRange(omnis.Values.OrderBy(a => a.GainDbi));
        return result;
    }

    /// <summary>Parses the raw block name (e.g.
    /// <c>"@PART[a|b]:HAS[!MODULE[ModuleRealAntenna]]:FOR[RealAntennas]:NEEDS[!RealismOverhaul]"</c>)
    /// into part names and :NEEDS clauses. Returns false if it isn't a <c>@PART[...]</c>
    /// patch.</summary>
    static bool TryParsePartPatchHeader(string raw, out List<string> partNames, out List<string> needs)
    {
        partNames = new List<string>();
        needs = new List<string>();
        if (raw == null || !raw.StartsWith("@PART[", StringComparison.OrdinalIgnoreCase)) return false;

        // Extract the bracketed selector after @PART. Brackets nest (e.g. :HAS[!MODULE[…]]),
        // so track depth and grab the first balanced [...] following @PART.
        int p = "@PART".Length;
        if (p >= raw.Length || raw[p] != '[') return false;
        int close = FindMatchingBracket(raw, p);
        if (close < 0) return false;
        string selector = raw.Substring(p + 1, close - p - 1);
        foreach (var name in selector.Split('|'))
        {
            string n = name.Trim();
            if (n.Length > 0) partNames.Add(n);
        }
        if (partNames.Count == 0) return false;

        // Walk remaining tail for ":NEEDS[…]" segments.
        int idx = close + 1;
        while (idx < raw.Length)
        {
            if (raw[idx] != ':') { idx++; continue; }
            const string needsTag = ":NEEDS[";
            if (idx + needsTag.Length <= raw.Length &&
                raw.Substring(idx, needsTag.Length).Equals(needsTag, StringComparison.OrdinalIgnoreCase))
            {
                int nClose = FindMatchingBracket(raw, idx + needsTag.Length - 1);
                if (nClose < 0) break;
                needs.Add(raw.Substring(idx + needsTag.Length, nClose - idx - needsTag.Length));
                idx = nClose + 1;
            }
            else
            {
                idx++;
            }
        }
        return true;
    }

    static int FindMatchingBracket(string s, int openIdx)
    {
        int depth = 0;
        for (int i = openIdx; i < s.Length; i++)
        {
            if (s[i] == '[') depth++;
            else if (s[i] == ']') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    /// <summary>Evaluate every <c>:NEEDS[...]</c> clause; all must pass. Supports
    /// <c>!mod</c> negation and <c>a|b</c> OR within one clause. Doesn't support <c>&amp;</c>
    /// AND (rare in RA patches; treated as part of the token).</summary>
    static bool CheckNeeds(List<string> needs, Func<string, bool> installed)
    {
        foreach (var clause in needs)
        {
            bool anyMatch = false;
            foreach (var token in clause.Split('|'))
            {
                string t = token.Trim();
                if (t.Length == 0) continue;
                bool negate = t.StartsWith("!");
                if (negate) t = t.Substring(1);
                bool ok = installed(t);
                if (negate) ok = !ok;
                if (ok) { anyMatch = true; break; }
            }
            if (!anyMatch) return false;
        }
        return true;
    }

    /// <summary>Find the <c>MODULE[ModuleRealAntenna]</c> child within a patch body. Both the
    /// selector form (<c>%MODULE[ModuleRealAntenna]</c>) and the rare bare-MODULE-with-inner-
    /// <c>name = ModuleRealAntenna</c> form are recognised.</summary>
    static CfgNode? FindRealAntennaModule(CfgNode patchNode)
    {
        foreach (var sub in patchNode.Children)
        {
            string nm = StripMmOpPrefix(sub.Name);
            if (nm.StartsWith("MODULE[ModuleRealAntenna]", StringComparison.OrdinalIgnoreCase))
                return sub;
            if (nm.Equals("MODULE", StringComparison.OrdinalIgnoreCase))
            {
                string? inner = GetValueStripMm(sub, "name");
                if (string.Equals(inner, "ModuleRealAntenna", StringComparison.OrdinalIgnoreCase))
                    return sub;
            }
        }
        return null;
    }

    static string StripMmOpPrefix(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        char c = s[0];
        if (c == '@' || c == '%' || c == '!' || c == '+' || c == '-' || c == '$' || c == '*' || c == '&')
            return s.Substring(1);
        return s;
    }

    /// <summary>Equivalent to <c>node.GetValue(key)</c> but also matches MM-prefixed keys
    /// (<c>%antennaDiameter = 1.22</c> matches a lookup of <c>antennaDiameter</c>).</summary>
    static string? GetValueStripMm(CfgNode node, string key)
    {
        for (int i = 0; i < node.Values.Count; i++)
        {
            string k = StripMmOpPrefix(node.Values[i].K);
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                return node.Values[i].V;
        }
        return null;
    }

    static double? ParseValStripMm(CfgNode node, string key)
    {
        var v = GetValueStripMm(node, key);
        return v == null ? null : KspCfgReader.ParseDouble(v);
    }

    /// <summary>Walk every cfg under <paramref name="gameDataPath"/>. Collects two maps:
    ///   - part name → title (from PART blocks),
    ///   - localization key → localized string (from Localization/en-us blocks).
    /// Part titles often contain <c>#LOC_X</c> keys; the caller substitutes them via the
    /// localization map. Tolerant — unparseable files are skipped silently.</summary>
    static void BuildTitleAndLocMaps(string gameDataPath,
                                      out Dictionary<string, string> titles,
                                      out Dictionary<string, string> loc)
    {
        titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        loc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> Files()
        {
            // EnumerateFiles on the whole tree can hit junctions / inaccessible folders; let
            // each top-level mod fail independently rather than abandoning the whole scan.
            foreach (var dir in SafeEnumerateDirectories(gameDataPath))
            {
                foreach (var f in SafeEnumerateFiles(dir, "*.cfg"))
                    yield return f;
            }
            foreach (var f in SafeEnumerateFiles(gameDataPath, "*.cfg", recursive: false))
                yield return f;
        }
        foreach (var path in Files())
        {
            CfgNode root;
            try { root = KspCfgReader.ParseFile(path); }
            catch { continue; }
            CollectPartTitles(root, titles);
            CollectLocalization(root, loc);
        }
    }

    static IEnumerable<string> SafeEnumerateDirectories(string root)
    {
        string[] dirs;
        try { dirs = Directory.GetDirectories(root); }
        catch { yield break; }
        foreach (var d in dirs) yield return d;
    }

    static IEnumerable<string> SafeEnumerateFiles(string root, string pattern, bool recursive = true)
    {
        IEnumerable<string> files;
        try
        {
            files = recursive
                ? Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
                : Directory.EnumerateFiles(root, pattern, SearchOption.TopDirectoryOnly);
        }
        catch { yield break; }
        foreach (var f in files) yield return f;
    }

    static void CollectPartTitles(CfgNode node, Dictionary<string, string> map)
    {
        foreach (var child in node.Children)
        {
            // Bare PART blocks only — skip @PART[…] / +PART / !PART patches. Their target
            // already has a base cfg somewhere else in the tree.
            if (string.Equals(child.Name, "PART", StringComparison.OrdinalIgnoreCase))
            {
                string? n = child.GetValue("name");
                string? t = child.GetValue("title");
                if (!string.IsNullOrEmpty(n) && !string.IsNullOrEmpty(t))
                    map[n!] = t!;
            }
            CollectPartTitles(child, map);
        }
    }

    /// <summary>Collect every <c>#LOC_X = display string</c> entry from
    /// <c>Localization { en-us { … } }</c> blocks. en-us takes priority; other languages
    /// fill in only when en-us has no entry for a given key (covers mods that only ship a
    /// non-English language file). Ignores keys outside a recognised language section.</summary>
    static void CollectLocalization(CfgNode node, Dictionary<string, string> map)
    {
        foreach (var child in node.Children)
        {
            if (string.Equals(child.Name, "Localization", StringComparison.OrdinalIgnoreCase))
            {
                // English first so it wins over later non-English entries with the same key.
                CfgNode? en = child.Children.FirstOrDefault(
                    n => string.Equals(n.Name, "en-us", StringComparison.OrdinalIgnoreCase));
                if (en != null)
                {
                    foreach (var (k, v) in en.Values)
                        if (k.StartsWith("#", StringComparison.Ordinal) && !map.ContainsKey(k))
                            map[k] = v;
                }
                // Fall-throughs for non-English mods (e.g. zh-cn only).
                foreach (var lang in child.Children)
                {
                    if (lang == en) continue;
                    foreach (var (k, v) in lang.Values)
                        if (k.StartsWith("#", StringComparison.Ordinal) && !map.ContainsKey(k))
                            map[k] = v;
                }
            }
            CollectLocalization(child, map);
        }
    }
}
