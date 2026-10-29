using System;
using System.IO;
using UnityEditor;

namespace Build
{
    /// <summary>
    /// Signs Android builds with a release keystore taken from environment variables, so secrets never appear on the
    /// command line or in the project. Without <see cref="KEYSTORE_VARIABLE"/> the project's own signing settings apply
    /// (Unity's debug keystore unless the project configures one).
    /// </summary>
    internal static class AndroidSigning
    {
        public const string KEYSTORE_VARIABLE = "UNITY_BUILD_KEYSTORE";
        public const string KEYSTORE_PASSWORD_VARIABLE = "UNITY_BUILD_KEYSTORE_PASSWORD";
        public const string KEY_ALIAS_VARIABLE = "UNITY_BUILD_KEY_ALIAS";
        public const string KEY_PASSWORD_VARIABLE = "UNITY_BUILD_KEY_PASSWORD";

        public static void Apply()
        {
            var keystore = Environment.GetEnvironmentVariable(KEYSTORE_VARIABLE);
            if (string.IsNullOrEmpty(keystore))
            {
                BuildLog.Event($"Android signing: project settings ({KEYSTORE_VARIABLE} not set)");
                return;
            }

            if (!File.Exists(keystore))
                throw new FileNotFoundException($"{KEYSTORE_VARIABLE} points to a missing keystore", keystore);

            var keystorePassword = Required(KEYSTORE_PASSWORD_VARIABLE);
            var alias = Required(KEY_ALIAS_VARIABLE);
            var keyPassword = Environment.GetEnvironmentVariable(KEY_PASSWORD_VARIABLE);

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(keyPassword) ? keystorePassword : keyPassword;

            BuildLog.Event($"Android signing: keystore {Path.GetFileName(keystore)}, alias {alias}");
        }

        private static string Required(string variable)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"{variable} is required when {KEYSTORE_VARIABLE} is set");

            return value;
        }
    }
}
