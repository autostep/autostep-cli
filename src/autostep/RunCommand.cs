using System.Threading;
using System.Threading.Tasks;
using AutoStep.CommandLine.Results;
using AutoStep.Extensions;
using AutoStep.Extensions.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

                return await CreateAndExecuteProject(args, logFactory, projectConfig, extensions, cancelToken);
            }
            catch (ProjectConfigurationException projectConfigEx)
            {
                LogConfigurationError(logger, projectConfigEx);
            }

            return 1;
        }

        private async Task<int> CreateAndExecuteProject(RunArgs args, ILoggerFactory logFactory, IConfiguration projectConfig, IExtensionSet extensions, CancellationToken cancelToken)
        {
            var project = CreateProject(args, projectConfig, extensions);

            if (await BuildAndWriteResultsAsync(args, project, logFactory, cancelToken))
            {
                // No errors, run.
                var testRun = project.CreateTestRun(projectConfig);

                testRun.Events.Add(new CommandLineResultsCollector());

                // Allow extensions to extend the execution behaviour.
                extensions.ExtendExecution(projectConfig, testRun);

                // Execute the test run, allowing extensions to register their own services.
                await testRun.ExecuteAsync(logFactory, (runConfig, builder) =>
                {
                    // Register the console provider.
                    builder.RegisterInstance<IConsoleResultsWriter>(new ConsoleResultsWriter(logFactory));

                    // Register the extension set (might need it later).
                    builder.RegisterInstance<ILoadedExtensions>(extensions);

                    extensions.ConfigureExtensionServices(runConfig, builder);
                });

                return 0;
            }

            return 1;
        }

    }
}
