using System;
using System.Threading.Tasks;
using AutoStep.Execution;
using AutoStep.Execution.Contexts;
using AutoStep.Execution.Events;
using Microsoft.Extensions.DependencyInjection;

namespace AutoStep.CommandLine.Results
{
    public class CommandLineResultsCollector : BaseEventHandler
    {
        public override async ValueTask OnExecute(IServiceProvider scope, RunContext ctxt, Func<IServiceProvider, RunContext, ValueTask> nextHandler)
        {
            var writer = GetConsoleWriter(scope);

            writer.WriteInfo("Test Run Starting");

            await nextHandler(scope, ctxt);

            // Finished.
            writer.WriteInfo("Test Run Complete");
        }

        public override async ValueTask OnFeature(IServiceProvider scope, FeatureContext ctxt, Func<IServiceProvider, FeatureContext, ValueTask> nextHandler)
        {
            var writer = GetConsoleWriter(scope);

            writer.WriteInfo($"Starting Feature: {ctxt.Feature.Name}");

            await nextHandler(scope, ctxt);

            writer.WriteInfo($"Completed Feature");
        }

        public override async ValueTask OnScenario(IServiceProvider scope, ScenarioContext ctxt, Func<IServiceProvider, ScenarioContext, ValueTask> nextHandler)
        {
            var writer = GetConsoleWriter(scope);

            writer.WriteInfo($"Starting Scenario: {ctxt.Scenario.Name}");

            await nextHandler(scope, ctxt);

            if (ctxt.FailException is object)
            {
                writer.WriteFailure("Scenario Failed");

                if (ctxt.FailException is StepFailureException failure)
                {
                    writer.WriteFailure($"  Step Failed: {failure.InnerException!.Message}");
                }
                else if (ctxt.FailException is EventHandlingException eventFail)
                {
                    writer.WriteFailure($"  Event Handler Failed: {eventFail.InnerException!.Message}");
                }

                if (ctxt.FailingStep is object)
                {
                    // Failure.
                    // Get the text of the step that failed.
                    var stepText = ctxt.FailingStep.Text;
                    writer.WriteFailure($"  Failing Step: {stepText}, on line {ctxt.FailingStep.SourceLine}");
                }
            }
            else
            {
                writer.WriteInfo("Scenario Passed");
            }
        }

        private IConsoleResultsWriter GetConsoleWriter(IServiceProvider scope)
        {
            return scope.GetRequiredService<IConsoleResultsWriter>();
        }
    }
}
