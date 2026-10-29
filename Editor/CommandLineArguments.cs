using System;
using System.Collections.Generic;
using System.Linq;

namespace Build
{
    /// <summary>
    /// Reads the Editor's command-line arguments: flags (<c>-development</c>) and named values
    /// (<c>-outputPath Builds/Game.apk</c>). Build hooks can use it for their own arguments.
    /// </summary>
    public static class CommandLineArguments
    {
        private const string PREFIX = "-";

        private static IReadOnlyList<string> _override;

        private static IReadOnlyList<string> Arguments => _override ?? Environment.GetCommandLineArgs();

        /// <summary><c>true</c> when the flag (e.g. <c>-development</c>) was passed.</summary>
        public static bool Has(string name)
            => Arguments.Contains(name);

        /// <summary>The value after <paramref name="name"/>, or <c>null</c> when it's missing or followed by another flag.</summary>
        public static string Get(string name)
        {
            var arguments = Arguments;
            for (var i = 0; i < arguments.Count - 1; i++)
            {
                if (arguments[i] != name)
                    continue;

                var value = arguments[i + 1];
                return value.StartsWith(PREFIX, StringComparison.Ordinal) ? null : value;
            }

            return null;
        }

        /// <summary>The value after <paramref name="name"/>; throws when it's missing.</summary>
        public static string Require(string name)
            => Get(name) ?? throw new ArgumentException($"{name} <value> is required");

        internal static void OverrideForTests(IReadOnlyList<string> arguments)
            => _override = arguments;
    }
}
