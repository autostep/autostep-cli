using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Base args structure for all commands.
    /// </summary>
    public class BaseGlobalArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether verbose mode is enabled.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether diagnostic mode is enabled.
        /// </summary>
        public bool Diagnostic { get; set; }
    }
}
