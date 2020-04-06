using AutoStep.Extensions;
using AutoStep.Language;
using AutoStep.Language.Interaction;
using AutoStep.Language.Test;
using AutoStep.Projects;
using AutoStep.Projects.Configuration;
using AutoStep.Projects.Files;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.CommandLine
{
    public abstract class BuildOperationCommand<TArgs> : AutoStepCommand<TArgs>
        where TArgs : BuildOperationArgs
    {
        public BuildOperationCommand(string name, string description = null) : base(name, description)
        {
        }

        protected async Task<ProjectConfiguration> GetConfiguration(BaseProjectArgs args, CancellationToken cancelToken)
        {
            ProjectConfiguration config = ProjectConfiguration.Default;

            var configFile = args.Config;

            if(configFile is null)
            {
                configFile = new FileInfo(Path.Combine(args.Directory.FullName, "autostep.config.json"));
            }

            // Is there a config file?
            if (configFile.Exists)
            {
                // Load the configuration file.
                // Deserialize from JSON.
                config = await LoadConfiguration(configFile, cancelToken);
            }

            return config;
        }

        protected Project CreateProject(BaseProjectArgs args, ProjectConfiguration projectConfig, LoadedExtensionSet extensions)
        {
            // Create the project.
            Project project;

            if (args.Diagnostic)
            {
                project = new Project(p => ProjectCompiler.CreateWithOptions(p, TestCompilerOptions.EnableDiagnostics, InteractionsCompilerOptions.EnableDiagnostics));
            }
            else
            {
                project = new Project();
            }

            // Let our extensions extend the project.
            extensions.AttachToProject(project);

            // Add any files from extension content.
            // Treat the extension directory as a single file set (one for interactions, one for test).
            var extInteractionFiles = FileSet.Create(extensions.ExtensionsRootDir, new string[] { "*/content/**/*.asi" });
            var extTestFiles = FileSet.Create(extensions.ExtensionsRootDir, new string[] { "*/content/**/*.as" });

            project.MergeInteractionFileSet(extInteractionFiles);
            project.MergeTestFileSet(extTestFiles);

            // Define file sets for interaction and test.
            var interactionFiles = FileSet.Create(args.Directory.FullName, projectConfig.Interactions, new string[] { ".autostep/**" });
            var testFiles = FileSet.Create(args.Directory.FullName, projectConfig.Tests, new string[] { ".autostep/**" });

            // Add the two file sets.
            project.MergeInteractionFileSet(interactionFiles);
            project.MergeTestFileSet(testFiles);

            return project;
        }

        protected async Task<bool> BuildAndWriteResultsAsync(BuildOperationArgs args, Project project, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            // Execution.
            var compiled = await CompileAsync(args, project, logFactory, cancelToken);

            var success = true;

            // Write results.
            WriteBuildResults(logFactory, compiled);

            if (compiled.Messages.Any(m => m.Level == CompilerMessageLevel.Error))
            {
                success = false;
            }

            var linked = Link(args, project, logFactory, cancelToken);

            if (success && linked.Messages.Any(m => m.Level == CompilerMessageLevel.Error))
            {
                success = false;
            }

            // Write link result.
            WriteBuildResults(logFactory, linked);

            return success;
        }

        protected void WriteBuildResults(ILoggerFactory logFactory, ProjectCompilerResult result)
        {
            var logger = logFactory.CreateLogger<Program>();

            foreach (var message in result.Messages)
            {
                var logLevel = message.Level switch
                {
                    CompilerMessageLevel.Error => LogLevel.Error,
                    _ => LogLevel.Information
                };

                logger.Log(logLevel, message.ToString());
            }
        }

        protected async Task<LoadedExtensionSet> LoadExtensionsAsync(BaseProjectArgs projectArgs, ILoggerFactory logFactory, ProjectConfiguration projectConfig, CancellationToken cancelToken)
        {
            var extensionsFolder = Path.GetFullPath(Path.Combine(".autostep", "extensions"), projectArgs.Directory.FullName);

            var depFilePath = Path.Combine(extensionsFolder, "extensions.deps.json");

            var cachedLoader = new CachedExtensionLoader(projectArgs, extensionsFolder, logFactory);

            var resolved = await cachedLoader.ResolveExtensionPackagesAsync(projectConfig, depFilePath, cancelToken);

            if (resolved is null)
            {
                using var nugetLoader = new NugetExtensionLoader(projectArgs, extensionsFolder, logFactory);

                resolved = await nugetLoader.ResolveExtensionPackagesAsync(projectConfig, depFilePath, cancelToken);
            }

            var loadedSet = new LoadedExtensionSet(projectConfig, resolved);

            loadedSet.Load(logFactory);

            return loadedSet;
        }

        protected async ValueTask<ProjectCompilerResult> CompileAsync(BuildOperationArgs args, Project project, ILoggerFactory logFactory, CancellationToken cancelToken)
        {            
            // Now, compile.
            return await project.Compiler.CompileAsync(logFactory, cancelToken);
        }

        protected ProjectCompilerResult Link(BuildOperationArgs args, Project project, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            return project.Compiler.Link(cancelToken);
        }

        protected ValueTask<ProjectConfiguration> LoadConfiguration(FileInfo file, CancellationToken cancelToken)
        {
            return ProjectConfiguration.Load(file.FullName, cancelToken);
        }
    }
}
