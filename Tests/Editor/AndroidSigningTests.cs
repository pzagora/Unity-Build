using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Build.Tests
{
    public sealed class AndroidSigningTests
    {
        private static readonly string[] Variables =
        {
            AndroidSigning.KEYSTORE_VARIABLE,
            AndroidSigning.KEYSTORE_PASSWORD_VARIABLE,
            AndroidSigning.KEY_ALIAS_VARIABLE,
            AndroidSigning.KEY_PASSWORD_VARIABLE
        };

        private readonly Dictionary<string, string> _environment = new();
        private bool _useCustomKeystore;
        private string _keystoreName;
        private string _keystorePass;
        private string _keyAliasName;
        private string _keyAliasPass;
        private string _temporaryKeystore;

        [SetUp]
        public void SetUp()
        {
            foreach (var variable in Variables)
            {
                _environment[variable] = Environment.GetEnvironmentVariable(variable);
                Environment.SetEnvironmentVariable(variable, null);
            }

            _useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            _keystoreName = PlayerSettings.Android.keystoreName;
            _keystorePass = PlayerSettings.Android.keystorePass;
            _keyAliasName = PlayerSettings.Android.keyaliasName;
            _keyAliasPass = PlayerSettings.Android.keyaliasPass;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var variable in Variables)
                Environment.SetEnvironmentVariable(variable, _environment[variable]);

            PlayerSettings.Android.useCustomKeystore = _useCustomKeystore;
            PlayerSettings.Android.keystoreName = _keystoreName;
            PlayerSettings.Android.keystorePass = _keystorePass;
            PlayerSettings.Android.keyaliasName = _keyAliasName;
            PlayerSettings.Android.keyaliasPass = _keyAliasPass;

            if (!string.IsNullOrEmpty(_temporaryKeystore) && File.Exists(_temporaryKeystore))
                File.Delete(_temporaryKeystore);
        }

        [Test]
        public void Apply_LeavesProjectSigningUntouchedWithoutEnvironmentKeystore()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = "project.keystore";

            AndroidSigning.Apply();

            Assert.IsFalse(PlayerSettings.Android.useCustomKeystore);
            Assert.AreEqual("project.keystore", PlayerSettings.Android.keystoreName);
        }

        [Test]
        public void Apply_RejectsMissingKeystoreFile()
        {
            Environment.SetEnvironmentVariable(AndroidSigning.KEYSTORE_VARIABLE, Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".keystore"));

            Assert.Throws<FileNotFoundException>(() => AndroidSigning.Apply());
        }

        [TestCase(AndroidSigning.KEYSTORE_PASSWORD_VARIABLE)]
        [TestCase(AndroidSigning.KEY_ALIAS_VARIABLE)]
        public void Apply_RequiresSigningVariables(string missingVariable)
        {
            SetValidEnvironment();
            Environment.SetEnvironmentVariable(missingVariable, null);

            var exception = Assert.Throws<InvalidOperationException>(() => AndroidSigning.Apply());
            StringAssert.Contains(missingVariable, exception.Message);
        }

        [Test]
        public void Apply_ConfiguresCustomKeystoreAndFallsBackToKeystorePassword()
        {
            SetValidEnvironment();

            AndroidSigning.Apply();

            Assert.IsTrue(PlayerSettings.Android.useCustomKeystore);
            Assert.AreEqual(Path.GetFullPath(_temporaryKeystore), Path.GetFullPath(PlayerSettings.Android.keystoreName));
            Assert.AreEqual("store-secret", PlayerSettings.Android.keystorePass);
            Assert.AreEqual("release", PlayerSettings.Android.keyaliasName);
            Assert.AreEqual("store-secret", PlayerSettings.Android.keyaliasPass);
        }

        [Test]
        public void Apply_UsesExplicitKeyPassword()
        {
            SetValidEnvironment();
            Environment.SetEnvironmentVariable(AndroidSigning.KEY_PASSWORD_VARIABLE, "key-secret");

            AndroidSigning.Apply();

            Assert.AreEqual("key-secret", PlayerSettings.Android.keyaliasPass);
        }

        private void SetValidEnvironment()
        {
            _temporaryKeystore = Path.GetTempFileName();
            Environment.SetEnvironmentVariable(AndroidSigning.KEYSTORE_VARIABLE, _temporaryKeystore);
            Environment.SetEnvironmentVariable(AndroidSigning.KEYSTORE_PASSWORD_VARIABLE, "store-secret");
            Environment.SetEnvironmentVariable(AndroidSigning.KEY_ALIAS_VARIABLE, "release");
        }
    }
}
