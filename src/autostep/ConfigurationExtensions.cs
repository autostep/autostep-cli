using System;
using System.Linq;
using AutoStep.Extensions;
using Microsoft.Extensions.Configuration;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Configuration helper methods.
    /// </summary>
    internal static class ConfigurationExtensions
    {
        private static readonly string[] DefaultTestGlobs = new[] { "**/*.as" };
        private static readonly string[] DefaultInteractionGlobs = new[] { "**/*.asi" };

        /// <summary>
        /// Get the set of globs for test files.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>A glob set.</returns>
        public static string[] GetTestFileGlobs(this IConfiguration config)
        {
            var globSection = config.GetSection("tests");

            if (globSection.Exists())
            {
                return globSection.Get<string[]>();
            }
            else
            {
                return DefaultTestGlobs;
            }
        }

        /// <summary>
        /// Get the set of globs for interaction files.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>A glob set.</returns>
        public static string[] GetInteractionFileGlobs(this IConfiguration config)
        {
            var globSection = config.GetSection("interactions");

            if (globSection.Exists())
            {
                return globSection.Get<string[]>();
            }
            else
            {
                return DefaultInteractionGlobs;
            }
        }

        /// <summary>
        /// Gets the set of configured package extensions.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns>Array of package configurations.</returns>
        public static PackageExtensionConfiguration[] GetPackageExtensionConfiguration(this IConfiguration config)
        {
            var all = config.GetSection("extensions").Get<PackageExtensionConfiguration[]>() ?? Array.Empty<PackageExtensionConfiguration>();

            if (all.Any(p => string.IsNullOrWhiteSpace(p.Package)))
            {
                throw new ProjectConfigurationException(Messages.ExtensionConfigPackageRequired);
            }

            return all;
        }

        /// <summary>
        /// Gets the set of configured package extensions.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns>Array of package configurations.</returns>
        public static FolderExtensionConfiguration[] GetLocalExtensionConfiguration(this IConfiguration config)
        {
            var all = config.GetSection("localExtensions").Get<FolderExtensionConfiguration[]>() ?? Array.Empty<FolderExtensionConfiguration>();

            if (all.Any(p => string.IsNullOrWhiteSpace(p.Folder)))
            {
                throw new ProjectConfigurationException(Messages.LocalExtensionsFolderRequired);
            }

            return all;
        }
    }
}
