using Microsoft.Build.Construction;

namespace SolutionTools.Switcher
{
    public class ExistingProjectResult
    {
        public ProjectInSolution Project { get; set; }

        public DllReferenceResult DllReference { get; set; }
    }
}
