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
    /// The Create project command that handles 'new project' commands passed to autostep-cli.
    /// </summary>
    internal class CreateProjectCommand : AutoStepCommand<RunArgs>
    {
        private readonly CreateProjectDelegate createProj;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProjectCommand"/> class.
        /// </summary>
        /// <param name="autoStepFiles">Instance of <see cref="AutoStepFiles"/> that creates the necessary project files for a blank project.</param>
        public CreateProjectCommand(AutoStepFiles autoStepFiles)
            : this("project", "creates a new project with single test file(.as) file and an empty autostep.config.json.", autoStepFiles.CreateBlankProject)
        {
            AddCommand(new CreateWebProjectCommand(autoStepFiles));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProjectCommand"/> class.
        /// </summary>
        /// <param name="name">command name.</param>
        /// <param name="description">command description.</param>
        /// <param name="createProj">delegate reference to the method that creates the project.</param>
        protected CreateProjectCommand(string name, string description, CreateProjectDelegate createProj)
            : base(name, description)
        {
            AddCommonOptions();
            this.createProj = createProj ?? this.createProj;
        }

        private void AddCommonOptions() => AddOption(CommonOptions.SpecifyDirectoryOption);

        /// <summary>
        /// Executes the create project commmand.
        /// </summary>
        /// <param name="args">The `RunArgs` command-line arguments.</param>
        /// <param name="logFactory">A logger factory.</param>
        /// <param name="cancelToken">A cancellation token.</param>
        /// <returns>Returns an awaitable object that outputs the command's exit code depending on the CreateProj's result.</returns>
        public override Task<int> Execute(RunArgs args, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            var logger = logFactory.CreateLogger<NewProjectCommand>();

            try
            {
                return Task.FromResult(createProj(args, logger));
            }
            catch (AggregateException ex)
            {
                foreach (var nestedEx in ex.InnerExceptions)
                {
                    logger.LogError(nestedEx.Message);
                }
            }

            return Task.FromResult(1);
        }

        /// <summary>
        /// A delegate for project creating function.
        /// </summary>
        /// <param name="args">Run args from the command line.</param>
        /// <param name="logger">logger instance.</param>
        /// <returns>success code - 0 (success) or 1 (fail).</returns>
        internal delegate int CreateProjectDelegate(RunArgs args, ILogger logger);

        /// <summary>
        /// Util class to host all the common Options for creating new autostep projects.
        /// </summary>
        internal class CommonOptions
        {
            /// <summary>
            /// System.CommandLine Option for letting the user specify a directory.
            /// Note: This option does not assert the presence of the directory specified.
            /// </summary>
            public static readonly Option SpecifyDirectoryOption = new Option(new[] { "-d", "--directory" }, "Provide the base directory for the autostep project.")
            {
                Argument = new Argument<DirectoryInfo>(() => new DirectoryInfo(Environment.CurrentDirectory)),
            };
        }
    }
}
