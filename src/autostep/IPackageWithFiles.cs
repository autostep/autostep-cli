using System.Collections.Generic;

namespace AutoStep.CommandLine
{
    internal interface IPackageWithFiles
    {
        string PackageId { get; }

        string PackageVersion { get; }

        string PackageFolder { get; }

        string EntryPoint { get; }

        IReadOnlyList<string> LibFiles { get; }

        IReadOnlyList<string> ContentFiles { get; }
    }
}
