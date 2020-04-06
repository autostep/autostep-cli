using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;

namespace AutoStep.CommandLine
{
    internal class ConsoleLoggerProvider : ILoggerProvider
    {
        private readonly IConsole console;
        private readonly ITerminal terminal;
        private readonly ConcurrentDictionary<string, ILogger> loggers = new ConcurrentDictionary<string, ILogger>();

        public ConsoleLoggerProvider(IConsole console)
        {
            this.console = console;
            this.terminal = console as ITerminal;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, n => new ConsoleLogger(this, categoryName));
        }

        public void Log<TState>(string name, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel >= LogLevel.Error && terminal is object)
            {   
                terminal.ForegroundColor = ConsoleColor.Red;
                terminal.Out.WriteLine(formatter(state, exception));
                terminal.ResetColor();
            }
            else if(logLevel >= LogLevel.Warning && terminal is object)
            {
                terminal.ForegroundColor = ConsoleColor.DarkYellow;
                terminal.Out.WriteLine(formatter(state, exception));
                terminal.ResetColor();
            }
            else
            {
                console.Out.WriteLine(formatter(state, exception));
            }
        }

        public void Dispose()
        {
            // Nothing to do here.
        }
    }

    internal class ConsoleLogger : ILogger
    {
        private readonly ConsoleLoggerProvider provider;
        private readonly string name;

        public ConsoleLogger(ConsoleLoggerProvider provider, string name)
        {
            this.provider = provider;
            this.name = name;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            provider.Log(name, logLevel, eventId, state, exception, formatter);
        }
    }
}