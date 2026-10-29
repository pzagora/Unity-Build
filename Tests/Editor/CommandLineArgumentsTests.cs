using NUnit.Framework;

namespace Build.Tests
{
    public class CommandLineArgumentsTests
    {
        private static readonly string[] Arguments =
        {
            "Unity", "-batchmode", "-outputPath", "Builds/Game.apk", "-development", "-buildNumber", "42", "-defines", "-quit"
        };

        [SetUp]
        public void SetUp()
            => CommandLineArguments.OverrideForTests(Arguments);

        [TearDown]
        public void TearDown()
            => CommandLineArguments.OverrideForTests(null);

        [Test]
        public void Get_ReturnsTheValueAfterTheName()
            => Assert.AreEqual("Builds/Game.apk", CommandLineArguments.Get("-outputPath"));

        [Test]
        public void Get_ReturnsNullWhenTheNextArgumentIsAFlag()
            => Assert.IsNull(CommandLineArguments.Get("-defines"));

        [Test]
        public void Get_ReturnsNullForAMissingOrTrailingArgument()
        {
            Assert.IsNull(CommandLineArguments.Get("-bundleVersion"));
            Assert.IsNull(CommandLineArguments.Get("-quit"));
        }

        [Test]
        public void Has_FindsFlags()
        {
            Assert.IsTrue(CommandLineArguments.Has("-development"));
            Assert.IsFalse(CommandLineArguments.Has("-buildAddressables"));
        }

        [Test]
        public void Require_ThrowsForAMissingValue()
            => Assert.Throws<System.ArgumentException>(() => CommandLineArguments.Require("-bundleVersion"));
    }
}
