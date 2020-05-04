using System.Collections.Generic;
using System.IO;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// The base arguments for all commands that work on projects.
    /// </summary>
    public class BaseProjectArgs : BaseGlobalArgs
    {
        /// <summary>
        /// Gets or sets the project directory provided by the user (or the current directory if none was provided).
        /// </summary>
        /// <remarks>null-informed because the commands binder will never let this value be null.</remarks>
        public DirectoryInfo Directory { get; set; } = null!;

        /// <summary>
        /// Gets or sets the provided configuration file.
        /// </summary>
        public FileInfo? Config { get; set; }

        /// <summary>
        /// Gets or sets the set of provided configuration override options.
        /// </summary>
        /// <remarks>null-informed because the commands binder will never let this value be null.</remarks>
        public IEnumerable<KeyValuePair<string, string>> Option { get; set; } = null!;
    }
}
