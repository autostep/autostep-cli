using System;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Execution;
using AutoStep.Execution.Contexts;
using AutoStep.Execution.Events;
using Microsoft.Extensions.DependencyInjection;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Results collector for displaying results on the command line.
    /// </summary>
    internal class CommandLineResultsCollector : BaseEventHandler
    {
        private int failedScenarios;

        public int FailedScenarios => failedScenarios;

        /// <inheritdoc/>
        public override async ValueTask OnExecuteAsync(IServiceProvider scope, RunContext ctxt, Func<IServiceProvider, RunContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            var writer = GetConsoleWriter(scope);

            writer.WriteInfo(ResultsMessages.TestRunStarting);

            await nextHandler(scope, ctxt, cancelToken);

            // Finished.
            writer.WriteInfo(ResultsMessages.TestRunComplete);
        }

        /// <inheritdoc/>
        public override async ValueTask OnFeatureAsync(IServiceProvider scope, FeatureContext ctxt, Func<IServiceProvider, FeatureContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            var writer = GetConsoleWriter(scope);

            writer.WriteInfo(ResultsMessages.StartingFeature.FormatWith(ctxt.Feature.Name));

            await nextHandler(scope, ctxt, cancelToken);

            writer.WriteInfo(ResultsMessages.CompletedFeature);
        }

        /// <inheritdoc/>
        public override async ValueTask OnScenarioAsync(IServiceProvider scope, ScenarioContext ctxt, Func<IServiceProvider, ScenarioContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            var writer = GetConsoleWriter(scope);

            writer.WriteInfo(ResultsMessages.StartingScenario.FormatWith(ctxt.Scenario.Name));

            await nextHandler(scope, ctxt, cancelToken);

            if (ctxt.FailException is object)
            {
                Interlocked.Increment(ref failedScenarios);

                writer.WriteFailure(ResultsMessages.ScenarioFailed);

                if (ctxt.FailException is StepFailureException failure)
                {
                    writer.WriteFailure(ResultsMessages.StepFailed.FormatWith(failure.InnerException!.Message));
                }
                else if (ctxt.FailException is EventHandlingException eventFail)
                {
                    writer.WriteFailure(ResultsMessages.EventHandlerFailed.FormatWith(eventFail.InnerException!.Message));
                }

                if (ctxt.FailingStep is object)
                {
                    // Failure.
                    // Get the text of the step that failed.
                    var stepText = ctxt.FailingStep.Text;
                    writer.WriteFailure(ResultsMessages.FailingStep.FormatWith(stepText, ctxt.FailingStep.SourceLine));
                }
            }
            else
            {
                writer.WriteInfo(ResultsMessages.ScenarioPassed);
            }
        }

        private IConsoleResultsWriter GetConsoleWriter(IServiceProvider scope)
        {
            return scope.GetRequiredService<IConsoleResultsWriter>();
        }
    }
}
