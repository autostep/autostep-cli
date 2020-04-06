using AutoStep.Extensions;
using AutoStep.Projects.Configuration;
using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.CommandLine
{
    internal abstract class BaseExtensionLoader : IDisposable
    {
        private readonly string runtimeTarget;
        private readonly NuGetFramework framework;
        private readonly FrameworkReducer frameworkReducer;

        protected BaseExtensionLoader()
        {
            // Use the entry assembly framework name.
            var entryAssembly = Assembly.GetEntryAssembly();
            var targetFramework = entryAssembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            
            runtimeTarget = targetFramework ?? ".NETCoreApp,Version=3.1";

            framework = NuGetFramework.ParseFrameworkName(runtimeTarget, DefaultFrameworkNameProvider.Instance);
            frameworkReducer = new FrameworkReducer();
        }

        public abstract Task<ExtensionPackages> ResolveExtensionPackagesAsync(ProjectConfiguration projConfig, string dependencyDataFileName, CancellationToken cancelToken);

        public virtual void Dispose()
        {
        }

        protected IEnumerable<string> GetFrameworkFiles(IEnumerable<FrameworkSpecificGroup> frameworkGroup)
        {
            var nearest = frameworkReducer.GetNearest(framework, frameworkGroup.Select(x => x.TargetFramework));

            var selectedItems = frameworkGroup.Where(x => x.TargetFramework.Equals(nearest))
                                              .SelectMany(x => x.Items);

            return selectedItems;
        }

        protected IEnumerable<string> GetRuntimeLibraryLibPaths(RuntimeLibrary library)
        {
            var targetRuntimeGroup = library.RuntimeAssemblyGroups.FirstOrDefault(t => string.IsNullOrEmpty(t.Runtime) || t.Runtime == runtimeTarget);

            if(targetRuntimeGroup == null)
            {
                return null;
            }

            return targetRuntimeGroup.AssetPaths;
        }
    }
}
