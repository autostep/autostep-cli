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
    }
}
