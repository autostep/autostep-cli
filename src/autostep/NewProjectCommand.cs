using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Base class for 'new' autostep cli command for creating projects.
    /// </summary>
    internal class NewProjectCommand : AutoStepCommand<RunArgs>
    {
        private bool isValidCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewProjectCommand"/> class.
        /// </summary>
        /// <param name="autoStepFiles">Instance of <see cref="AutoStepFiles"/> that creates the necessary project files.</param>
        public NewProjectCommand(AutoStepFiles autoStepFiles)
            : base("new", "create a new project.")
        {
            AddCommand(new CreateProjectCommand(autoStepFiles));
        }

        /// <summary>
        /// Executes the new command without any subcommands.
        /// </summary>
        /// <param name="args">The `RunArgs` command-line arguments.</param>
        /// <param name="logFactory">A logger factory.</param>
        /// <param name="cancelToken">A cancellation token.</param>
        /// <returns>Returns an awaitable object that outputs `incorrect function`exit code (1) since `new` command must be issued with subcomands.</returns>
        public override Task<int> Execute(RunArgs args, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            isValidCommand = false;

            return Task.FromResult(1);
        }

        /// <summary>
        /// Checks if the `new` commmand is propely issued.
        /// </summary>
        /// <returns>Returns true if `new` command is followed by subcommands like `new project` or `new project web`.</returns>
        public override bool CommandIsValid() => isValidCommand;
    }
}
