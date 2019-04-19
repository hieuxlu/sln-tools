using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace SolutionTools.Extensions
{
    public static class DirectoryExtensions
    {
        public static List<string> LoadFiles(string basePath, string pattern)
        {
            var directoryInfo = new DirectoryInfo(basePath);
            var matchDirectory = new DirectoryInfoWrapper(directoryInfo);

            var matcher = new Matcher();
            matcher.AddInclude(pattern);

            var matchResult = matcher.Execute(matchDirectory);

            return matchResult.Files.Select(item => item.Path).ToList();
        }
    }
}
