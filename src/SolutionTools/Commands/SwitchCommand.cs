using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Mono.Options;
using SolutionTools.Extensions;
using SolutionTools.Switcher;
using SolutionTools.Utils;

namespace SolutionTools.Commands
{
    public static class SwitchCommand
    {
        public static void Run(string[] args)
        {
            string solutionFile = string.Empty;
            string pattern = string.Empty;
            bool overwrite = false;
            bool help = false;
            bool inSolutionProjectsOnly = false;

            var options = new OptionSet
            {
                {"s|solution=", "the path to solution file.", s => solutionFile = s},
                { "p|pattern=", "the path to project files", p => pattern = p},
                { "i|in", "include only projects inside source solution file", i => inSolutionProjectsOnly = i != null},
                { "w|write", "overwrite project file(s)", w => overwrite = w != null},
                { "h|help", "show options", h => help = h != null},
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                // output some error message
                Logger.Error(e.Message);
                Logger.Error("Try `--help' for more information.");
                return;
            }

            if (help)
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var switcher = new ReferenceSwitcher();
            switcher.Execute(solutionFile, pattern, overwrite, inSolutionProjectsOnly);
        }
    }
}
