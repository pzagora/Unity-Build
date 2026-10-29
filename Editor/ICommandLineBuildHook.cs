using UnityEditor.Build.Reporting;

namespace Build
{
    /// <summary>
    /// Project-specific steps around <see cref="CommandLineBuild.Build"/>: implement it in any Editor assembly of the
    /// project (with a parameterless constructor) and it's found automatically. Hooks run in ascending <see cref="Order"/>.
    /// Read custom arguments with <see cref="CommandLineArguments"/>.
    /// </summary>
    public interface ICommandLineBuildHook
    {
        int Order { get; }

        /// <summary>Runs after the package's own settings are applied, right before the player is built.</summary>
        void BeforeBuild(CommandLineBuildContext context);

        /// <summary>Runs after the build, whether it succeeded or not (check <paramref name="report"/>).</summary>
        void AfterBuild(CommandLineBuildContext context, BuildReport report);
    }
}
