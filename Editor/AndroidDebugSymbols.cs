using System;
#if UNITY_ANDROID
using Unity.Android.Types;
using UnityEditor.Android;
#endif

namespace Build
{
    /// <summary>
    /// Sets the Android debug symbols Unity writes next to the build (<c>*.symbols.zip</c>, uploaded to the store so
    /// native crashes are readable): <c>None</c>, <c>SymbolTable</c> or <c>Full</c>.
    /// </summary>
    internal static class AndroidDebugSymbols
    {
        public static void Apply(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

#if UNITY_ANDROID
            if (!Enum.TryParse<DebugSymbolLevel>(value, true, out var level))
                throw new ArgumentException($"{CommandLineBuild.ANDROID_DEBUG_SYMBOLS_ARGUMENT} must be one of " +
                    $"{string.Join(", ", Enum.GetNames(typeof(DebugSymbolLevel)))}, got '{value}'");

            UserBuildSettings.DebugSymbols.level = level;
            UserBuildSettings.DebugSymbols.format = DebugSymbolFormat.Zip;
            BuildLog.Event($"Android debug symbols: {level}");
#else
            BuildLog.Warning($"{CommandLineBuild.ANDROID_DEBUG_SYMBOLS_ARGUMENT} ignored: the active build target isn't Android");
#endif
        }
    }
}
