using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Writes results to a console.
    /// </summary>
    internal class ConsoleResultsWriter : IConsoleResultsWriter
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleResultsWriter"/> class.
        /// </summary>
        /// <param name="logFactory">The log factory.</param>
        public ConsoleResultsWriter(ILoggerFactory logFactory)
        {
            logger = logFactory.CreateLogger("results");
        }

        /// <inheritdoc/>
        public void WriteFailure(string message)
        {
            logger.LogError(message);
        }

        /// <inheritdoc/>
        public void WriteInfo(string message)
        {
            logger.LogInformation(message);
        }
    }
}
