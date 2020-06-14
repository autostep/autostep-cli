using System;
using System.CommandLine.Rendering;
using AutoStep.Elements.Metadata;
using AutoStep.Execution.Results;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Provides failure writing capabilities.
    /// </summary>
    internal class FailWriter
    {
        private readonly ITerminal terminal;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailWriter"/> class.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        public FailWriter(ITerminal terminal)
        {
            this.terminal = terminal;
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

                terminal.WriteFormatLine(ResultsMessages.Fail_Feature, feature.Feature.Name, feature.Feature.SourceName!);

                if (feature.FeatureFailureException is object)
                {
                    terminal.WriteErrorLine(ResultsMessages.Fail_FeatureException, 2);
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
            terminal.WriteIndent(2);
            terminal.Write(ResultsMessages.Bullet);
            terminal.WriteLine(scenario.Scenario.Name);

            if (scenario.IsScenarioOutline)
            {
                foreach (var invocation in scenario.Invocations)
                {
                    if (invocation.Passed)
                    {
                        continue;
                    }

                    terminal.WriteIndent(4);
                    terminal.Write(ResultsMessages.Fail_InvokeBullet);
                    terminal.WriteLine(invocation.InvocationName ?? ResultsMessages.Fail_AnonInvokeName);
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
                terminal.WriteFormattedErrorLine(ResultsMessages.Fail_StepFailureOnLine, baseIndent, invocation.FailingStep.SourceLine);
                terminal.WriteFormattedErrorLine(ResultsMessages.Fail_StepRef, baseIndent + 2, failingStep.Type, failingStep.Text);

                if (invocation.OutlineVariables is object)
                {
                    foreach (var arg in failingStep.ReferencedVariables)
                    {
                        var variableValue = invocation.OutlineVariables.Get(arg);

                        if (variableValue is object)
                        {
                            terminal.WriteFormattedErrorLine(ResultsMessages.Fail_VariableDisplay, baseIndent + 4, arg, variableValue);
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

            terminal.WriteErrorLine(actualException.Message, indent);
        }
    }
}
