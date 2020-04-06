using AutoStep.Extensions;
using AutoStep.Projects.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace AutoStep.CommandLine
{
    public class LoadedExtensionSet : ExtensionSet
    {
        private readonly ExtLoadContext loadContext;
        private readonly ExtensionPackages extPackages;

        private bool isDisposed;
        private readonly IReadOnlyList<string> requiredPackages;

        internal LoadedExtensionSet(ProjectConfiguration projectConfig, ExtensionPackages packages)
            : base(projectConfig)
        {
            loadContext = new ExtLoadContext(packages);
            isDisposed = false;
            this.requiredPackages = projectConfig.Extensions.Select(e => e.Value.Name).ToList();
            this.extPackages = packages;
            this.ExtensionsRootDir = packages.ExtensionsRootDir;
        }

        public string ExtensionsRootDir { get; private set; }

        public void Load(ILoggerFactory loggerFactory)
        {
            foreach (var package in extPackages.Packages)
            {
                if (package.EntryPoint is null)
                {
                    ThrowIfRequestedExtensionPackage(package);
                    continue;
                }

                var entryPointAssembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(package.EntryPoint, package.PackageFolder));

                // Find the type that implements IProjectExtension.
                var extensionType = entryPointAssembly.GetExportedTypes()
                                                      .FirstOrDefault(t =>
                                                        typeof(IProjectExtension).IsAssignableFrom(t) &&
                                                         t.IsClass && !t.IsAbstract);

                if (extensionType is null)
                {
                    ThrowIfRequestedExtensionPackage(package);
                    continue;
                }

                var getValidConstructor = extensionType.GetConstructors().Where(IsValidConstructor).FirstOrDefault();

                if (getValidConstructor is null)
                {
                    throw new ProjectConfigurationException($"Cannot load the entry point for the {package.PackageId} extension. " +
                                                             "Extension entry points must implement the IProjectExtension interface, and have a public constructor" +
                                                             "with only ILoggerFactory (optionally) as a constructor argument.");
                }

                Add(Construct(extensionType, loggerFactory));
            }

        }
        public override void Dispose()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(LoadedExtensionSet));
            }

            loadContext.Unload();

            isDisposed = true;
        }

        private void ThrowIfRequestedExtensionPackage(IPackageWithFiles package)
        {
            if (requiredPackages.Contains(package.PackageId))
            {
                throw new ProjectConfigurationException($"Could not locate entry point for requested extension {package.PackageId}.");
            }
        }

        private IProjectExtension Construct(Type extensionType, ILoggerFactory logFactory)
        {
            var constructor = extensionType.GetConstructor(new[] { typeof(ILoggerFactory) });

            if (constructor is object)
            {
                return (IProjectExtension)constructor.Invoke(new[] { logFactory });
            }

            return (IProjectExtension)Activator.CreateInstance(extensionType);
        }

        private bool IsValidConstructor(ConstructorInfo constructor)
        {
            var constructorArgs = constructor.GetParameters();

            if (constructorArgs.Any(x => x.ParameterType != typeof(ILoggerFactory)))
            {
                return false;
            }

            return true;
        }

        private class ExtLoadContext : AssemblyLoadContext
        {
            private readonly ExtensionPackages extFiles;
            
            public ExtLoadContext(ExtensionPackages extFiles)
                : base(true)
            {
                this.extFiles = extFiles;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                var dllName = assemblyName.Name + ".dll";

                // Find all DLLs that match.
                foreach (var package in extFiles.Packages)
                {
                    var matchingFile = package.LibFiles.FirstOrDefault(f => Path.GetFileName(f) == dllName);

                    if (matchingFile is object)
                    {
                        // Got it.
                        return LoadFromAssemblyPath(Path.GetFullPath(matchingFile, package.PackageFolder));
                    }
                }

                return null;
            }
        }
    }
}
