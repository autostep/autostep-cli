using AutoStep.Extensions;
using AutoStep.Projects.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.CommandLine
{
    internal class CachedExtensionLoader : BaseExtensionLoader
    {
        private readonly string extensionsFolder;
        private readonly ILogger<CachedExtensionLoader> logger;

        public CachedExtensionLoader(BaseProjectArgs projectArgs, string extensionsFolder, ILoggerFactory logFactory)
        {
            this.extensionsFolder = extensionsFolder;
            this.logger = logFactory.CreateLogger<CachedExtensionLoader>();
        }

        public override Task<ExtensionPackages> ResolveExtensionPackagesAsync(ProjectConfiguration projConfig, string dependencyDataFileName, CancellationToken cancelToken)
        {
            ExtensionPackages resolvedPackages = null;

            if (File.Exists(dependencyDataFileName))
            {
                logger.LogDebug("Extension dependency cache data exists; verifying.");

                // Load the dependency context.
                var dependencyContextLdr = new DependencyContextJsonReader();

                using (var stream = File.OpenRead(dependencyDataFileName))
                {
                    var depContext = dependencyContextLdr.Read(stream);
                    var cacheValid = ValidateRootPackages(projConfig, depContext);

                    if(cacheValid)
                    {
                        logger.LogDebug("Set of root packages is valid; verifying package files.");

                        // Now we need to validate the files and build the package set.
                        if(TryGetPackageSet(depContext, out var packageSet))
                        {
                            logger.LogDebug("Package files available. Cache valid, using it.");
                            resolvedPackages = new ExtensionPackages(extensionsFolder, packageSet);
                        }
                        else
                        {
                            logger.LogDebug("Package files not all available; ignoring cache.");
                        }
                    }
                    else
                    {
                        logger.LogDebug("Set of root packages not valid; ignoring cache.");
                    }
                }
            }

            return Task.FromResult(resolvedPackages);
        }

        private bool TryGetPackageSet(DependencyContext depContext, out IReadOnlyList<IPackageWithFiles> packageSet)
        {
            var loadedPackages = new List<IPackageWithFiles>();
            var isValid = true;
            var packagePathResolver = new PackagePathResolver(extensionsFolder, true);

            foreach (var runtimeLib in depContext.RuntimeLibraries)
            {
                var packageId = new PackageIdentity(runtimeLib.Name, NuGetVersion.Parse(runtimeLib.Version));

                // First off, does the package folder exist?
                var packageDir = packagePathResolver.GetInstalledPath(packageId);

                if(packageDir is object)
                {
                    // We have a package directory; now we need to validate the library files exist.
                    var nugetFolderReader = new PackageFolderReader(packageDir);

                    var libFiles = GetFrameworkFiles(nugetFolderReader.GetLibItems());
                    var contentFiles = GetFrameworkFiles(nugetFolderReader.GetContentItems());

                    // Verify that the libraries match the libs specified in the cache.
                    var cachedAssemblyPaths = GetRuntimeLibraryLibPaths(runtimeLib) ?? Enumerable.Empty<string>();

                    foreach (var cachedFile in cachedAssemblyPaths)
                    {
                        if(!libFiles.Contains(cachedFile))
                        {
                            // Cached file not present in folder, corrupt package folder most likely.
                            logger.LogDebug("Cannot find library file {0} for cached package {1}.", cachedFile);
                            isValid = false;
                        }
                    }

                    var entryPoint = libFiles.FirstOrDefault(f => Path.GetFileName(f) == runtimeLib.Name + ".dll");

                    loadedPackages.Add(new PackageEntry(runtimeLib.Name, runtimeLib.Version, packageDir, entryPoint, libFiles, contentFiles));
                }
                else
                {
                    logger.LogDebug("Install directory for cached package {0} does not exist.", runtimeLib.Path);
                    isValid = false;
                }
            }

            packageSet = loadedPackages;
            return isValid;
        }

        /// <summary>
        /// Look for all runtime libraries that are declared as 'rootPackage' type.
        /// If the set of root packages have changed from the list of extensions, we can throw away
        /// the cache and retrieve again.
        /// 
        /// If an explicit version is set in the extensions config, and that version is no longer met by
        /// the version in the cache, then we will also go again.
        /// </summary>
        /// <param name="projConfig"></param>
        /// <param name="depContext"></param>
        /// <returns></returns>
        private bool ValidateRootPackages(ProjectConfiguration projConfig, DependencyContext depContext)
        {
           
            var rootRuntimeLibraries = depContext.RuntimeLibraries.Where(x => x.Type == ExtensionRuntimeLibraryType.RootPackage).ToDictionary(x => x.Name);            
            var isValid = true;

            foreach (var extConfig in projConfig.Extensions.Values)
            {
                // Look for a runtime library.
                if(rootRuntimeLibraries.TryGetValue(extConfig.Name, out var lib))
                {
                    // Remove the item from the dictionary (to give us our 'extras' list at the end of the loop).
                    rootRuntimeLibraries.Remove(extConfig.Name);

                    // Ok, so we have a cached entry; is it the right version?
                    if(NuGetVersion.TryParse(lib.Version, out var parsedCacheVersion))
                    {
                        if(extConfig.Version is object)
                        {
                            if (VersionRange.TryParse(extConfig.Version, out var range))
                            {
                                if(!range.Satisfies(parsedCacheVersion))
                                {
                                    // Range not satisfied.
                                    logger.LogDebug("Version range specified in the {0} extension configuration, {1}, is not a match for the cached version, {2}.", extConfig.Name, extConfig.Version, lib.Version);
                                    isValid = false;
                                }
                            }
                            else
                            {
                                throw new ProjectConfigurationException($"Invalid extension version range specified for the {extConfig.Name} extension.");
                            }
                        }    
                        
                        if(extConfig.PreRelease == false && parsedCacheVersion.IsPrerelease)
                        {
                            // Pre-releases no longer allowed.
                            logger.LogDebug("The cached extension version for {0} is a pre-release, but the extension configuration does not allow pre-releases.", extConfig.Name);
                            isValid = false;
                        }
                    }
                    else
                    {
                        logger.LogDebug("Bad version in dependency cache for {0}", extConfig.Name);
                        isValid = false;
                    }
                }
                else
                {
                    logger.LogDebug("No entry in dependency cache for {0}", extConfig.Name);
                    isValid = false;
                }

                if(isValid)
                {
                    logger.LogDebug("Cached extension dependency info for {0} is valid.", extConfig.Name);
                }
            }

            foreach (var extraRootPackage in rootRuntimeLibraries)
            {
                logger.LogDebug("Extension dependency cache contains extension package {0} that has not been requested by configuration.", extraRootPackage.Key);
                isValid = false;
            }

            return isValid;
        }
    }
}
