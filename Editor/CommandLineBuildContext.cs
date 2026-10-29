using UnityEditor;
using UnityEditor.Build;

namespace Build
{
    /// <summary>The player build <see cref="CommandLineBuild.Build"/> is about to run, as seen by build hooks.</summary>
    public sealed class CommandLineBuildContext
    {
        public CommandLineBuildContext(BuildTarget target, string outputPath, int? buildNumber, bool development, BuildPlayerOptions options)
        {
            Target = target;
            NamedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
            OutputPath = outputPath;
            BuildNumber = buildNumber;
            Development = development;
            Options = options;
        }

        public BuildTarget Target { get; }
        public NamedBuildTarget NamedTarget { get; }
        public string OutputPath { get; }

        /// <summary>The <c>-buildNumber</c> argument, already applied to the version code / build number.</summary>
        public int? BuildNumber { get; }

        public bool Development { get; }

        /// <summary>What is passed to <see cref="BuildPipeline.BuildPlayer(BuildPlayerOptions)"/>; hooks may change it.</summary>
        public BuildPlayerOptions Options { get; set; }
    }
}
