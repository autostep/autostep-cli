using AutoStep.Language;
using AutoStep.Language.Test;
using AutoStep.Projects;
using AutoStep.Projects.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.CommandLine
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var runCommand = new Command("run", "Execute tests.");

            runCommand.Handler = CommandHandler.Create(async (RunArgs args, IConsole console, CancellationToken cancelToken) => await Run(args, console, cancelToken));
            AddGlobalOptions(runCommand);

            var buildCommand = new Command("build", "Compile and Link tests.");
            buildCommand.Handler = CommandHandler.Create(async (BuildOperationArgs args, IConsole console, CancellationToken cancelToken) => await Build(args, console, cancelToken));
            AddGlobalOptions(buildCommand);

            var parser = new CommandLineBuilder()                            
                            .AddCommand(runCommand)
                            .AddCommand(buildCommand)
                            .UseParseErrorReporting()
                            .CancelOnProcessTermination()
                            .UseHelp()
                            .UseVersionOption()
                            .Build();

            var result = parser.Parse(args);

            if (args.Length == 0)
            {
                var helpBuilder = new HelpBuilder(new SystemConsole());
                helpBuilder.Write(result.CommandResult.Command);
                return 0;
            }

            return await result.InvokeAsync();
        }

        private static void AddGlobalOptions(Command cmd)
        {
            cmd.Add(new Option(new[] { "-d", "--directory" }, "Provide the base directory for the autostep project.")
            {
                Argument = new Argument<DirectoryInfo>(() => new DirectoryInfo(Environment.CurrentDirectory)).ExistingOnly()
            });

            cmd.Add(new Option(new[] { "-c", "--config" }, "Specify the autostep configuration file.")
            {
                Argument = new Argument<FileInfo>(() => new FileInfo("autostep.config.json")).ExistingOnly()
            });

            cmd.Add(new Option(new[] { "-v", "--verbose" }, "Indicates the verbosity level.")
            {
                Argument = new Argument<LogLevel>(() => LogLevel.Information)
            });
        }

        private class BaseGlobalArgs
        {
            public LogLevel Verbose { get; set; }
        }

        private class BaseProjectArgs : BaseGlobalArgs
        {
            public DirectoryInfo Directory { get; set; }

            public FileInfo Config { get; set; }
        }

        private class BuildOperationArgs : BaseProjectArgs
        {
        }

        private class RunArgs : BuildOperationArgs
        {
        }

        static ILoggerFactory GetLoggerFactory(BaseGlobalArgs args, IConsole console)
        {
            return LoggerFactory.Create(cfg =>
            {
                cfg.AddProvider(new ConsoleLoggerProvider(console));
                cfg.SetMinimumLevel(args.Verbose);                
            });
        }

        static async Task<int> Run(RunArgs args, IConsole console, CancellationToken cancelToken)
        {
            using var logFactory = GetLoggerFactory(args, console);

            var (project, success) = await InternalBuildAsync(args, logFactory, cancelToken);

            if (success)
            {
                // Now, run.
                var testRun = project.CreateTestRun();

                await testRun.ExecuteAsync(logFactory);
            }

            return success ? 0 : 1;
        }

        static async Task<int> Build(BuildOperationArgs args, IConsole console, CancellationToken cancelToken)
        {
            using var logFactory = GetLoggerFactory(args, console);

            var (_, success) = await InternalBuildAsync(args, logFactory, cancelToken);

            return success ? 0 : 1;
        }

        static async Task<(Project, bool)> InternalBuildAsync(BuildOperationArgs args, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            // Execution.
            var compiled = await CompileAsync(args, logFactory, cancelToken);

            var success = true;

            // Write results.
            WriteBuildResults(logFactory, compiled);

            if (compiled.Messages.Any(m => m.Level == CompilerMessageLevel.Error))
            {
                success = false;
            }

            var linked = Link(args, compiled.Output, logFactory, cancelToken);

            if (success && linked.Messages.Any(m => m.Level == CompilerMessageLevel.Error))
            {
                success = false;
            }

            // Write link result.
            WriteBuildResults(logFactory, linked);

            return (linked.Output, success);
        }

        static void WriteBuildResults(ILoggerFactory logFactory, ProjectCompilerResult result)
        {
            var logger = logFactory.CreateLogger<Program>();

            foreach (var message in result.Messages)
            {
                var logLevel = message.Level switch
                {
                    CompilerMessageLevel.Error => LogLevel.Error,
                    _ => LogLevel.Information
                };

                logger.Log(logLevel, message.ToString());
            }
        }

        static async Task<ProjectCompilerResult> CompileAsync(BuildOperationArgs args, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            // Create the project.
            var project = new Project();

            ProjectConfiguration config = ProjectConfiguration.Default;

            // Is there a config file?
            if (args.Config.Exists)
            {
                // Load the configuration file.
                // Deserialize from JSON.
                config = await LoadConfiguration(args.Config, cancelToken);
            }

            // Define file sets for interaction and test.
            var interactionFiles = FileSet.Create(args.Directory.FullName, config.Interactions);
            var testFiles = FileSet.Create(args.Directory.FullName, config.Tests);

            // Add the two file sets.
            project.MergeInteractionFileSet(interactionFiles);
            project.MergeTestFileSet(testFiles);

            // Now, compile.
            return await project.Compiler.CompileAsync(logFactory, cancelToken);
        }

        static ProjectCompilerResult Link(BuildOperationArgs args, Project project, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            return project.Compiler.Link(cancelToken);            
        }

        static ValueTask<ProjectConfiguration> LoadConfiguration(FileInfo file, CancellationToken cancelToken)
        {
            return ProjectConfiguration.Load(file.FullName, cancelToken);
        }
    }
}
