using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;

namespace Build
{
    internal static class ScriptingDefines
    {
        private const string SEPARATOR = ";";
        private static readonly char[] InputSeparators = { ';', ',', ' ', '\t', '\n', '\r' };
        private static readonly Regex Symbol = new("^[A-Za-z_][A-Za-z0-9_]*$");

        public static IReadOnlyList<string> Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            var symbols = value.Split(InputSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(symbol => symbol.Trim())
                .Where(symbol => symbol.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var invalid = symbols.FirstOrDefault(symbol => !Symbol.IsMatch(symbol));
            if (invalid != null)
                throw new ArgumentException($"'{invalid}' isn't a valid scripting define symbol");

            return symbols;
        }

        public static string Merge(string existing, IEnumerable<string> extra)
        {
            var current = Parse(existing).ToList();
            current.AddRange(extra.Where(symbol => !current.Contains(symbol, StringComparer.Ordinal)));
            return string.Join(SEPARATOR, current);
        }

        /// <summary>Adds the symbols to the target's define symbols and saves the project settings.</summary>
        public static string Apply(NamedBuildTarget target, IReadOnlyList<string> extra)
        {
            var merged = Merge(PlayerSettings.GetScriptingDefineSymbols(target), extra);
            PlayerSettings.SetScriptingDefineSymbols(target, merged);
            AssetDatabase.SaveAssets();
            return merged;
        }
    }
}
