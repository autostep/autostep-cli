using System;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.CommandLine.Output;
using AutoStep.Execution.Contexts;
using AutoStep.Execution.Results;
using Humanizer;
using Microsoft.Extensions.Configuration;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Exporter for writing a test summary to the console.
    /// </summary>
    public class ConsoleResultsExporter : IResultsExporter
    {
        private readonly FailWriter failWriter;
        private readonly IConsoleWriter console;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleResultsExporter"/> class.
        /// </summary>
        /// <param name="console">The console writer.</param>
        public ConsoleResultsExporter(IConsoleWriter console)
        {
            this.console = console;
            failWriter = new FailWriter(console);
        }

        /// <summary>
        /// Gets the run results.
        /// </summary>
        public IRunResultSet? RunResults { get; private set; }

        /// <inheritdoc/>
        public ValueTask ExportAsync(IServiceProvider scope, RunContext runContext, IRunResultSet results, CancellationToken cancelToken)
        {
            if (runContext is null)
            {
                throw new ArgumentNullException(nameof(runContext));
            }

            RunResults = results ?? throw new ArgumentNullException(nameof(results));

            // Write gap, then write table, then write plain-text summary.
            console.WriteLine();
            console.WriteLine();

            console.WriteHeading(ResultsMessages.Heading_TestResults, '=');

            console.WriteHeading(ResultsMessages.Heading_Environment, '-');

            // Environment info.
            PrintEnvironment(runContext.Configuration);

            console.WriteLine();

            console.WriteHeading(ResultsMessages.Heading_Summary, '-');

            var stats = GetStats(results);

            PrintStats(stats);

            console.WriteLine();

            if (!RunResults.AllPassed)
            {
                console.WriteHeading(ResultsMessages.Heading_Failures, '-');

                failWriter.RenderFailures(results);

                console.WriteLine();
            }

            return default;
        }

        private void PrintStats(ResultStats stats)
        {
            if (stats.FeaturesPassed == stats.FeaturesTotal)
            {
                // All tests passed.
                //   300/300 features passed.
                //   1200/1200 scenarios and outline examples passed.
                //   Average scenario took 12ms to run.
                console.WriteSuccess(ResultsMessages.AllTestsPassed);
                console.WriteLine();
                console.WriteIndent(2);
                console.Write(ResultsMessages.Bullet);
                console.WriteSuccess(stats.FeaturesPassed.ToString(CultureInfo.CurrentCulture));
                console.WriteFormatLine(ResultsMessages.NumberOfFeaturesPassed, stats.FeaturesTotal);
            }
            else
            {
                // One or more tests FAILED.
                //   300/400 features passed.
                //   1200/2440 scenario invocations passed.
                //   Average scenario took 20ms.
                console.WriteError(ResultsMessages.TestsFailed);
                console.WriteLine();
                console.WriteIndent(2);
                console.Write(ResultsMessages.Bullet);
                console.WriteError(stats.FeaturesPassed.ToString(CultureInfo.CurrentCulture));
                console.WriteFormatLine(ResultsMessages.NumberOfFeaturesPassed, stats.FeaturesTotal);
            }

            console.WriteIndent(2);
            console.Write(ResultsMessages.Bullet);

            if (stats.ScenarioInvocationsPassed == stats.TotalScenarioInvocations)
            {
                console.WriteSuccess(stats.ScenarioInvocationsPassed.ToString(CultureInfo.CurrentCulture));
            }
            else
            {
                console.WriteError(stats.ScenarioInvocationsPassed.ToString(CultureInfo.CurrentCulture));
            }

            console.WriteFormatLine(ResultsMessages.OutOfScenarios, stats.TotalScenarioInvocations);

            console.WriteIndent(2);
            console.Write(ResultsMessages.Bullet);
            console.WriteFormatLine(ResultsMessages.AverageScenarioRun, stats.AverageElapsed.Humanize());
        }

        private class ResultStats
        {
            public int FeaturesTotal { get; set; }

            public int FeaturesPassed { get; set; }

            public int FeaturesFailed { get; set; }

            public int TotalScenarioInvocations { get; set; }

            public int ScenarioInvocationsPassed { get; set; }

            public int ScenarioInvocationsFailed { get; set; }

            public TimeSpan AverageElapsed { get; set; }
        }

        private ResultStats GetStats(IRunResultSet resultSet)
        {
            var stats = new ResultStats();

            TimeSpan elapsedTotal = TimeSpan.Zero;

            foreach (var feature in resultSet.Features)
            {
                stats.FeaturesTotal++;
                var passed = true;

                foreach (var scenarioInvocation in feature.Scenarios.SelectMany(x => x.Invocations))
                {
                    stats.TotalScenarioInvocations++;

                    if (scenarioInvocation.Passed)
                    {
                        stats.ScenarioInvocationsPassed++;
                    }
                    else
                    {
                        stats.ScenarioInvocationsFailed++;
                        passed = false;
                    }

                    elapsedTotal = elapsedTotal.Add(scenarioInvocation.Elapsed);
                }

                if (passed && feature.FeatureFailureException is null)
                {
                    stats.FeaturesPassed++;
                }
                else
                {
                    stats.FeaturesFailed++;
                }
            }

            // Divide for the average.
            stats.AverageElapsed = elapsedTotal.Divide(stats.TotalScenarioInvocations);

            return stats;
        }

        private void PrintEnvironment(IConfiguration configuration)
        {
            PrintEnvironmentLine(ResultsMessages.Environment_Computer, Environment.MachineName);
            PrintEnvironmentLine(ResultsMessages.Environment_OS, RuntimeInformation.OSDescription);

            var runConfig = configuration.GetValue<string?>("runConfig", null);

            if (runConfig is object)
            {
                PrintEnvironmentLine(ResultsMessages.Environment_RunConfig, runConfig);
            }
        }

        private void PrintEnvironmentLine(string name, string value)
        {
            console.WriteIndent(2);
            console.Write(ResultsMessages.Bullet);
            console.WriteFormatLine(ResultsMessages.Environment_Format, name, value);
        }
    }
}
