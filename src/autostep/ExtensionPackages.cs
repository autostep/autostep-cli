using System.Collections.Generic;
using System.IO;

namespace AutoStep.CommandLine
{
    internal class ExtensionPackages
    {
        public ExtensionPackages(string extensionsRootDir, IReadOnlyList<IPackageWithFiles> packages)
        {
            ExtensionsRootDir = extensionsRootDir;
            Packages = packages;
        }

        public string ExtensionsRootDir { get; }

        public IReadOnlyList<IPackageWithFiles> Packages { get; }
    }
}
