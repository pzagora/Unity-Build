using System.IO;
using UnityEditor;
using UnityEditor.Build;
#if PACKAGE_ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
#endif

namespace Build
{
    internal static class AddressablesContent
    {
        private const string CONTENT_PATH = "Library/com.unity.addressables/aa";
        private const string NOT_INSTALLED = "The project doesn't have the Addressables package (com.unity.addressables).";

        public static void Build()
        {
#if PACKAGE_ADDRESSABLES
            BuildLog.Event("Building Addressables content");
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
                throw new BuildFailedException($"Addressables build failed: {result.Error}");

            BuildLog.Event($"Addressables content built in {result.Duration:0.0} s", true);
#else
            throw new BuildFailedException(NOT_INSTALLED);
#endif
        }

        /// <summary>Fails when a player would be built without the Addressables content it packages (none built yet).</summary>
        public static void Require(BuildTarget target)
        {
#if PACKAGE_ADDRESSABLES
            var contentPath = Path.Combine(CONTENT_PATH, target.ToString());
            if (!Directory.Exists(contentPath))
                throw new BuildFailedException($"No Addressables content in {contentPath}; build it first or pass {CommandLineBuild.BUILD_ADDRESSABLES_ARGUMENT}");

            BuildLog.Event($"Packaging the existing Addressables content from {contentPath}");
#endif
        }
    }
}
