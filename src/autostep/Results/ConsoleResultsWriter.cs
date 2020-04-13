using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine.Results
{
    internal class ConsoleResultsWriter : IConsoleResultsWriter
    {
        private readonly ILogger logger;

        public ConsoleResultsWriter(ILoggerFactory logFactory)
        {
            logger = logFactory.CreateLogger("results");
        }

        public void WriteFailure(string message)
        {
            logger.LogError(message);
        }

        public void WriteInfo(string info)
        {
            logger.LogInformation(info);
        }
    }
}
