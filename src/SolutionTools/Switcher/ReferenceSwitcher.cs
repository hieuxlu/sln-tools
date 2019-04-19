using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using SolutionTools.Extensions;
using SolutionTools.Utils;

namespace SolutionTools.Switcher
{
    public class ReferenceSwitcher
    {
        public void Execute(string solutionFile, string pattern, bool overwrite, bool inSolutionProjectsOnly = false)
        {
            var solutionDirectory = Path.GetDirectoryName(solutionFile);

            if (string.IsNullOrEmpty(solutionDirectory))
            {
                solutionDirectory = Directory.GetCurrentDirectory();
            }

            // Load solution file
            var solution = SolutionFile.Parse(Path.GetFullPath(solutionFile));

            // Include only projects in solution file
            List<string> projectFiles;

            if (inSolutionProjectsOnly)
            {
                projectFiles = solution.ProjectsInOrder
                    .Where(item => item.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                    .Select(item => item.RelativePath)
                    .ToList();
            }
            else
            {
                // Search glob file patterns
                projectFiles = DirectoryExtensions.LoadFiles(solutionDirectory, pattern);
            }

            foreach (var item in projectFiles)
            {
                // Load project file
                var projectDirectory = Path.Combine(solutionDirectory, item);
                var project = MsBuildExtensions.LoadProject(projectDirectory);
                try
                {
                    var isDirty = UpdateProjectReference(solution, project);
                    if (isDirty)
                    {
                        if (overwrite)
                        {
                            Logger.Info($"Project reference for {item} updated");
                            project.Save();
                        }
                        else
                        {
                            Logger.Info($"Project reference for {item} can be updated with --write flag");
                        }
                    }
                }
                catch (Exception e)
                {
                    // output some error message
                    Logger.Error($"Error updating project reference for project {item}");
                    Logger.Error(e.Message);
                }
            }
        }

        public bool UpdateProjectReference(SolutionFile solution, Project project)
        {
            var localProjects = solution.GetProjectDictionary();
            var referenceProjects = project.GetLocalDllReference();

            var existingProjects = referenceProjects.Where(item => localProjects.ContainsKey(item.ProjectName))
                .Select(item => new ExistingProjectResult
                {
                    Project = localProjects[item.ProjectName],
                    DllReference = item
                }).ToList();

            foreach (var existingProjectResult in existingProjects)
            {
                project.UpdateProjectReference(existingProjectResult.Project, existingProjectResult.DllReference.EvaluatedInclude);
            }

            return project.IsDirty;
        }
    }
}
