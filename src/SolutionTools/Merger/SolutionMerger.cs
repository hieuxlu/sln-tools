using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CWDev.SLNTools.Core;
using Microsoft.Build.Construction;
using SolutionTools.Extensions;
using SolutionTools.Utils;
using EditableSolutionFile = CWDev.SLNTools.Core.SolutionFile;
using SolutionFile = Microsoft.Build.Construction.SolutionFile;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace SolutionTools.Merger
{
    public class SolutionMerger
    {
        public bool Execute(string source, string target, bool overwrite, bool addFolder, bool addSameGuid)
        {
            var isDirty = false;
            var sourceSolution = EditableSolutionFile.FromFile(Path.GetFullPath(source));
            var targetSolution = EditableSolutionFile.FromFile(Path.GetFullPath(target));
            var mergedResult = Merge(sourceSolution, targetSolution, addFolder, addSameGuid);

            if (overwrite)
            {
                mergedResult.MergedSolution.Save();
                mergedResult.ConflictingSolutions.ForEach(item =>
                {
                    Logger.Warn($"Updating solutions with project guids conflicts: {item.SolutionFullPath}");
                    item.Save();
                });

                mergedResult.ConflictingProjects.ForEach(item =>
                {
                    Logger.Warn($"Updating project guids/reference conflicts: {item.FullPath}");
                    item.Save();
                });
                isDirty = true;
            }

            return isDirty;
        }

        public SolutionMergeResult Merge(EditableSolutionFile source, EditableSolutionFile target, bool addFolder = true, bool addSameGuid = true)
        {
            var conflictingProjects = new List<MsBuildProject>();
            var conflictingSolutions = new List<EditableSolutionFile>();

            // Compare project diff
            var sourceProjects = source.Projects
                .Where(item => item.ProjectTypeGuid == MsBuildExtensions.csProjectGuid || item.ProjectTypeGuid == MsBuildExtensions.solutionFolderGuid)
                .ToList();

            var targetProjects = target.Projects
                .Where(item => item.ProjectTypeGuid == MsBuildExtensions.csProjectGuid || item.ProjectTypeGuid == MsBuildExtensions.solutionFolderGuid)
                .ToDictionary(item => item.ProjectGuid, item => item);

            // Correct relative project paths for cs projects
            var directoryPath = Path.GetDirectoryName(source.SolutionFullPath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                directoryPath = Directory.GetCurrentDirectory();
            }

            var targetPath = new Uri(target.SolutionFullPath);

            // Add root solution folder based on relative uri
            Project parentProjectFolder = null;
            if (addFolder)
            {
                parentProjectFolder = CreateRelativeSolutionFolder(target, source.SolutionFullPath);
            }

            // Add diff guids
            List<Project> diffProjects = sourceProjects
                .Where(item => !targetProjects.ContainsKey(item.ProjectGuid))
                .Select(item => new Project(target, item))
                .ToList();

            // Add conflicts guids
            var conflicts = sourceProjects
                .Where(item => targetProjects.ContainsKey(item.ProjectGuid) && targetProjects[item.ProjectGuid].ProjectName != item.ProjectName)
                .ToList();

            // Auto replace duplicate guids
            if (addSameGuid && conflicts.Count > 0)
            {
                // Add duplicate guids
                var conflitingProjects = conflicts
                    .Select(item =>
                    {
                        // Clone to new GUID
                        var clone = new Project(target, Guid.NewGuid().ToString("B"), item.ProjectTypeGuid,
                            item.ProjectName, item.RelativePath, item.ParentFolderGuid, item.ProjectSections,
                            item.VersionControlLines,
                            item.ProjectConfigurationPlatformsLines);

                        // Hope there's only 1-2 duplicates per solution
                        // Update reference projects to new project guids
                        var temp = source.UpdateProjectGuidReference(new Guid(item.ProjectGuid), new Guid(clone.ProjectGuid));
                        conflictingProjects.AddRange(temp);

                        return clone;
                    })
                    .ToList();

                // Solution needs updating
                if (conflictingProjects.Count > 0)
                {
                    conflictingSolutions.Add(source);
                }

                // Add newly cloned projects
                diffProjects.AddRange(conflitingProjects);
            }

            // Add projects to solution
            diffProjects.ForEach(project =>
            {
                // Correct relative project paths for cs projects
                if (project.ProjectTypeGuid == MsBuildExtensions.csProjectGuid)
                {
                    var projectPath = new Uri(Path.Combine(directoryPath, project.RelativePath));
                    var relativePath = targetPath.MakeRelativeUri(projectPath);

                    project.RelativePath = relativePath.OriginalString;
                }

                // Add root solution folder based on relative uri
                if (parentProjectFolder != null && string.IsNullOrEmpty(project.ParentFolderGuid))
                {
                    project.ParentFolderGuid = parentProjectFolder.ProjectGuid;
                }

                // Add same guid projects
                target.Projects.Add(project);
            });

            return new SolutionMergeResult
            {
                MergedSolution = target,
                ConflictingSolutions = conflictingSolutions,
                ConflictingProjects = conflictingProjects
            };
        }

        public Project CreateRelativeSolutionFolder(EditableSolutionFile target, string source)
        {
            var targetPath = new Uri(target.SolutionFullPath);
            var sourcePath = new Uri(source);
            var relativeSourcePath = targetPath.MakeRelativeUri(sourcePath);
            var parentSourceFolder = Path.GetDirectoryName(relativeSourcePath.OriginalString);

            if (string.IsNullOrEmpty(parentSourceFolder))
            {
                return null;
            }

            var folders = parentSourceFolder.Split('\\');
            Project parentFolder = null;

            // Search existing folder
            var existingFolderList =
                target.Projects.Where(item => item.ProjectTypeGuid == MsBuildExtensions.solutionFolderGuid)
                    .ToList();

            foreach (var folder in folders)
            {
                // Check existing solution folder
                var existingFolder = existingFolderList.FirstOrDefault(item => item.ProjectName == folder);
                if (existingFolder != null)
                {
                    var compareParentFolder = existingFolder.ParentFolder;
                    if (compareParentFolder == parentFolder)
                    {
                        parentFolder = existingFolder;
                        continue;
                    }
                }

                var project = new Project(target, Guid.NewGuid().ToString("B").ToUpper(), MsBuildExtensions.solutionFolderGuid, folder, folder, parentFolder?.ProjectGuid, new List<Section>() { }, new List<PropertyLine>(), new List<PropertyLine>());
                target.Projects.Add(project);
                parentFolder = project;
            }

            return parentFolder;
        }
    }
}
