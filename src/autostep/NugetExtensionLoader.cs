using AutoStep.Extensions;
using AutoStep.Projects.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ILogger = NuGet.Common.ILogger;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// This class is liberally inspired by the post from Martin Bjorkstrom (https://martinbjorkstrom.com/posts/2018-09-19-revisiting-nuget-client-libraries), 
    /// and associated gist (https://gist.github.com/mholo65/ad5776c36559410f45d5dcd0181a5c64).
    /// </summary>
    internal class NugetExtensionLoader : BaseExtensionLoader
    {
        const string DefaultCISource = "https://f.feedz.io/autostep/ci/nuget/index.json";
        private readonly NuGetFramework framework;
        private readonly SourceCacheContext sourceCacheContext;
        private readonly SourceRepositoryProvider sourceRepositoryProvider;
        private readonly ILogger nugetLogger;
        private readonly string packageDirectory;
        private readonly DependencyContext depContext;
        private readonly ISettings settings;

        public NugetExtensionLoader(BaseProjectArgs projectArgs, string extensionsFolder, ILoggerFactory logFactory)
        {
            framework = NuGetFramework.ParseFolder("netcoreapp3.1");

            sourceCacheContext = new SourceCacheContext();

            settings = Settings.LoadDefaultSettings(projectArgs.Directory.FullName);

            var packageSourceProvider = new PackageSourceProvider(settings);

            packageSourceProvider.AddPackageSource(new PackageSource(DefaultCISource));

            sourceRepositoryProvider = new SourceRepositoryProvider(packageSourceProvider, Repository.Provider.GetCoreV3());

            nugetLogger = new NuGetLogger(logFactory);

            // Determine the packages on which we already depend.
            // TODO: This may need to be loaded from an isolated file.
            // DependencyContextJsonReader.Read
            depContext = DependencyContext.Default;

            packageDirectory = extensionsFolder;
        }

        public override async Task<ExtensionPackages> ResolveExtensionPackagesAsync(ProjectConfiguration projConfig, string outputDependencyFile, CancellationToken cancelToken)
        {
            var repositories = sourceRepositoryProvider.GetRepositories();

            var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
            var listTargetIds = new List<string>();

            foreach (var package in projConfig.Extensions.Values)
            {
                // Determine the correct version.
                var packageIdentity = await GetPackageIdentity(package, repositories, cancelToken);

                if (packageIdentity is null)
                {
                    throw new ProjectConfigurationException($"Could not locate extension package {package.Name}");
                }

                listTargetIds.Add(packageIdentity.Id);

                await GetPackageDependencies(packageIdentity, framework, sourceCacheContext, nugetLogger, repositories, availablePackages, cancelToken);
            }

            var resolverContext = new PackageResolverContext(
                   DependencyBehavior.Lowest,
                   listTargetIds,
                   Enumerable.Empty<string>(),
                   Enumerable.Empty<PackageReference>(),
                   Enumerable.Empty<PackageIdentity>(),
                   availablePackages,
                   sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
                   nugetLogger);

            var resolver = new PackageResolver();
            var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));

            var packagePathResolver = new PackagePathResolver(packageDirectory, true);
            var packageExtractionContext = new PackageExtractionContext(
                PackageSaveMode.Defaultv3,
                XmlDocFileSaveMode.Skip,
                ClientPolicyContext.GetClientPolicy(settings, nugetLogger),
                nugetLogger);

            var frameworkReducer = new FrameworkReducer();

            var packageEntries = new List<PackageEntryWithDependencyInfo>();

            // Purge all the extension directories in the main one. We're going to get them from scratch anyway,
            // and this will make sure we clear out old package folders.
            foreach (var packageDir in Directory.GetDirectories(packageDirectory))
            {
                Directory.Delete(packageDir, true);
            }

            foreach (var package in packagesToInstall)
            {
                PackageReaderBase packageReader;
                var installedPath = packagePathResolver.GetInstalledPath(package);

                var downloadResource = await package.Source.GetResourceAsync<DownloadResource>(cancelToken);
                var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                    package,
                    new PackageDownloadContext(sourceCacheContext),
                    SettingsUtility.GetGlobalPackagesFolder(settings),
                    nugetLogger,
                    cancelToken);

                await PackageExtractor.ExtractPackageAsync(
                    downloadResult.PackageSource,
                    downloadResult.PackageStream,
                    packagePathResolver,
                    packageExtractionContext,
                    cancelToken);

                packageReader = downloadResult.PackageReader;

                // Get it again.
                installedPath = packagePathResolver.GetInstalledPath(package);

                var libItems = GetFrameworkFiles(await packageReader.GetLibItemsAsync(cancelToken));

                // Define the entry point (DLL with the same name as the package).
                string entryPoint = libItems.FirstOrDefault(f => Path.GetFileName(f) == package.Id + ".dll");

                var packageEntry = new PackageEntryWithDependencyInfo(
                    package,
                    installedPath,
                    entryPoint,
                    libItems,
                    GetFrameworkFiles(await packageReader.GetContentItemsAsync(cancelToken))
                );

                packageEntries.Add(packageEntry);
            }

            var newDepContext = new DependencyContext(
                depContext.Target,
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                packageEntries.Select(p => new RuntimeLibrary(
                    listTargetIds.Contains(p.PackageId) ? ExtensionRuntimeLibraryType.RootPackage : ExtensionRuntimeLibraryType.Dependency,
                    p.PackageId,
                    p.PackageVersion,
                    null,
                    new[] {
                        new RuntimeAssetGroup(
                            depContext.Target.Runtime,
                            p.LibFiles.Where(f => Path.GetExtension(f) == ".dll")
                            .Select(f => GetRuntimeFile(p, f)))
                    },
                    new List<RuntimeAssetGroup>(),
                    Enumerable.Empty<ResourceAssembly>(),
                    p.Dependencies.Select(d => packageEntries.FirstOrDefault(p => p.PackageId == d.Id))
                                  .Where(d => d is object)
                                  .Select(d => new Dependency(d.PackageId, d.PackageVersion)),
                    true
                    )),
                Enumerable.Empty<RuntimeFallbacks>()
            );

            // Write the dependency files.
            var dependencyWriter = new DependencyContextWriter();

            using (var fileStream = File.Open(outputDependencyFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                dependencyWriter.Write(newDepContext, fileStream);
            }

            return new ExtensionPackages(packageDirectory, packageEntries);
        }

        private static RuntimeFile GetRuntimeFile(IPackageWithFiles packageEntry, string file)
        {
            var fullPath = Path.GetFullPath(file, packageEntry.PackageFolder);

            var assemblyName = AssemblyName.GetAssemblyName(fullPath);

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);

            return new RuntimeFile(file, assemblyName.Version.ToString(), fileVersionInfo.FileVersion);
        }

        private async Task<PackageIdentity> GetPackageIdentity(ProjectExtensionConfiguration extConfig, IEnumerable<SourceRepository> repositories, CancellationToken cancelToken)
        {
            foreach (var sourceRepository in repositories)
            {
                var findPackageResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

                var allVersions = (await findPackageResource.GetAllVersionsAsync(extConfig.Name, sourceCacheContext, nugetLogger, cancelToken)).ToList();

                NuGetVersion selected;

                if (extConfig.Version != null)
                {
                    if (!VersionRange.TryParse(extConfig.Version, out var range))
                    {
                        throw new ProjectConfigurationException($"Invalid extension version range specified for the {extConfig.Name} extension.");
                    }

                    // Find the best package version match for the range. 
                    // Consider pre-release versions, but only if the extension is configured to use them.
                    var bestVersion = range.FindBestMatch(allVersions.Where(v => extConfig.PreRelease || !v.IsPrerelease));

                    selected = bestVersion;
                }
                else
                {
                    // Todo, use the pre-release setting.
                    selected = allVersions.LastOrDefault(v => v.IsPrerelease == extConfig.PreRelease);
                }

                if (selected is object)
                {
                    return new PackageIdentity(extConfig.Name, selected);
                }
            }

            return null;
        }

        private async Task GetPackageDependencies(PackageIdentity package,
                NuGetFramework framework,
                SourceCacheContext cacheContext,
                ILogger logger,
                IEnumerable<SourceRepository> repositories,
                ISet<SourcePackageDependencyInfo> availablePackages,
                CancellationToken cancelToken)
        {
            if (availablePackages.Contains(package))
            {
                return;
            }

            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package, framework, cacheContext, logger, CancellationToken.None);

                if (dependencyInfo == null) continue;

                // Filter the dependency info.
                var actualSourceDep = new SourcePackageDependencyInfo(
                    dependencyInfo.Id,
                    dependencyInfo.Version,
                    dependencyInfo.Dependencies.Where(dep => depContext.RuntimeLibraries.All(r => r.Name != dep.Id)),
                    dependencyInfo.Listed,
                    dependencyInfo.Source);

                availablePackages.Add(actualSourceDep);

                foreach (var dependency in actualSourceDep.Dependencies)
                {
                    await GetPackageDependencies(
                        new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                        framework, cacheContext, logger, repositories, availablePackages, cancelToken);                    
                }

                break;
            }
        }

        public override void Dispose()
        {
            sourceCacheContext.Dispose();
        }
    }
}
