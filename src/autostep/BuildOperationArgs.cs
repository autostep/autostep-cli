namespace AutoStep.CommandLine
{
    /// <summary>
    /// Base arguments class for all commands that build.
    /// </summary>
    public class BuildOperationArgs : BaseProjectArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether we should prompt for a .NET debugger before doing any work.
        /// </summary>
        public bool Attach { get; set; }
    }
}
