using System;
using System.Diagnostics;
using System.Linq;

namespace AutoStep.CommandLine.Output
{
    /// <summary>
    /// Detects the desired console output mode, and generates an <see cref="IConsoleWriter"/>.
    /// </summary>
    public static class ConsoleDetector
    {
        private static readonly string[] ContinousIntegrationEnvironmentVariables = new[]
        {
            "CI",
            "TF_BUILD",
            "DOCKERFILE_PATH",
            "JENKINS_URL",
            "bamboo.buildKey",
            "TEAMCITY_VERSION",
        };

        /// <summary>
        /// Get an instance of <see cref="IConsoleWriter"/>, choosing the appropriate implementation.
        /// </summary>
        /// <returns>A console writer.</returns>
        public static IConsoleWriter GetConsoleWriter()
        {
            if (Console.IsOutputRedirected)
            {
                // Ok, so now we need to detect whether we should issue ANSI sequences.
                if (ColorEnableEnvironmentVariablePresent())
                {
                    return new AnsiConsoleWriter();
                }
                else
                {
                    return new PlainTextWriter();
                }
            }

            // Not redirected, just use the console.
            return new SystemConsoleWriter();
        }

        private static bool ColorEnableEnvironmentVariablePresent()
        {
            return ContinousIntegrationEnvironmentVariables.Any(env => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(env)));
        }
    }
}
