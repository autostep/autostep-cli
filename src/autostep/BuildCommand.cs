using AutoStep.Projects.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.CommandLine
{
    public class BuildCommand : BuildOperationCommand<BuildOperationArgs>
    {
        public BuildCommand() 
            : base("build", "Build interactions and tests.")
        {
        }

        public override async Task<int> Execute(BuildOperationArgs args, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            var success = false;
            var logger = logFactory.CreateLogger<BuildCommand>();

            try
            {
                var projectConfig = await GetConfiguration(args, cancelToken);

                using var extensions = await LoadExtensionsAsync(args, logFactory, projectConfig, cancelToken);

                var project = CreateProject(args, projectConfig, extensions);

                success = await BuildAndWriteResultsAsync(args, project, logFactory, cancelToken);

            } 
            catch (ProjectConfigurationException configEx)
            {
                LogConfigurationError(logger, configEx);
            }

            return success ? 0 : 1;
        }
    }
}
