using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.CommandLine.Results;
using AutoStep.Execution;
using AutoStep.Extensions;
using AutoStep.Extensions.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Defines the command for running tests.
    /// </summary>
    internal class RunCommand : BuildOperationCommand<RunArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunCommand"/> class.
        /// </summary>
        public RunCommand()
            : base("run", "Build and execute tests.")
        {
            AddArgument(new Argument<string?>("runConfig") { Arity = ArgumentArity.ZeroOrOne });
        }

        /// <inheritdoc/>
        public override async Task<int> Execute(RunArgs args, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            var logger = logFactory.CreateLogger<BuildCommand>();

            try
            {
                IConfiguration projectConfig;

                if (args.RunConfig is object)
                {
                    projectConfig = GetConfiguration(args, new[] { new KeyValuePair<string, string>("runConfig", args.RunConfig) });

                    if (!projectConfig.GetSection("runConfigs:" + args.RunConfig).Exists())
                    {
                        logger.LogError(Messages.RunConfigurationNotAvailable, args.RunConfig);
                        return 1;
                    }
                }
                else
                {
                    projectConfig = GetConfiguration(args);
                }

                using var extensions = await LoadExtensionsAsync(args, logFactory, projectConfig, cancelToken);

                if (args.Attach)
                {
                    Debugger.Launch();
                }

                return await CreateAndExecuteProject(args, logFactory, projectConfig, extensions, cancelToken);
            }
            catch (ExtensionLoadException ex)
            {
                LogException(logger, ex);
            }
            catch (EventHandlingException ex)
            {
                logger.LogError(Messages.EventHandlerFailed);

                if (ex.InnerException is object)
                {
                    LogException(logger, ex);
                }
            }
            catch (AggregateException ex)
            {
                LogException(logger, ex);
            }
            catch (ProjectConfigurationException configEx)
            {
                LogConfigurationError(logger, configEx);
            }

            return 1;
        }

        private void LogException(ILogger logger, Exception ex)
        {
            if (ex is AggregateException aggregate)
            {
                foreach (var nestedEx in aggregate.InnerExceptions)
                {
                    LogException(logger, nestedEx);
                }
            }
            else
            {
                logger.LogError(ex.Message);
            }
        }

        private async Task<int> CreateAndExecuteProject(RunArgs args, ILoggerFactory logFactory, IConfiguration projectConfig, ExtensionsContext extensions, CancellationToken cancelToken)
        {
            var project = CreateProject(args, projectConfig, extensions);

            if (await BuildAndWriteResultsAsync(project, logFactory, cancelToken))
            {
                // No errors, run.
                var testRun = project.CreateTestRun(projectConfig);

                var resultsCollector = new CommandLineResultsCollector();

                testRun.Events.Add(resultsCollector);

                foreach (var ext in extensions.LoadedExtensions.ExtensionEntryPoints)
                {
                    // Allow extensions to extend the execution behaviour.
                    ext.ExtendExecution(projectConfig, testRun);
                }

                // Execute the test run, allowing extensions to register their own services.
                await testRun.ExecuteAsync(logFactory, cancelToken, (runConfig, builder) =>
                {
                    // Register the console provider.
                    builder.RegisterInstance<IConsoleResultsWriter>(new ConsoleResultsWriter(logFactory));

                    // Register the extension set (might need it later).
                    builder.RegisterInstance<ILoadedExtensions>(extensions.LoadedExtensions);

                    foreach (var ext in extensions.LoadedExtensions.ExtensionEntryPoints)
                    {
                        ext.ConfigureExecutionServices(runConfig, builder);
                    }
                });

                if (resultsCollector.FailedScenarios > 0)
                {
                    // Give a non-zero exit code if tests fail.
                    return 1;
                }

                return 0;
            }

            return 1;
        }
    }
}
