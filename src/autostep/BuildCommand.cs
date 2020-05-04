using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Defines the command that builds a project (but does not run it).
    /// </summary>
    internal class BuildCommand : BuildOperationCommand<BuildOperationArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildCommand"/> class.
        /// </summary>
        public BuildCommand()
            : base("build", "Build interactions and tests.")
        {
        }

        /// <inheritdoc/>
        public override async Task<int> Execute(BuildOperationArgs args, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            var success = false;
            var logger = logFactory.CreateLogger<BuildCommand>();

            try
            {
                var projectConfig = GetConfiguration(args);

                using var extensions = await LoadExtensionsAsync(args, logFactory, projectConfig, cancelToken);

                if (args.Attach)
                {
                    Debugger.Launch();
                }

                success = await CreateAndBuildProject(args, projectConfig, logFactory, extensions, cancelToken);
            }
            catch (ExtensionLoadException ex)
            {
                logger.LogError(ex.Message);
            }
            catch (AggregateException ex)
            {
                foreach (var nestedEx in ex.InnerExceptions)
                {
                    logger.LogError(nestedEx.Message);
                }
            }
            catch (ProjectConfigurationException configEx)
            {
                LogConfigurationError(logger, configEx);
            }

            return success ? 0 : 1;
        }

        private async Task<bool> CreateAndBuildProject(BuildOperationArgs args, IConfiguration projectConfig, ILoggerFactory logFactory, ExtensionsContext extensions, CancellationToken cancelToken)
        {
            var project = CreateProject(args, projectConfig, extensions);

            return await BuildAndWriteResultsAsync(project, logFactory, cancelToken);
        }
    }
}
