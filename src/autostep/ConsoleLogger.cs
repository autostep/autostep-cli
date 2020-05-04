using System;
using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Exposes a logger for writing to the console.
    /// </summary>
    internal class ConsoleLogger : ILogger
    {
        private readonly ConsoleLoggerProvider provider;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        /// <param name="provider">The backing provider.</param>
        /// <param name="name">The logger name.</param>
        public ConsoleLogger(ConsoleLoggerProvider provider, string name)
        {
            this.provider = provider;
            this.name = name;
        }

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state)
        {
            // Not using scopes right now.
            return new EmptyDisposable();
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= provider.MinimumLevel;
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            provider.Log(logLevel, state, exception, formatter);
        }

        private class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
