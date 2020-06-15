using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.Threading.Tasks;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Entry Point.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main Method.
        /// </summary>
        /// <param name="args">CLI args.</param>
        /// <returns>Async task.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Do not want an unexpected error to look like a proper crash, let's handle it more gracefully.")]
        public static async Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder()
                            .AddCommand(new RunCommand())
                            .AddCommand(new BuildCommand())
                            .AddCommand(new NewProjectCommand(AutoStepFiles.Default))
                            .UseParseErrorReporting()
                            .CancelOnProcessTermination()
                            .UseHelp()
                            .UseVersionOption()
                            .Build();

            var result = parser.Parse(args);

            var console = new SystemConsole();

            if (ShouldDisplayHelp(args, result))
            {
                var helpBuilder = new HelpBuilder(console);
                helpBuilder.Write(result.CommandResult.Command);
                return 0;
            }

            try
            {
                return await result.InvokeAsync();
            }
            catch (OperationCanceledException)
            {
                console.Out.WriteLine("Cancelled.");
                return 1;
            }
            catch (Exception ex)
            {
                console.Out.WriteLine($"Unexpected Error: {ex.Message}.");
                return 1;
            }
        }

        private static bool ShouldDisplayHelp(string[] args, ParseResult result) =>
            NoArgsPassed(args) ||
            !IsValidAutoStepCommand(result);

        private static bool NoArgsPassed(string[] args) => args.Length == 0;

        private static bool IsValidAutoStepCommand(ParseResult result) =>
            (result.CommandResult.Command is IAutostepCommand cmd) ? cmd.CommandIsValid() : false;
    }
}
