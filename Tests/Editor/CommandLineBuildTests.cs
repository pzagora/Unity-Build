using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Build.Tests
{
    public sealed class CommandLineBuildTests
    {
        private int _androidBuildNumber;
        private string _iosBuildNumber;
        private string _macBuildNumber;

        [SetUp]
        public void SetUp()
        {
            _androidBuildNumber = PlayerSettings.Android.bundleVersionCode;
            _iosBuildNumber = PlayerSettings.iOS.buildNumber;
            _macBuildNumber = PlayerSettings.macOS.buildNumber;
        }

        [TearDown]
        public void TearDown()
        {
            CommandLineArguments.OverrideForTests(null);
            PlayerSettings.Android.bundleVersionCode = _androidBuildNumber;
            PlayerSettings.iOS.buildNumber = _iosBuildNumber;
            PlayerSettings.macOS.buildNumber = _macBuildNumber;
        }

        [Test]
        public void ApplyBuildNumber_UpdatesEveryPlatformBuildNumber()
        {
            CommandLineArguments.OverrideForTests(new[] { "Unity", CommandLineBuild.BUILD_NUMBER_ARGUMENT, "42" });

            var result = InvokePrivate("ApplyBuildNumber");

            Assert.AreEqual(42, result);
            Assert.AreEqual(42, PlayerSettings.Android.bundleVersionCode);
            Assert.AreEqual("42", PlayerSettings.iOS.buildNumber);
            Assert.AreEqual("42", PlayerSettings.macOS.buildNumber);
        }

        [TestCase("0")]
        [TestCase("-1")]
        [TestCase("one")]
        public void ApplyBuildNumber_RejectsInvalidValues(string value)
        {
            CommandLineArguments.OverrideForTests(new[] { "Unity", CommandLineBuild.BUILD_NUMBER_ARGUMENT, value });

            var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivate("ApplyBuildNumber"));
            Assert.IsInstanceOf<ArgumentException>(exception.InnerException);
        }

        [Test]
        public void Hooks_AreOrderedByOrderThenTypeName()
        {
            var hooks = ((System.Collections.Generic.IEnumerable<ICommandLineBuildHook>)InvokePrivate("Hooks"))
                .Where(hook => hook is EarlyHook or LateHook)
                .ToArray();

            Assert.That(hooks.Select(hook => hook.GetType()), Is.EqualTo(new[] { typeof(EarlyHook), typeof(LateHook) }));
        }

        [Test]
        public void Context_ExposesBuildInputsAndAllowsHooksToReplaceOptions()
        {
            var options = new BuildPlayerOptions
            {
                target = BuildTarget.StandaloneWindows64,
                locationPathName = "Builds/Game.exe"
            };
            var context = new CommandLineBuildContext(
                BuildTarget.StandaloneWindows64,
                options.locationPathName,
                buildNumber: 7,
                development: true,
                options);

            Assert.AreEqual(BuildTarget.StandaloneWindows64, context.Target);
            Assert.AreEqual("Builds/Game.exe", context.OutputPath);
            Assert.AreEqual(7, context.BuildNumber);
            Assert.IsTrue(context.Development);

            options.options = BuildOptions.Development;
            context.Options = options;
            Assert.AreEqual(BuildOptions.Development, context.Options.options);
        }

        private static object InvokePrivate(string name)
            => typeof(CommandLineBuild)
                .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, null);
    }

    public sealed class EarlyHook : ICommandLineBuildHook
    {
        public int Order => -1000;
        public void BeforeBuild(CommandLineBuildContext context) { }
        public void AfterBuild(CommandLineBuildContext context, BuildReport report) { }
    }

    public sealed class LateHook : ICommandLineBuildHook
    {
        public int Order => 1000;
        public void BeforeBuild(CommandLineBuildContext context) { }
        public void AfterBuild(CommandLineBuildContext context, BuildReport report) { }
    }
}
