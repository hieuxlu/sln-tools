using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SolutionTools.Extensions;
using SolutionTools.Utils;

namespace SolutionTools.ImportRemoval
{
    public class RemoveImport
    {
        public void Execute(string basePath, string projects, string pattern, bool overwrite)
        {
            var projectFiles = DirectoryExtensions.LoadFiles(basePath, projects);
            foreach (var projectFile in projectFiles)
            {
                XDocument doc = XDocument.Load(Path.Combine(basePath, projectFile));                
                var regex = new Regex(pattern);

                var imports = doc.Root.Descendants()
                    .Where(item => item.Name.LocalName == "Import" && regex.IsMatch(item.Attribute("Project")?.Value))
                    .ToList();

                if (overwrite && imports.Count > 0)
                {
                    Logger.Info($"Removing imports from project: {projectFile}");

                    imports.ForEach(item =>
                    {
                        Logger.Info($"Removing import {item.Attribute("Project")?.Value} in {projectFile}");
                        item.Remove();
                    });

                    doc.Save(Path.Combine(basePath, projectFile));
                }
            }
        }
    }
}
