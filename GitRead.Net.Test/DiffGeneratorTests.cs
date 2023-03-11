using System.Collections.Generic;
using Xunit;

namespace GitRead.Net.Test
{
 
    public class DiffGeneratorTests
    {
        [Fact]
        public void TestOverlap()
        {
            string text1 = @"
                az
                az
                ab
                ac";
            string text2 = @"
                az
                ab
                ac";
            (int added, int deleted) = DiffGenerator.GetLinesChanged(text1, text2);
            Assert.Equal(0, added);
            Assert.Equal(1, deleted);
        }

        [Fact]
        public void TestDiff01()
        {
            Dictionary<string, string> files = TestUtils.ExtractZippedFiles("Files01");
            (int added, int deleted) = DiffGenerator.GetLinesChanged(files["JsonSerializerTest_4793c7b.cs"], files["JsonSerializerTest_2368a8e.cs"]);
            Assert.Equal(23, added);
            Assert.Equal(0, deleted);
        }
    }
}