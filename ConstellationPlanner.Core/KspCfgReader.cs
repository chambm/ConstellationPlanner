using System;
using System.Collections.Generic;
using System.IO;

namespace ConstellationPlanner.Core
{
    /// <summary>Minimal KSP / ModuleManager cfg parser. Tolerant — does not understand MM
    /// patches (@, !, etc.); just parses bare blocks and key=value pairs into a tree. Used to
    /// pull station coordinates out of RealAntennas + Skopos cfg files.</summary>
    public sealed class CfgNode
    {
        public string Name = "";
        public List<(string K, string V)> Values = new List<(string, string)>();
        public List<CfgNode> Children = new List<CfgNode>();

        /// <summary>First top-level key by name, or null.</summary>
        public string? GetValue(string key)
        {
            for (int i = 0; i < Values.Count; i++)
                if (string.Equals(Values[i].K, key, StringComparison.OrdinalIgnoreCase))
                    return Values[i].V;
            return null;
        }

        /// <summary>All top-level values for a given key, in declaration order. Used for cfg
        /// blocks where the same key repeats — e.g. Skopos's <c>connection { rx = a; rx = b; }</c>
        /// where each <c>rx</c> entry adds a destination.</summary>
        public IEnumerable<string> GetValues(string key)
        {
            for (int i = 0; i < Values.Count; i++)
                if (string.Equals(Values[i].K, key, StringComparison.OrdinalIgnoreCase))
                    yield return Values[i].V;
        }

        /// <summary>Walk subtree (depth-first) yielding every block whose Name matches.
        /// Match is by trimmed prefix before any '[' so MM-patched names like "City2[*]" still
        /// match "City2". Bare nodes (no MM prefix) match by exact compare.</summary>
        public IEnumerable<CfgNode> FindAll(string blockName)
        {
            var stack = new Stack<CfgNode>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var n = stack.Pop();
                foreach (var c in n.Children)
                {
                    if (NameMatches(c.Name, blockName))
                        yield return c;
                    stack.Push(c);
                }
            }
        }

        static bool NameMatches(string nodeName, string target)
        {
            if (string.IsNullOrEmpty(nodeName)) return false;
            // Strip MM ops at start (@, !, %, +, -, $, *, &) so "City2" matches "City2[*]" but
            // not "@City2" — patches edit existing nodes and would double-count.
            char first = nodeName[0];
            if (first == '@' || first == '!' || first == '%' || first == '+' ||
                first == '-' || first == '$' || first == '*' || first == '&')
                return false;
            int bracket = nodeName.IndexOf('[');
            string bare = bracket >= 0 ? nodeName.Substring(0, bracket) : nodeName;
            return string.Equals(bare, target, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class KspCfgReader
    {
        public static CfgNode ParseFile(string path)
        {
            string text = File.ReadAllText(path);
            return Parse(text);
        }

        public static CfgNode Parse(string s)
        {
            int i = 0;
            var root = new CfgNode { Name = "" };
            ParseBlockBody(s, ref i, root, depth: 0);
            return root;
        }

        static void ParseBlockBody(string s, ref int i, CfgNode parent, int depth)
        {
            while (i < s.Length)
            {
                SkipWsAndComments(s, ref i);
                if (i >= s.Length) return;
                if (s[i] == '}')
                {
                    if (depth == 0) { i++; continue; } // stray top-level brace, ignore
                    i++;
                    return;
                }

                int start = i;
                while (i < s.Length && !char.IsWhiteSpace(s[i]) && s[i] != '=' && s[i] != '{' && s[i] != '}')
                    i++;
                if (i == start) { i++; continue; }
                string ident = s.Substring(start, i - start);

                SkipInlineWs(s, ref i);
                // Allow newlines between identifier and { (KSP cfg often does this).
                int peek = i;
                while (peek < s.Length && (s[peek] == '\r' || s[peek] == '\n' || s[peek] == '\t' || s[peek] == ' ')) peek++;
                if (peek < s.Length && (s[peek] == '{' || s[peek] == '='))
                    i = peek;

                if (i < s.Length && s[i] == '=')
                {
                    i++;
                    SkipInlineWs(s, ref i);
                    int vstart = i;
                    while (i < s.Length && s[i] != '\n' && s[i] != '\r') i++;
                    string v = s.Substring(vstart, i - vstart);
                    int comm = v.IndexOf("//");
                    if (comm >= 0) v = v.Substring(0, comm);
                    parent.Values.Add((ident, v.Trim()));
                }
                else if (i < s.Length && s[i] == '{')
                {
                    i++;
                    var child = new CfgNode { Name = ident };
                    ParseBlockBody(s, ref i, child, depth + 1);
                    parent.Children.Add(child);
                }
                // else: bare identifier with no value/block — skip silently
            }
        }

        static void SkipWsAndComments(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
                {
                    while (i < s.Length && s[i] != '\n') i++;
                    continue;
                }
                break;
            }
        }

        static void SkipInlineWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t')) i++;
        }

        public static double? ParseDouble(string? v)
        {
            if (v == null) return null;
            if (double.TryParse(v.Trim(), System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out double d))
                return d;
            return null;
        }
    }
}
