using AutoStep.Projects.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.CommandLine
{
    public abstract class AutoStepCommand<TArgs> : Command
        where TArgs : BaseGlobalArgs
    {
        public AutoStepCommand(string name, string description = null) : base(name, description)
        {
            AddCommonOptions();

            Handler = CommandHandler.Create(async (TArgs args, IConsole con, CancellationToken token) => {
                using var logFactory = GetLoggerFactory(args, con);
                return await Execute(args, GetLoggerFactory(args, con), token);
            });
        }

        protected ILoggerFactory GetLoggerFactory(BaseGlobalArgs args, IConsole console)
        {
            return LoggerFactory.Create(cfg =>
            {
                cfg.AddProvider(new ConsoleLoggerProvider(console));

                var minLevel = LogLevel.Information;
                
                if(args.Verbose)
                {
                    minLevel = LogLevel.Debug;
                }

                if(args.Diagnostic)
                {
                    minLevel = LogLevel.Trace;
                }

                cfg.SetMinimumLevel(minLevel);
            });
        }

        public abstract Task<int> Execute(TArgs args, ILoggerFactory logFactory, CancellationToken cancelToken);

        protected void LogConfigurationError(ILogger logger, ProjectConfigurationException configEx)
        {
            logger.LogError("Project Configuration Error: {0}", configEx.Message);
        }

        private void AddCommonOptions()
        {
            Add(new Option(new[] { "-d", "--directory" }, "Provide the base directory for the autostep project.")
            {
                Argument = new Argument<DirectoryInfo>(() => new DirectoryInfo(Environment.CurrentDirectory)).ExistingOnly()
            });

            Add(new Option(new[] { "-c", "--config" }, "Specify the autostep configuration file.")
            {
                Argument = new Argument<FileInfo>(() => null).ExistingOnly()
            });

            Add(new Option(new[] { "-v", "--verbose" }, "Enable verbose execution, providing more execution detail."));

            Add(new Option(new[] { "--diagnostic" }, "Enables diagnostic verbosity level, providing internal execution details."));
        }
    }
}
