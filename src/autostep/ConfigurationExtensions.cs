using System.Linq;
using AutoStep.Extensions;
using Microsoft.Extensions.Configuration;

namespace AutoStep.CommandLine
{
    public static class ConfigurationExtensions
    {
        public static string[] GetTestFileGlobs(this IConfiguration config)
        {
            return config.GetValue("tests", new[] { "**/*.as" });
        }

        public static string[] GetInteractionFileGlobs(this IConfiguration config)
        {
            return config.GetValue("interactions", new[] { "**/*.asi" });
        }

        public static ExtensionConfiguration[] GetExtensionConfiguration(this IConfiguration config)
        {
            var all = config.GetSection("extensions").Get<ExtensionConfiguration[]>();

            if (all.Any(p => string.IsNullOrWhiteSpace(p.Package)))
            {
                throw new ProjectConfigurationException("Extensions must have a specified Package Id.");
            }

            return all;
        }
    }
}
