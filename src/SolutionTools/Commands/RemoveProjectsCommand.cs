using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using SolutionTools.ProjectRemoval;
using SolutionTools.Utils;

namespace SolutionTools.Commands
{
    public static class RemoveProjectsCommand
    {
        public static void Run(string[] args)
        {
            string solutionFile = string.Empty;
            string pattern = string.Empty;
            string target = string.Empty;
            bool help = false;

            var options = new OptionSet
            {
                {"s|solution=", "the path to solution files.", s => solutionFile = s},
                { "p|pattern=", "the regex pattern for project name", p => pattern = p},
                { "t|target=", "the target solution file to write to", t => target = t},
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

            var switcher = new RemoveProjects();
            switcher.Execute(Directory.GetCurrentDirectory(), solutionFile, pattern, target);
        }
    }
}
