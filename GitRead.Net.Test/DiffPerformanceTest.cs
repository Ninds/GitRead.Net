using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitRead.Net.Data;
using Xunit;
using static GitRead.Net.RepositoryAnalyzer;

namespace GitRead.Net.Test
{
    public class DiffPerformanceTest
    {
        [Fact]
        public async Task PerformanceTest()
        {
            int comparisons = 600;
            var (content1,content2) = await Prepare(comparisons);
            int repeatCount = 25;
            double total = 0;
            foreach (int repeat in Enumerable.Range(0, repeatCount))
            {
                //TestContext.Progress.Write(repeat);
                Stopwatch watch = Stopwatch.StartNew();
                for (int i = 0; i < comparisons; i++)
                {
                    DiffGenerator.GetLinesChanged(content1[0], content2[0]);
                }
                watch.Stop();
                total += watch.ElapsedMilliseconds;
            }
            Console.WriteLine($"Average {total / repeatCount}ms");
            //TestContext.Progress.WriteLine($"Average {total / repeatCount}ms");
        }

        private async Task<(string[], string[])> Prepare(int comparisons)
        {
            string repoDir = TestUtils.ExtractZippedRepo("vcpkg.git");
            RepositoryAnalyzer repositoryAnalyzer = new RepositoryAnalyzer(repoDir);
            RepositoryReader repositoryReader = new RepositoryReader(repoDir);
            var content1 = new string[comparisons];
            var content2 = new string[comparisons];
            int i = 0;
            var commits = await repositoryAnalyzer.GetCommits();
            foreach (Commit commit in commits.Where(x => x.Parents.Any()))
            {
                Dictionary<string, PathHashMode> current = repositoryAnalyzer.GetPathAndHashForFiles(commit.Hash).ToDictionary(x => x.Path);
                Dictionary<string, PathHashMode> parent = repositoryAnalyzer.GetPathAndHashForFiles(commit.Parents[0]).ToDictionary(x => x.Path);
                foreach ((string hash1, string hash2) in current.Keys.Intersect(parent.Keys).Select(x => (current[x].Hash, parent[x].Hash)))
                {
                    if (hash1 != hash2)
                    {
                        content1[i] = await repositoryReader.ReadBlob(hash1);
                        content2[i] = await repositoryReader.ReadBlob(hash2);
                        i++;
                    }
                    if (i == comparisons)
                    {
                        break;
                    }
                }
                if (i == comparisons)
                {
                    break;
                }
            }
            return (content1, content2);
        }
    }
}
