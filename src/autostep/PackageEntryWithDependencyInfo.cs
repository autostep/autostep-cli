using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using System.Collections.Generic;

namespace AutoStep.CommandLine
{
    internal class PackageEntryWithDependencyInfo : PackageEntry
    {
        public PackageEntryWithDependencyInfo(
            SourcePackageDependencyInfo packageDepInfo,
            string packageFolder,
            string entryPoint,
            IEnumerable<string> libFiles,
            IEnumerable<string> contentFiles)
            : base(packageDepInfo.Id, packageDepInfo.Version.ToNormalizedString(), packageFolder, entryPoint, libFiles, contentFiles)
        {
            Dependencies = packageDepInfo.Dependencies;
        }

        public IEnumerable<PackageDependency> Dependencies { get; }
    }
}
