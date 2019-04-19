using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using SolutionTools.Merger;
using SolutionTools.Switcher;
using SolutionTools.Utils;
using EditableSolutionFile = CWDev.SLNTools.Core.SolutionFile;

namespace SolutionTools.Extensions
{
    public static class MsBuildExtensions
    {
        private const string LOCAL_REFERENCE = "Reference";
        private const string PROJECT_REFERENCE = "ProjectReference";


        public const string csProjectGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        public const string solutionFolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";


        public static IDictionary<string, ProjectInSolution> GetProjectDictionary(this SolutionFile solution)
        {
            var projectSet = new Dictionary<string, ProjectInSolution>();
            var projects = solution.ProjectsByGuid.Where(item => item.Value.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                .ToDictionary(item => item.Value.ProjectName, item => item.Value);

            return projects;
        }

        public static IEnumerable<DllReferenceResult> GetLocalDllReference(this Project project, string prefix = null)
        {
            return project.Items
                .Where(item => item.ItemType == LOCAL_REFERENCE &&
                               (string.IsNullOrEmpty(prefix) || item.EvaluatedInclude.StartsWith(prefix)))
                .Select(item =>
                {
                    string projectName;

                    if (item.EvaluatedInclude.Contains(','))
                    {
                        projectName = item.EvaluatedInclude.Split(',')[0];
                    }
                    else
                    {
                        projectName = item.EvaluatedInclude;
                    }

                    return new DllReferenceResult
                    {
                        EvaluatedInclude = item.EvaluatedInclude,
                        ProjectName = projectName
                    };
                });
        }

        public static Project UpdateProjectReference(this Project project, ProjectInSolution reference, string evaluatedInclude)
        {
            var projectItems = project.Items.Where(
                item => item.ItemType == LOCAL_REFERENCE && item.EvaluatedInclude == evaluatedInclude)
                .ToList();

            // Handle abnormal case when dll reference is duplicated
            if (projectItems.Count > 1)
            {
                Logger.Warn($"Found {projectItems.Count} duplicated dll reference for {evaluatedInclude}");
            }

            foreach (var projectItem in projectItems)
            {
                projectItem.Xml.Parent?.RemoveChild(projectItem.Xml);
            }

            var currentProjectUri = new Uri(project.FullPath);
            var referenceProjectUri = new Uri(reference.AbsolutePath);
            var diff = currentProjectUri.MakeRelativeUri(referenceProjectUri);

            // Only add project reference once
            var existingProject = project.Items
                .Where(item => item.ItemType == PROJECT_REFERENCE && item.EvaluatedInclude == diff.OriginalString)
                .ToList();

            // Remove all existing project reference
            if (existingProject.Count > 0)
            {
                Logger.Warn($"Remove existing project reference(s) for ${evaluatedInclude}");

                foreach (var projectItem in existingProject)
                {
                    projectItem.Xml.Parent?.RemoveChild(projectItem.Xml);
                }
            }

            // Update new project reference
            project.AddItem(PROJECT_REFERENCE, diff.OriginalString, new[]
            {
                new KeyValuePair<string, string>("Project", reference.ProjectGuid),
                new KeyValuePair<string, string>("Name", reference.ProjectName)
            });

            return project;
        }

        public static Project LoadProject(string path)
        {
            var projectCollection = new ProjectCollection();
            return projectCollection.LoadProject(path);
        }

        public static Project UpdateGuidReference(this Project loadedProject, Guid oldGuid, Guid newGuid)
        {
            var project = loadedProject.Properties.First(item => item.Name == "ProjectGuid");
            var projectGuid = new Guid(project.EvaluatedValue);

            if (projectGuid.Equals(oldGuid))
            {
                // Update own GUID
                project.UnevaluatedValue = newGuid.ToString("B").ToUpper();
            }
            else
            {
                // Update project reference
                var oldReference = loadedProject.Items
                    .FirstOrDefault(item => item.ItemType == PROJECT_REFERENCE &&
                                            string.Equals(item.GetMetadataValue("Project"), oldGuid.ToString("B"), StringComparison.OrdinalIgnoreCase));

                // Found reference
                if (oldReference != null)
                {
                    loadedProject.RemoveItem(oldReference);
                    loadedProject.AddItem(PROJECT_REFERENCE, oldReference.EvaluatedInclude, new[]
                    {
                    new KeyValuePair<string, string>("Project", newGuid.ToString("B").ToUpper()),
                    new KeyValuePair<string, string>("Name", oldReference.GetMetadataValue("Name"))
                });
                }
            }

            return loadedProject;
        }

        public static List<Project> UpdateProjectGuidReference(this EditableSolutionFile solution, Guid oldGuid, Guid newGuid)
        {
            // Remove project guid itself
            var project = solution.Projects[oldGuid.ToString("B")];

            if (project != null)
            {
                solution.Projects.Remove(project);
                var clone = new CWDev.SLNTools.Core.Project(solution, newGuid.ToString("B"), project.ProjectTypeGuid,
                    project.ProjectName, project.RelativePath, project.ParentFolderGuid, project.ProjectSections,
                    project.VersionControlLines,
                    project.ProjectConfigurationPlatformsLines);
                solution.Projects.Add(clone);
            }

            var updatedProjects = new List<Project>();

            // Loop each project in solution
            foreach (var projectInSolution in solution.Projects)
            {
                if (projectInSolution.ProjectTypeGuid == csProjectGuid)
                {
                    // Load project
                    var loadedProject = MsBuildExtensions.LoadProject(projectInSolution.FullPath);

                    loadedProject.UpdateGuidReference(oldGuid, newGuid);

                    if (loadedProject.IsDirty)
                    {
                        updatedProjects.Add(loadedProject);
                    }
                }
            }

            return updatedProjects;
        }
    }
}
