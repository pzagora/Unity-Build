using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace Build.Tests
{
    public sealed class AddressablesContentTests
    {
        private const BuildTarget Target = BuildTarget.NoTarget;
        private string _contentPath;
        private bool _createdDirectory;

        [SetUp]
        public void SetUp()
        {
            _contentPath = Path.Combine("Library/com.unity.addressables/aa", Target.ToString());
            Assert.IsFalse(Directory.Exists(_contentPath), $"Test path unexpectedly exists: {_contentPath}");
        }

        [TearDown]
        public void TearDown()
        {
            if (_createdDirectory && Directory.Exists(_contentPath))
                Directory.Delete(_contentPath, recursive: true);
        }

        [Test]
        public void Require_FailsWhenNoContentWasBuilt()
        {
            var exception = Assert.Throws<BuildFailedException>(() => AddressablesContent.Require(Target));
            StringAssert.Contains("No Addressables content", exception.Message);
            StringAssert.Contains(CommandLineBuild.BUILD_ADDRESSABLES_ARGUMENT, exception.Message);
        }

        [Test]
        public void Require_AcceptsExistingContentDirectory()
        {
            Directory.CreateDirectory(_contentPath);
            _createdDirectory = true;

            Assert.DoesNotThrow(() => AddressablesContent.Require(Target));
        }
    }
}
