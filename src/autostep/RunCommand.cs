using AutoStep.Execution;
using AutoStep.Projects;
using AutoStep.Projects.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.CommandLine
{
    public class RunCommand : BuildOperationCommand<RunArgs>
    {
        public RunCommand() : base("run", "Build and execute tests.")
        {
        }

        public override async Task<int> Execute(RunArgs args, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            var logger = logFactory.CreateLogger<BuildCommand>();

            try
            {
                var projectConfig = await GetConfiguration(args, cancelToken);

                using var extensions = await LoadExtensionsAsync(args, logFactory, projectConfig, cancelToken);

                var project = CreateProject(args, projectConfig, extensions);

                if (await BuildAndWriteResultsAsync(args, project, logFactory, cancelToken))
                {
                    var runConfiguration = new RunConfiguration();

                    // No errors, run.
                    var testRun = project.CreateTestRun(runConfiguration);

                    // Allow extensions to extend the execution behaviour.
                    extensions.ExtendExecution(runConfiguration, testRun);

                    // Execute the test run, allowing extensions to register their own services.
                    await testRun.ExecuteAsync(logFactory, builder => extensions.ConfigureExtensionServices(builder, runConfiguration));

                    return 0;
                }

                // Throw project away, so extensions can unload.
                project = null;
            }
            catch (ProjectConfigurationException projectConfigEx)
            {
                LogConfigurationError(logger, projectConfigEx);
            }

            return 1;
        }
    }
}
