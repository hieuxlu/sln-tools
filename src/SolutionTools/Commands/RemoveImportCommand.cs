using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using SolutionTools.ImportRemoval;
using SolutionTools.Switcher;
using SolutionTools.Utils;

namespace SolutionTools.Commands
{
    public static class RemoveImportCommand
    {
        public static void Run(string[] args)
        {
            string solutionFile = string.Empty;
            string pattern = string.Empty;
            bool overwrite = false;
            bool help = false;

            var options = new OptionSet
            {
                {"f|files=", "the glob pattern for project files.", s => solutionFile = s},
                { "p|pattern=", "the regex pattern for import statement", p => pattern = p},
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

            var switcher = new RemoveImport();  
            switcher.Execute(Directory.GetCurrentDirectory(), solutionFile, pattern, overwrite);
        }
    }
}
