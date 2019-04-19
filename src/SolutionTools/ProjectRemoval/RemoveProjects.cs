using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CWDev.SLNTools.Core;
using SolutionTools.Extensions;
using SolutionTools.Utils;

namespace SolutionTools.ProjectRemoval
{
    public class RemoveProjects
    {
        public void Execute(string basePath, string solutionFile, string pattern, string target)
        {
            var solution = SolutionFile.FromFile(Path.Combine(basePath, solutionFile));
            var regex = new Regex(pattern);

            var projects = solution.Projects.Where(item => regex.IsMatch(item.ProjectName))
                .ToList();

            if (!string.IsNullOrEmpty(target))
            {
                foreach (var project in projects)
                {
                    // If project type is folder, remove all child folders
                    if (project.ProjectTypeGuid == MsBuildExtensions.solutionFolderGuid)
                    {
                        foreach (var childProject in project.Childs.ToList())
                        {
                            if (solution.Projects.Remove(childProject))
                            {
                                Logger.Info($"Removing child project {project.ProjectName} from solution {solutionFile}");
                            }
                        }
                    }

                    if (solution.Projects.Remove(project))
                    {
                        Logger.Info($"Removing project {project.ProjectName} from solution {solutionFile}");
                    }
                }

                solution.SaveAs(Path.Combine(basePath, target));
            }
        }
    }
}
