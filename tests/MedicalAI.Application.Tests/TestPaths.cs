using System;
using System.IO;

namespace MedicalAI.Application.Tests
{
    internal static class TestPaths
    {
        private static readonly Lazy<string> RepoRootLazy = new(() =>
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")));

        public static string RepoRoot => RepoRootLazy.Value;

        public static string Samples(string fileName) =>
            Path.Combine(RepoRoot, "datasets", "samples", fileName);
    }
}
