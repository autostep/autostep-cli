using System;
using System.Collections.Generic;
using System.Text;
using AutoStep.Extensions;
using AutoStep.Extensions.Abstractions;

namespace AutoStep.CommandLine
{
    public class ExtensionsContext : IDisposable
    {
        public ExtensionsContext(string extensionRootDirectory, ILoadedExtensions<IExtensionEntryPoint> loadedExtensions)
        {
            ExtensionRootDirectory = extensionRootDirectory;
            LoadedExtensions = loadedExtensions;
        }

        public string ExtensionRootDirectory { get; }

        public ILoadedExtensions<IExtensionEntryPoint> LoadedExtensions { get; }

        public void Dispose()
        {
            LoadedExtensions.Dispose();
        }
    }
}
