using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.CommandLine.Results;
using AutoStep.Execution;
using AutoStep.Execution.Results;
using AutoStep.Extensions;
using AutoStep.Extensions.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
            var logger = logFactory.CreateLogger<RunCommand>();

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

                var environment = CreateEnvironment(args);

                using var extensions = await LoadExtensionsAsync(args, logFactory, projectConfig, environment, cancelToken);

                if (args.Attach)
                {
                    Debugger.Launch();
                }

                return await CreateAndExecuteProject(args, logFactory, projectConfig, extensions, environment, cancelToken);
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
                    LogException(logger, ex.InnerException);
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

        private async Task<int> CreateAndExecuteProject(
            RunArgs args,
            ILoggerFactory logFactory,
            IConfiguration projectConfig,
            ILoadedExtensions<IExtensionEntryPoint> extensions,
            IAutoStepEnvironment environment,
            CancellationToken cancelToken)
        {
            var project = CreateProject(args, projectConfig, extensions, environment);

            if (await BuildAndWriteResultsAsync(project, logFactory, cancelToken))
            {
                // No errors, run.
                var testRun = project.CreateTestRun(projectConfig);

                // Add our progress reporter.
                testRun.Events.Add(new CommandLineProgressReporter(Console));

                testRun.AddDefaultResultsCollector();

                foreach (var ext in extensions.ExtensionEntryPoints)
                {
                    // Allow extensions to extend the execution behaviour.
                    ext.ExtendExecution(projectConfig, testRun);
                }

                var consoleResultsExporter = new ConsoleResultsExporter(Console);

                // Do not use the logger factory for console output (we will capture events and use that instead).
                // TODO: If a 'log file' option is supplied, then we may write to that as well in future.

                // Execute the test run, allowing extensions to register their own services.
                await testRun.ExecuteAsync(NullLoggerFactory.Instance, cancelToken, (runConfig, builder) =>
                {
                    // Register the extension set (might need it later).
                    builder.RegisterInstance<ILoadedExtensions>(extensions);

                    builder.RegisterInstance(environment);

                    // Register the exporter that writes the summary at the end of the run.
                    builder.RegisterInstance<IResultsExporter>(consoleResultsExporter);

                    foreach (var ext in extensions.ExtensionEntryPoints)
                    {
                        ext.ConfigureExecutionServices(runConfig, builder);
                    }
                });

                if (consoleResultsExporter.RunResults!.AllPassed)
                {
                    return 0;
                }
            }

            return 1;
        }
    }
}
