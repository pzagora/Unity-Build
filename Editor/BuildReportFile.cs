using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Build
{
    /// <summary>
    /// Writes a JSON summary of a player build for CI: target, result, size, duration and the largest source assets by
    /// packed size, e.g. to track size between builds.
    /// </summary>
    internal static class BuildReportFile
    {
        private const int LARGEST_ASSET_COUNT = 25;

        public static void Write(BuildReport report, string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var summary = report.summary;
            var contents = new Contents
            {
                target = summary.platform.ToString(),
                result = summary.result.ToString(),
                outputPath = summary.outputPath,
                totalSize = (long)summary.totalSize,
                totalSeconds = summary.totalTime.TotalSeconds,
                errors = summary.totalErrors,
                warnings = summary.totalWarnings,
                largestAssets = report.packedAssets
                    .SelectMany(packed => packed.contents)
                    .GroupBy(info => info.sourceAssetPath)
                    .Select(group => new AssetSize { path = group.Key, size = group.Sum(info => (long)info.packedSize) })
                    .OrderByDescending(asset => asset.size)
                    .Take(LARGEST_ASSET_COUNT)
                    .ToList()
            };

            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonUtility.ToJson(contents, true));
            BuildLog.Event($"Build report written to {path}");
        }

        [Serializable]
        private sealed class Contents
        {
            public string target;
            public string result;
            public string outputPath;
            public long totalSize;
            public double totalSeconds;
            public int errors;
            public int warnings;
            public List<AssetSize> largestAssets;
        }

        [Serializable]
        private sealed class AssetSize
        {
            public string path;
            public long size;
        }
    }
}
