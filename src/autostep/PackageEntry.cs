using System.Collections.Generic;
using System.Linq;

namespace AutoStep.CommandLine
{
    internal class PackageEntry : IPackageWithFiles
    {
        public PackageEntry(
            string packageId,
            string packageVersion,
            string packageFolder,
            string entryPoint,
            IEnumerable<string> libFiles,
            IEnumerable<string> contentFiles)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            PackageFolder = packageFolder;
            LibFiles = libFiles.ToList();
            ContentFiles = contentFiles.ToList();
            EntryPoint = entryPoint;
        }

        public string PackageFolder { get; }

        public string PackageId { get; }

        public string PackageVersion { get; }

        public IReadOnlyList<string> LibFiles { get; }

        public IReadOnlyList<string> ContentFiles { get; }

        public string EntryPoint { get; }
    }
}
