using GitRead.Net.Data;
using System;
using System.Collections.Generic;
using Xunit;

namespace GitRead.Net.Test
{
    
    public class RepositoryReaderTests
    {
        [Fact]
        public void Test01ReadLooseBlob()
        {
            string repoDir = TestUtils.ExtractZippedRepo("TestRepo01");
            RepositoryReader reader = new RepositoryReader(repoDir);
            string res = reader.ReadBlob("d670460b4b4aece5915caf5c68d12f560a9fe3e4");
            Assert.Equal("test content\n", res);
        }

        [Fact]
        public void Test02ReadCommit()
        {
            string repoDir = TestUtils.ExtractZippedRepo("TestRepo02");
            RepositoryReader reader = new RepositoryReader(repoDir);
            string hash = reader.ReadBranch("master");
            Commit commit = reader.ReadCommit(hash);
            Assert.Equal("ce2d3a85f185830a19e84d404155bf9847ede8b8", commit.Tree,true);
        }

        [Fact]
        public void Test02ReadTree()
        {
            string repoDir = TestUtils.ExtractZippedRepo("TestRepo02");
            RepositoryReader reader = new RepositoryReader(repoDir);
            IReadOnlyList<TreeEntry> res = reader.ReadTree("ce2d3a85f185830a19e84d404155bf9847ede8b8");
            Assert.Equal(1, res.Count);
            Assert.Equal("31d6d2184fe8deab8e52bd9581d67f35d4ecd5ca", res[0].Hash,true);
            Assert.Equal("mydocument.txt", res[0].Name);
            Assert.Equal(TreeEntryMode.RegularNonExecutableFile, res[0].Mode);
        }

        [Fact]
        public void Test02ReadLooseBlob()
        {
            string repoDir = TestUtils.ExtractZippedRepo("TestRepo02");
            RepositoryReader reader = new RepositoryReader(repoDir);
            string res = reader.ReadBlob("31d6d2184fe8deab8e52bd9581d67f35d4ecd5ca");
            Assert.Equal("abc xyz", res);
        }

        [Fact]
        public void TestCsharplangReadPackedRef()
        {
            string repoDir = TestUtils.ExtractZippedRepo("csharplang.git");
            RepositoryReader reader = new RepositoryReader(repoDir);
            string res = reader.ReadBranch("master");
            Assert.Equal("411106b0108a37789ed3d53fd781acf8f75ef97b", res);
        }

        [Fact]
        public void TestCsharplangReadIndex()
        {
            string repoDir = TestUtils.ExtractZippedRepo("csharplang.git");
            PackIndexReader reader = new PackIndexReader(repoDir);
            long packFileOffset = reader.ReadIndex("pack-dae4b1886286da035b337f24ab5b707ad18d8a3c", "411106b0108a37789ed3d53fd781acf8f75ef97b");
            Assert.Equal(744249, packFileOffset);
        }

        [Fact]
        public void TestCsharplangReadIndexLast()
        {
            //This test ensures the edge case of getting the offset for the last hash in the index works.
            string repoDir = TestUtils.ExtractZippedRepo("csharplang.git");
            PackIndexReader reader = new PackIndexReader(repoDir);
            long packFileOffset = reader.ReadIndex("pack-dae4b1886286da035b337f24ab5b707ad18d8a3c", "FFE711AA1535AF6FF434CBBCC50E1D3B1B9FDA82");
            Assert.Equal(300872, packFileOffset);
        }

        [Fact]
        public void TestCsharplangReadPackfile()
        {
            string repoDir = TestUtils.ExtractZippedRepo("csharplang.git");
            RepositoryReader reader = new RepositoryReader(repoDir);
            string hash = "411106b0108a37789ed3d53fd781acf8f75ef97b";
            Commit res = reader.ReadCommit(hash);
            Assert.Equal("Add design notes\n", res.Message);
            Assert.Equal("1af7239766b45f2c85f422a99867919ca9e1e935", res.Tree);
            Assert.Equal("Mads Torgersen", res.Author);
            Assert.Equal("mads.torgersen@microsoft.com", res.EmailAddress);
            Assert.Equal(res.Timestamp, new DateTime(2017,8,9,0,17,9));
            Assert.Equal("-0700", res.TimeZoneOffset);
        }

        [Fact]
        public void TestCsharplangReadTree()
        {
            string repoDir = TestUtils.ExtractZippedRepo("csharplang.git");
            RepositoryReader reader = new RepositoryReader(repoDir);
            string hash = "1af7239766b45f2c85f422a99867919ca9e1e935";
            IReadOnlyList<TreeEntry> res = reader.ReadTree(hash);
            Assert.Equal(6, res.Count);
            Assert.Equal("176A458F94E0EA5272CE67C36BF30B6BE9CAF623", res[0].Hash);
            Assert.Equal(TreeEntryMode.RegularNonExecutableFile, res[0].Mode);
            Assert.Equal(".gitattributes", res[0].Name);
            Assert.Equal("B00C0CD41F02E6CD62C292B00F25E26A3AC7E64F", res[3].Hash);
            Assert.Equal(TreeEntryMode.Directory, res[3].Mode);
            Assert.Equal("meetings", res[3].Name);
        }

        [Fact]
        public void TestCsharplangReadTreeDelta()
        {
            string repoDir = TestUtils.ExtractZippedRepo("csharplang.git");
            RepositoryReader reader = new RepositoryReader(repoDir);
            string hash = "ba2a7c63986f13c2f554c32353a9a69ff6292106";
            IReadOnlyList<TreeEntry> res = reader.ReadTree(hash);
            Assert.Equal(31, res.Count);
        }
    }
}