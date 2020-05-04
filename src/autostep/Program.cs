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

            if (args.Length == 0)
            {
                var console = new SystemConsole();
                var helpBuilder = new HelpBuilder(console);
                helpBuilder.Write(result.CommandResult.Command);
                return 0;
            }

            return await result.InvokeAsync();
        }
    }
}
