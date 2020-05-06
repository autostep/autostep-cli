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
                            .UseParseErrorReporting()
                            .CancelOnProcessTermination()
                            .UseHelp()
                            .ConfigureConsole(context =>
                            {
                                var console = context.Console;

                                var terminal = console.GetTerminal(false, OutputMode.Ansi);

                                if (terminal is object)
                                {
                                    return terminal;
                                }

                                return console;
                            })
                            .UseVersionOption()
                            .Build();

            var result = parser.Parse(args);

            var console = new SystemConsole();

            if (args.Length == 0)
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
    }
}
