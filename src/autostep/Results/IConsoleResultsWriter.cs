namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Defines a service that writes content to the console.
    /// </summary>
    internal interface IConsoleResultsWriter
    {
        /// <summary>
        /// Write an info message.
        /// </summary>
        /// <param name="message">Message text.</param>
        void WriteInfo(string message);

        /// <summary>
        /// Write a failure message.
        /// </summary>
        /// <param name="message">Message text.</param>
        void WriteFailure(string message);
    }
}
