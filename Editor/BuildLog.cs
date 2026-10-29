using UnityEngine;

namespace Build
{
    /// <summary>
    /// Logging for the build package. Routes through the Reporter package (<c>UnityEngine.Report</c>) when it is in the
    /// project (<c>PACKAGE_REPORTER</c>, set by the assembly's version defines) and falls back to <see cref="Debug"/>
    /// otherwise.
    /// </summary>
    internal static class BuildLog
    {
        private const string SOURCE = "Build";
#if !PACKAGE_REPORTER
        private const string FALLBACK_FORMAT = "[" + SOURCE + "] {0}";
#endif

        [HideInCallstack]
        public static void Event(object content, bool? passed = null)
        {
#if PACKAGE_REPORTER
            Report.Event(content, passed, sourceName: SOURCE);
#else
            Debug.Log(string.Format(FALLBACK_FORMAT, content));
#endif
        }

        [HideInCallstack]
        public static void Warning(object content)
        {
#if PACKAGE_REPORTER
            Report.Warning(content, sourceName: SOURCE);
#else
            Debug.LogWarning(string.Format(FALLBACK_FORMAT, content));
#endif
        }
    }
}
