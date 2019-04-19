using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using SolutionTools.Merger;
using SolutionTools.Utils;

namespace SolutionTools.Commands
{
    public static class MergeCommand
    {
        public static void Run(string[] args)
        {
            string source = string.Empty;
            string target = string.Empty;
            bool overwrite = false;
            bool help = false;
            bool addFolder = true;
            bool addSameGuid = true;

            var options = new OptionSet
            {
                { "s|source=", "the glob pattern to match solution files", s => source = s},
                { "t|target=", "the target solution file", t => target = t},
                { "f|add-folder", "add relative solution folder", f => addFolder = f != null},
                { "g|add-same-guid", "add projects with same guid by creating new guid", g => addSameGuid = g != null},
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

            var merger = new SolutionMerger();

            try
            {
                var isDirty = merger.Execute(source, target, overwrite, addFolder, addSameGuid);
                if (isDirty)
                {
                    Logger.Info($"Merged solution {source} to {target}");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error merging solutions {source} to {target}");
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
        }
    }
}
