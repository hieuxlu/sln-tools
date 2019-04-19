using System.Linq;
using System.Reflection;
using Mono.Options;
using SolutionTools.Commands;

namespace SolutionTools
{
    class Program
    {
        static void Main(string[] args)
        {
            var name = Assembly.GetCallingAssembly().GetName().Name;

            var suite = new CommandSet(name)
            {
                $"usage: {name} COMMAND [OPTIONS]+",
                new Command("switch", "switch dll reference to project reference in a solution")
                {
                    Run = args2 =>  SwitchCommand.Run(args2.ToArray())
                },
                new Command("merge", "merge solution files to a single solution")
                {
                    Run = args2 =>  MergeCommand.Run(args2.ToArray())
                },
                new Command("remove-imports", "remove imports from project files")
                {
                    Run = args2 =>  RemoveImportCommand.Run(args2.ToArray())
                },
                new Command("remove-projects", "remove projects from solution files")
                {
                    Run = args2 =>  RemoveProjectsCommand.Run(args2.ToArray())
                }
            };

            suite.Run(args);
        }
    }
}
