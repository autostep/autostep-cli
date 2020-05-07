namespace AutoStep.CommandLine
{
    /// <summary>
    /// Arguments for a test run operation.
    /// </summary>
    public class RunArgs : BuildOperationArgs
    {
        /// <summary>
        /// Gets or sets the selected run configuration.
        /// </summary>
        public string? RunConfig { get; set; }
    }
}
