using System;
using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using AutoStep.CommandLine.Output;
using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Defines a logger that writes to the console.
    /// </summary>
    internal class ConsoleLoggerProvider : ILoggerProvider
    {
        private readonly IConsoleWriter console;
        private readonly ConcurrentDictionary<string, ILogger> loggers = new ConcurrentDictionary<string, ILogger>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLoggerProvider"/> class.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="minimumLevel">The minimum log level.</param>
        public ConsoleLoggerProvider(IConsoleWriter console, LogLevel minimumLevel)
        {
            this.console = console;
            MinimumLevel = minimumLevel;
        }

        /// <summary>
        /// Gets the minimum log level for loggers.
        /// </summary>
        public LogLevel MinimumLevel { get; }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, n => new ConsoleLogger(this, categoryName));
        }

        /// <summary>
        /// Write a log item to the console.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The log level.</param>
        /// <param name="state">The state object.</param>
        /// <param name="exception">A relevant exception.</param>
        /// <param name="formatter">The formatter.</param>
        public void Log<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                using (console.EnterErrorBlock())
                {
                    console.WriteLine(formatter(state, exception));
                }
            }
            else if (logLevel >= LogLevel.Warning)
            {
                using (console.EnterWarnBlock())
                {
                    console.WriteLine(formatter(state, exception));
                }
            }
            else
            {
                console.WriteLine(formatter(state, exception));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Nothing to do here.
        }
    }
}
