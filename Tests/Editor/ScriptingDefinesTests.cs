using System;
using NUnit.Framework;

namespace Build.Tests
{
    public class ScriptingDefinesTests
    {
        [Test]
        public void Parse_AcceptsCommonSeparatorsAndDropsDuplicates()
            => CollectionAssert.AreEqual(new[] { "A", "B_2", "_C" }, ScriptingDefines.Parse(" A;B_2, _C A\n"));

        [Test]
        public void Parse_ReturnsNothingForEmptyInput()
        {
            CollectionAssert.IsEmpty(ScriptingDefines.Parse(null));
            CollectionAssert.IsEmpty(ScriptingDefines.Parse("  ;, "));
        }

        [Test]
        public void Parse_RejectsInvalidSymbols()
            => Assert.Throws<ArgumentException>(() => ScriptingDefines.Parse("VALID;2INVALID"));

        [Test]
        public void Merge_AppendsOnlyMissingSymbols()
            => Assert.AreEqual("EXISTING;OTHER;NEW", ScriptingDefines.Merge("EXISTING;OTHER", new[] { "OTHER", "NEW" }));

        [Test]
        public void Merge_WorksWithoutExistingSymbols()
            => Assert.AreEqual("NEW", ScriptingDefines.Merge(string.Empty, new[] { "NEW" }));
    }
}
