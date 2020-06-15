using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using AutoStep.CommandLine.Output;
using AutoStep.Elements.Metadata;
using AutoStep.Execution;
using AutoStep.Execution.Contexts;
using AutoStep.Execution.Events;
using AutoStep.Execution.Logging;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Progress reporter for displaying errors as they happen.
    /// </summary>
    internal class CommandLineProgressReporter : BaseEventHandler
    {
        private readonly IConsoleWriter console;
        private readonly object consoleSync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineProgressReporter"/> class.
        /// </summary>
        /// <param name="console">Console writer.</param>
        public CommandLineProgressReporter(IConsoleWriter console)
        {
            this.console = console;
        }

        /// <inheritdoc/>
        public override async ValueTask OnExecuteAsync(ILifetimeScope scope, RunContext ctxt, Func<ILifetimeScope, RunContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            var startTime = DateTime.UtcNow;

            console.WriteLine();
            console.WriteLine(ResultsMessages.TestRunStarting);

            var logConsumer = ctxt.GetLogConsumer();

            RenderLogs(logConsumer, 0);

            try
            {
                await nextHandler(scope, ctxt, cancelToken);
            }
            finally
            {
                RenderLogs(logConsumer, 0);

                var totalElapsed = DateTime.UtcNow - startTime;

                // Finished.
                console.WriteLine(ResultsMessages.TestRunComplete.FormatWith(totalElapsed.Humanize()));
            }
        }

        /// <inheritdoc/>
        public override async ValueTask OnFeatureAsync(ILifetimeScope scope, FeatureContext ctxt, Func<ILifetimeScope, FeatureContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            var logConsumer = ctxt.GetLogConsumer();

            lock (consoleSync)
            {
                if (ctxt.Feature.SourceName is null)
                {
                    console.WriteLine(ResultsMessages.StartingFeature.FormatWith(ctxt.Feature.Name, DateTime.Now));
                }
                else
                {
                    console.WriteLine(ResultsMessages.StartingFeatureWithFile.FormatWith(ctxt.Feature.Name, ctxt.Feature.SourceName, DateTime.Now));
                }

                RenderLogs(logConsumer, 2);
            }

            try
            {
                await base.OnFeatureAsync(scope, ctxt, nextHandler, cancelToken);
            }
            finally
            {
                lock (consoleSync)
                {
                    RenderLogs(logConsumer, 2);
                    console.WriteLine(ResultsMessages.CompletedFeature.FormatWith(ctxt.Feature.Name, DateTime.Now));
                }
            }
        }

        /// <inheritdoc/>
        public override async ValueTask OnScenarioAsync(ILifetimeScope scope, ScenarioContext ctxt, Func<ILifetimeScope, ScenarioContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            var logConsumer = ctxt.GetLogConsumer();

            var featureContext = scope.Resolve<FeatureContext>();

            lock (consoleSync)
            {
                if (ctxt.Scenario is IScenarioOutlineInfo outline)
                {
                    var variables = (TableVariableSet)ctxt.Variables;

                    var invokeName = DetermineInvocationName(outline, variables);

                    if (invokeName is null)
                    {
                        invokeName = "no name";
                    }
                    else if (invokeName.Length > 30)
                    {
                        invokeName = invokeName.Substring(0, 30) + "...";
                    }

                    console.WriteLine(ResultsMessages.StartingScenarioInvocation.FormatWith(ctxt.Scenario.Name, invokeName, featureContext.Feature.Name), 2);
                }
                else
                {
                    console.WriteLine(ResultsMessages.StartingScenario.FormatWith(ctxt.Scenario.Name, featureContext.Feature.Name), 2);
                }

                RenderLogs(logConsumer, 4);
            }

            try
            {
                await base.OnScenarioAsync(scope, ctxt, nextHandler, cancelToken);
            }
            finally
            {
                lock (consoleSync)
                {
                    RenderLogs(logConsumer, 4);

                    if (ctxt.FailException is object)
                    {
                        console.WriteErrorLine(ResultsMessages.ScenarioFailed.FormatWith(ctxt.Scenario.Name, ctxt.Elapsed.Humanize()), 2);

                        if (ctxt.FailException is StepFailureException failure)
                        {
                            console.WriteErrorLine(ResultsMessages.StepFailed.FormatWith(failure.InnerException!.Message), 4);
                        }
                        else if (ctxt.FailException is EventHandlingException eventFail)
                        {
                            console.WriteErrorLine(ResultsMessages.EventHandlerFailed.FormatWith(eventFail.InnerException!.Message), 4);
                        }

                        if (ctxt.FailingStep is object)
                        {
                            // Failure.
                            // Get the text of the step that failed.
                            var stepText = ctxt.FailingStep.Text;
                            console.WriteErrorLine(ResultsMessages.FailingStep.FormatWith(stepText, ctxt.FailingStep.SourceLine), 4);
                        }
                    }
                    else
                    {
                        console.WriteSuccessLine(ResultsMessages.ScenarioPassed.FormatWith(ctxt.Scenario.Name, ctxt.Elapsed.Humanize()), 2);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override async ValueTask OnStepAsync(ILifetimeScope scope, StepContext ctxt, Func<ILifetimeScope, StepContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            var scenarioContext = scope.Resolve<ScenarioContext>();

            // Attach the log messages from the step context to the scenario.
            var logConsumer = ctxt.GetLogConsumer();

            await nextHandler(scope, ctxt, cancelToken);

            // 'Redirect' logs from the child context to the parent.
            scenarioContext.CaptureLogs(logConsumer);
        }

        private void RenderLogs(LogConsumer logConsumer, int indent)
        {
            while (logConsumer.TryGetNextEntry(out var logEntry))
            {
                IDisposable? colorBlock = null;

                if (logEntry.LogLevel == LogLevel.Warning)
                {
                    colorBlock = console.EnterWarnBlock();
                }
                else if (logEntry.LogLevel > LogLevel.Warning)
                {
                    colorBlock = console.EnterErrorBlock();
                }

                using (colorBlock)
                {
                    var entryText = logEntry.Text;

                    console.WriteIndentedBlock(entryText, indent);
                }
            }
        }

        /// <summary>
        /// Determines the name of an individual invocation from the scenario info and the table-provided variables
        /// passed into the scenario.
        /// </summary>
        /// <param name="info">The scenario outline information.</param>
        /// <param name="variables">The set of variables being used.</param>
        /// <returns>An optional name for the individual invocation.</returns>
        protected virtual string? DetermineInvocationName(IScenarioOutlineInfo info, TableVariableSet variables)
        {
            if (variables is null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            // Get the first column of the table.
            var firstColumn = variables.ColumnNames.FirstOrDefault();

            if (firstColumn is object)
            {
                return variables.Get(firstColumn);
            }

            return null;
        }
    }
}
