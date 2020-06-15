using System;
using AutoStep.CommandLine.Output;
using AutoStep.Elements.Metadata;
using AutoStep.Execution.Results;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Provides failure writing capabilities.
    /// </summary>
    internal class FailWriter
    {
        private readonly IConsoleWriter console;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailWriter"/> class.
        /// </summary>
        /// <param name="console">The console.</param>
        public FailWriter(IConsoleWriter console)
        {
            this.console = console;
        }

        /// <summary>
        /// Render all failures in the result set.
        /// </summary>
        /// <param name="resultSet">The results.</param>
        public void RenderFailures(IRunResultSet resultSet)
        {
            foreach (var feature in resultSet.Features)
            {
                if (feature.Passed)
                {
                    continue;
                }

                console.WriteFormatLine(ResultsMessages.Fail_Feature, feature.Feature.Name, feature.Feature.SourceName!);

                if (feature.FeatureFailureException is object)
                {
                    console.WriteErrorLine(ResultsMessages.Fail_FeatureException, 2);
                    RenderException(feature.FeatureFailureException, 2);
                }

                foreach (var scenario in feature.Scenarios)
                {
                    if (scenario.Passed)
                    {
                        continue;
                    }

                    WriteScenarioErrors(scenario);
                }
            }
        }

        private void WriteScenarioErrors(IScenarioResult scenario)
        {
            console.WriteIndent(2);
            console.Write(ResultsMessages.Bullet);
            console.WriteLine(scenario.Scenario.Name);

            if (scenario.IsScenarioOutline)
            {
                foreach (var invocation in scenario.Invocations)
                {
                    if (invocation.Passed)
                    {
                        continue;
                    }

                    console.WriteIndent(4);
                    console.Write(ResultsMessages.Fail_InvokeBullet);
                    console.WriteLine(invocation.InvocationName ?? ResultsMessages.Fail_AnonInvokeName);
                    WriteInvocationError(invocation, 6);
                }
            }
            else
            {
                // Just use the single invoke.
                var invoke = scenario.Invocations[0];
                WriteInvocationError(invoke, 4);
            }
        }

        private void WriteInvocationError(IScenarioInvocationResult invocation, int baseIndent)
        {
            if (invocation.FailingStep is IStepReferenceInfo failingStep)
            {
                console.WriteFormattedErrorLine(ResultsMessages.Fail_StepFailureOnLine, baseIndent, invocation.FailingStep.SourceLine);
                console.WriteFormattedErrorLine(ResultsMessages.Fail_StepRef, baseIndent + 2, failingStep.Type, failingStep.Text);

                if (invocation.OutlineVariables is object)
                {
                    foreach (var arg in failingStep.ReferencedVariables)
                    {
                        var variableValue = invocation.OutlineVariables.Get(arg);

                        if (variableValue is object)
                        {
                            console.WriteFormattedErrorLine(ResultsMessages.Fail_VariableDisplay, baseIndent + 4, arg, variableValue);
                        }
                    }
                }
            }

            if (invocation.FailException is object)
            {
                RenderException(invocation.FailException, baseIndent);
            }
        }

        private void RenderException(Exception failureException, int indent)
        {
            var actualException = failureException;

            // Go to the inner-most exception.
            while (actualException.InnerException is object)
            {
                actualException = actualException.InnerException;
            }

            console.WriteErrorLine(actualException.Message, indent);
        }
    }
}
