using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Build
{
    /// <summary>
    /// Entry points for <c>Unity -batchmode -quit -buildTarget &lt;target&gt; -executeMethod Build.CommandLineBuild.&lt;method&gt;</c>.
    /// Every method works on the active build target and throws on failure, which makes Unity exit with code 1.
    /// See the package README for the arguments.
    /// </summary>
    public static class CommandLineBuild
    {
        public const string OUTPUT_PATH_ARGUMENT = "-outputPath";
        public const string BUILD_NUMBER_ARGUMENT = "-buildNumber";
        public const string BUNDLE_VERSION_ARGUMENT = "-bundleVersion";
        public const string DEVELOPMENT_ARGUMENT = "-development";
        public const string MANAGED_CODE_VARIANT_ARGUMENT = "-managedCodeVariant";
        public const string DEFINES_ARGUMENT = "-defines";
        public const string BUILD_ADDRESSABLES_ARGUMENT = "-buildAddressables";
        public const string ANDROID_DEBUG_SYMBOLS_ARGUMENT = "-androidDebugSymbols";
        public const string BUILD_REPORT_ARGUMENT = "-buildReport";

        private const string APP_BUNDLE_EXTENSION = ".aab";
        private const string VARIANT_SEPARATOR = ", ";
        private const ulong BYTES_PER_MEGABYTE = 1024 * 1024;

        /// <summary>
        /// Builds the player to <c>-outputPath</c> (an <c>.aab</c> path builds an Android App Bundle). Applies, in order:
        /// <c>-defines</c>, <c>-bundleVersion</c>, <c>-buildNumber</c>, <c>-managedCodeVariant</c>, Android signing from
        /// environment variables and <c>-androidDebugSymbols</c>, then builds the Addressables content with <c>-buildAddressables</c> (otherwise requires
        /// existing content when the project uses Addressables), runs the build hooks and builds.
        /// </summary>
        public static void Build()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var outputPath = CommandLineArguments.Require(OUTPUT_PATH_ARGUMENT);

            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
            ApplyDefines(namedTarget);
            ApplyBundleVersion();
            var buildNumber = ApplyBuildNumber();
            ApplyManagedCodeVariant(namedTarget);

            if (target == BuildTarget.Android)
            {
                AndroidSigning.Apply();
                AndroidDebugSymbols.Apply(CommandLineArguments.Get(ANDROID_DEBUG_SYMBOLS_ARGUMENT));
                EditorUserBuildSettings.buildAppBundle = outputPath.EndsWith(APP_BUNDLE_EXTENSION, StringComparison.OrdinalIgnoreCase);
            }

            if (CommandLineArguments.Has(BUILD_ADDRESSABLES_ARGUMENT))
                AddressablesContent.Build();
            else
                AddressablesContent.Require(target);

            var development = CommandLineArguments.Has(DEVELOPMENT_ARGUMENT);
            var context = new CommandLineBuildContext(target, outputPath, buildNumber, development, new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = outputPath,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = development ? BuildOptions.Development : BuildOptions.None
            });

            var hooks = Hooks();
            foreach (var hook in hooks)
                hook.BeforeBuild(context);

            BuildLog.Event($"Building {target}{(development ? " (development)" : string.Empty)} to {context.Options.locationPathName}");
            var report = BuildPipeline.BuildPlayer(context.Options);

            foreach (var hook in hooks)
                hook.AfterBuild(context, report);

            BuildReportFile.Write(report, CommandLineArguments.Get(BUILD_REPORT_ARGUMENT));

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"{target} build {summary.result} with {summary.totalErrors} error(s)");

            BuildLog.Event($"{target} build succeeded in {summary.totalTime:mm\\:ss}: {summary.outputPath} ({summary.totalSize / BYTES_PER_MEGABYTE} MB)", true);
        }

        /// <summary>Builds only the Addressables content (after applying <c>-defines</c> and <c>-managedCodeVariant</c>).</summary>
        public static void BuildAddressables()
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            ApplyDefines(namedTarget);
            ApplyManagedCodeVariant(namedTarget);
            AddressablesContent.Build();
        }

        /// <summary>
        /// Adds <c>-defines</c> to the active target's scripting define symbols and saves them, so a following Editor run
        /// (e.g. <c>-runTests</c>) compiles with them.
        /// </summary>
        public static void ApplyDefines()
            => ApplyDefines(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));

        private static void ApplyDefines(NamedBuildTarget target)
        {
            var symbols = ScriptingDefines.Parse(CommandLineArguments.Get(DEFINES_ARGUMENT));
            if (symbols.Count == 0)
                return;

            BuildLog.Event($"Scripting define symbols ({target.TargetName}): {ScriptingDefines.Apply(target, symbols)}");
        }

        private static void ApplyBundleVersion()
        {
            var version = CommandLineArguments.Get(BUNDLE_VERSION_ARGUMENT);
            if (string.IsNullOrEmpty(version))
                return;

            PlayerSettings.bundleVersion = version;
            BuildLog.Event($"Bundle version {version}");
        }

        private static int? ApplyBuildNumber()
        {
            var value = CommandLineArguments.Get(BUILD_NUMBER_ARGUMENT);
            if (string.IsNullOrEmpty(value))
            {
                if (CommandLineArguments.Has(BUILD_NUMBER_ARGUMENT))
                    throw new ArgumentException($"{BUILD_NUMBER_ARGUMENT} must be followed by a positive whole number");

                return null;
            }

            if (!int.TryParse(value, out var buildNumber) || buildNumber <= 0)
                throw new ArgumentException($"{BUILD_NUMBER_ARGUMENT} must be a positive whole number, got '{value}'");

            PlayerSettings.Android.bundleVersionCode = buildNumber;
            PlayerSettings.iOS.buildNumber = value;
            PlayerSettings.macOS.buildNumber = value;
            BuildLog.Event($"Build number {buildNumber}");
            return buildNumber;
        }

        private static void ApplyManagedCodeVariant(NamedBuildTarget target)
        {
            var value = CommandLineArguments.Get(MANAGED_CODE_VARIANT_ARGUMENT);
            if (string.IsNullOrEmpty(value))
                return;

#if UNITY_6000_6_OR_NEWER
            if (!Enum.TryParse<ManagedCodeVariant>(value, true, out var variant))
                throw new ArgumentException($"{MANAGED_CODE_VARIANT_ARGUMENT} must be one of " +
                    $"{string.Join(VARIANT_SEPARATOR, Enum.GetNames(typeof(ManagedCodeVariant)))}, got '{value}'");

            PlayerSettings.SetManagedCodeVariant(target, variant);
            BuildLog.Event($"Managed code variant {variant}");
#else
            BuildLog.Warning($"{MANAGED_CODE_VARIANT_ARGUMENT} needs Unity 6.6 or newer; ignored");
#endif
        }

        private static IReadOnlyList<ICommandLineBuildHook> Hooks()
            => TypeCache.GetTypesDerivedFrom<ICommandLineBuildHook>()
                .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => (ICommandLineBuildHook)Activator.CreateInstance(type))
                .OrderBy(hook => hook.Order)
                .ThenBy(hook => hook.GetType().FullName, StringComparer.Ordinal)
                .ToList();
    }
}
