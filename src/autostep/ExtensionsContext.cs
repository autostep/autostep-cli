using System;
using AutoStep.Extensions;
using AutoStep.Extensions.Abstractions;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Defines the loaded extensions context, containing the extension root directory, and the set of loaded extensions.
    /// </summary>
    internal sealed class ExtensionsContext : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionsContext"/> class.
        /// </summary>
        /// <param name="extensionRootDirectory">The root directory for all extension files.</param>
        /// <param name="loadedExtensions">The set of loaded extensions.</param>
        public ExtensionsContext(string extensionRootDirectory, ILoadedExtensions<IExtensionEntryPoint> loadedExtensions)
        {
            ExtensionRootDirectory = extensionRootDirectory;
            LoadedExtensions = loadedExtensions;
        }

        /// <summary>
        /// Gets the root extension directory.
        /// </summary>
        public string ExtensionRootDirectory { get; }

        /// <summary>
        /// Gets the set of loaded extensions.
        /// </summary>
        public ILoadedExtensions<IExtensionEntryPoint> LoadedExtensions { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            LoadedExtensions.Dispose();
        }
    }
}
