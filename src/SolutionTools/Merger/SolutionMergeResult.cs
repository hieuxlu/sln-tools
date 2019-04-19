using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using EditableSolutionFile = CWDev.SLNTools.Core.SolutionFile;

namespace SolutionTools.Merger
{
    public class SolutionMergeResult
    {
        public EditableSolutionFile MergedSolution { get; set; }

        public List<EditableSolutionFile> ConflictingSolutions { get; set; }

        public List<Project> ConflictingProjects { get; set; }
    }
}
