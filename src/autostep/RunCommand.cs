using AutoStep.Execution;
using AutoStep.Extensions;
using AutoStep.Extensions.Abstractions;
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
                var projectConfig = GetConfiguration(args);

                using var extensions = await LoadExtensionsAsync(args, logFactory, projectConfig, cancelToken);

                var project = CreateProject(args, projectConfig, extensions);

                if (await BuildAndWriteResultsAsync(args, project, logFactory, cancelToken))
                {
                    // No errors, run.
                    var testRun = project.CreateTestRun(projectConfig);

                    // Allow extensions to extend the execution behaviour.
                    extensions.ExtendExecution(projectConfig, testRun);

                    // Execute the test run, allowing extensions to register their own services.
                    await testRun.ExecuteAsync(logFactory, (runConfig, builder) =>
                    {
                        // Register the extension set (might need it later).
                        builder.RegisterSingleInstance<ILoadedExtensions>(extensions);

                        extensions.ConfigureExtensionServices(runConfig, builder);
                    });

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
