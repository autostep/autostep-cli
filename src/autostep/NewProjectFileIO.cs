using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Utility class that handles all File IO operations related to creating a autostep project.
    /// </summary>
    internal static class NewProjectFileIO
    {
        /// <summary>
        /// Handles creation of autostep web project.
        /// </summary>
        /// <param name="args">args.</param>
        /// <param name="logger">logger instance.</param>
        /// <returns>0/1 indicating success/failure.</returns>
        public static int CreateWebProjectFiles(RunArgs args, ILogger logger)
        {
            var dirInfo = args.Directory;

            if (!Directory.Exists(dirInfo.FullName))
            {
                Directory.CreateDirectory(dirInfo.FullName);
            }

            void CreateAutoStepInteractionsFile()
            {
                var fpath = Path.Combine(dirInfo.FullName, dirInfo.Name + ".asi");

                // create autostep interactions file
                string asInteractionsFileContent = $"# autostep interactions file";

                CreateFileWithContent(fpath, asInteractionsFileContent);
            }

            void CreateAutoStepTestFile()
            {
                var fpath = Path.Combine(dirInfo.FullName, dirInfo.Name + ".as");

                // create autostep test file
                string asTestFileContent = $"Feature: <Feature title> {Environment.NewLine}   Scenario: Clicked on X shows Y";

                CreateFileWithContent(fpath, asTestFileContent);
            }

            void CreateAutoStepConfiguration()
            {
                var fpath = Path.Combine(dirInfo.FullName, "autostep.config.json");

                // create config file
                const string WebAutoStepConfigContent = @"{
    ""extensions"": [
      { ""package"": ""AutoStep.Web"",  ""prerelease"": true }
    ],
    ""extensionSources"": [
      ""https://f.feedz.io/autostep/ci/nuget/index.json""
    ]
} ";

                CreateFileWithContent(fpath, WebAutoStepConfigContent);
            }

            CreateAutoStepInteractionsFile();
            CreateAutoStepTestFile();
            CreateAutoStepConfiguration();

            logger.LogInformation(Messages.BlankWebProjectCreated);

            return 0;
        }

        /// <summary>
        /// Handles creation of blank autostep project.
        /// </summary>
        /// <param name="args">args.</param>
        /// <param name="logger">logger instance.</param>
        /// <returns>0/1 indicating success/failure.</returns>
        public static int CreateBlankProjectFiles(RunArgs args, ILogger logger)
        {
            var dirInfo = args.Directory;

            if (!Directory.Exists(dirInfo.FullName))
            {
                Directory.CreateDirectory(dirInfo.FullName);
            }

            void CreateAutoStepTestFile()
            {
                var fpath = Path.Combine(dirInfo.FullName, dirInfo.Name + ".as");

                // create autostep test file
                string asTestFileContent = $"Feature: <Feature title> {Environment.NewLine}   Scenario: Clicked on X shows Y";

                CreateFileWithContent(fpath, asTestFileContent);
            }

            void CreateAutoStepConfiguration()
            {
                var fpath = Path.Combine(dirInfo.FullName, "autostep.config.json");

                // create config file
                const string BlankAutoStepConfigContent = @"{
    ""extensions"": [],
    ""extensionSources"": []
} ";

                CreateFileWithContent(fpath, BlankAutoStepConfigContent);
            }

            CreateAutoStepTestFile();
            CreateAutoStepConfiguration();

            logger.LogInformation(Messages.BlankProjectCreated);

            return 0;
        }

        private static void CreateFileWithContent(string filePath, string content)
        {
            using (var writer = File.CreateText(filePath))
            {
                writer.Write(content);
            }
        }
    }
}
