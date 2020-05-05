using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions;
using AutoStep.Language;
using AutoStep.Language.Interaction;
using AutoStep.Language.Test;
using AutoStep.Projects;
using AutoStep.Projects.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Base class for commands that will build a project.
    /// </summary>
    /// <typeparam name="TArgs">The command-line arguments structure.</typeparam>
    internal abstract class BuildOperationCommand<TArgs> : AutoStepCommand<TArgs>
        where TArgs : BuildOperationArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildOperationCommand{TArgs}"/> class.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="description">The command description.</param>
        public BuildOperationCommand(string name, string? description = null)
            : base(name, description)
        {
            Add(new Option(new[] { "--attach" }, "Prompts for a debugger prior to project build."));
        }

        /// <summary>
        /// Get the autostep configuration, given the supplied arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The configuration.</returns>
        protected IConfiguration GetConfiguration(BaseProjectArgs args)
        {
            var configurationBuilder = new ConfigurationBuilder();

            var configFile = args.Config;

            if (configFile is null)
            {
                configFile = new FileInfo(Path.Combine(args.Directory.FullName, "autostep.config.json"));
            }

            // Is there a config file?
            if (configFile.Exists)
            {
                // Add the JSON file.
                configurationBuilder.AddJsonFile(configFile.FullName);
            }

            // Add environment.
            configurationBuilder.AddEnvironmentVariables("AutoStep");

            // Add the provided command line options.
            configurationBuilder.AddInMemoryCollection(args.Option);

            return configurationBuilder.Build();
        }

        /// <summary>
        /// Create a new AutoStep project.
        /// </summary>
        /// <param name="args">The provided arguments.</param>
        /// <param name="projectConfig">The project configuration.</param>
        /// <param name="extensions">The loaded set of extensions.</param>
        /// <returns>A new project.</returns>
        protected Project CreateProject(BaseProjectArgs args, IConfiguration projectConfig, ExtensionsContext extensions)
        {
            // Create the project.
            Project project;

            // If diagnostic mode is enabled, then create a project with a compiler in diagnostic mode.
            if (args.Diagnostic)
            {
                project = new Project(p => ProjectCompiler.CreateWithOptions(
                    p, TestCompilerOptions.EnableDiagnostics, InteractionsCompilerOptions.EnableDiagnostics, buildExtendedMethodTableReferences: false));
            }
            else
            {
                project = new Project();
            }

            // Let our extensions extend the project.
            foreach (var ext in extensions.LoadedExtensions.ExtensionEntryPoints)
            {
                ext.AttachToProject(projectConfig, project);
            }

            // Add any files from extension content.
            // Treat the extension directory as a single file set (one for interactions, one for test).
            var extInteractionFiles = FileSet.Create(extensions.ExtensionRootDirectory, new string[] { "*/content/**/*.asi" });
            var extTestFiles = FileSet.Create(extensions.ExtensionRootDirectory, new string[] { "*/content/**/*.as" });

            project.MergeInteractionFileSet(extInteractionFiles);
            project.MergeTestFileSet(extTestFiles);

            // Define file sets for interaction and test.
            var interactionFiles = FileSet.Create(args.Directory.FullName, projectConfig.GetInteractionFileGlobs(), new string[] { ".autostep/**" });
            var testFiles = FileSet.Create(args.Directory.FullName, projectConfig.GetTestFileGlobs(), new string[] { ".autostep/**" });

            // Add the two file sets.
            project.MergeInteractionFileSet(interactionFiles);
            project.MergeTestFileSet(testFiles);

            return project;
        }

        /// <summary>
        /// Build a project and output the results of the build.
        /// </summary>
        /// <param name="project">The autostep project.</param>
        /// <param name="logFactory">The logger factory.</param>
        /// <param name="cancelToken">The cancellation token to abort the build.</param>
        /// <returns>Awaitable task with a result indicating whether the build succeeded.</returns>
        protected async Task<bool> BuildAndWriteResultsAsync(Project project, ILoggerFactory logFactory, CancellationToken cancelToken)
        {
            var logger = logFactory.CreateLogger("build");

            logger.LogInformation(Messages.CompilingProject);

            // Execution.
            var compiled = await project.Compiler.CompileAsync(logFactory, cancelToken);

            var success = true;

            // Write results.
            WriteBuildResults(logFactory, compiled);

            if (compiled.Messages.Any(m => m.Level == CompilerMessageLevel.Error))
            {
                logger.LogWarning(Messages.CompilationFailed);
                success = false;
            }
            else
            {
                logger.LogInformation(Messages.CompiledSuccessfully);
            }

            logger.LogInformation(Messages.BindingSteps);

            var linked = project.Compiler.Link(cancelToken);

            // Write link result.
            WriteBuildResults(logFactory, linked);

            if (success && linked.Messages.Any(m => m.Level == CompilerMessageLevel.Error))
            {
                logger.LogWarning(Messages.BindingFailed);
                success = false;
            }
            else
            {
                logger.LogInformation(Messages.BindingStepsSuccess);
            }

            return success;
        }

        /// <summary>
        /// Write a build result to the log.
        /// </summary>
        /// <param name="logFactory">A log factory.</param>
        /// <param name="result">The results.</param>
        protected static void WriteBuildResults(ILoggerFactory logFactory, ProjectCompilerResult result)
        {
            var logger = logFactory.CreateLogger("build");

            foreach (var message in result.Messages)
            {
                var logLevel = message.Level switch
                {
                    CompilerMessageLevel.Error => LogLevel.Error,
                    CompilerMessageLevel.Warning => LogLevel.Warning,
                    _ => LogLevel.Information
                };

                logger.Log(logLevel, message.ToString());
            }
        }

        /// <summary>
        /// Load a set of extensions.
        /// </summary>
        /// <param name="commandArgs">The command arguments.</param>
        /// <param name="logFactory">A logger factory.</param>
        /// <param name="projectConfig">The project configuration.</param>
        /// <param name="cancelToken">A cancellation token for aborting the extension load.</param>
        /// <returns>An awaitable task, resulting in a set of loaded extensions.</returns>
        protected async Task<ExtensionsContext> LoadExtensionsAsync(BaseProjectArgs commandArgs, ILoggerFactory logFactory, IConfiguration projectConfig, CancellationToken cancelToken)
        {
            var sourceSettings = new SourceSettings(commandArgs.Directory.FullName);

            var customSources = projectConfig.GetSection("extensionSources").Get<string[]>() ?? Array.Empty<string>();

            if (customSources.Length > 0)
            {
                // Add any additional configured sources.
                sourceSettings.AppendCustomSources(customSources);
            }

            var extensionsDir = Path.Combine(commandArgs.Directory.FullName, ".autostep", "extensions");
            var setLoader = new ExtensionSetLoader(commandArgs.Directory.FullName, extensionsDir, logFactory, "autostep");
            var debugExtensionBuilds = projectConfig.GetValue("debugExtensionBuilds", false);

            var resolved = await setLoader.ResolveExtensionsAsync(
                sourceSettings,
                projectConfig.GetPackageExtensionConfiguration(),
                projectConfig.GetLocalExtensionConfiguration(),
                false,
                debugExtensionBuilds,
                cancelToken);

            if (resolved.IsValid)
            {
                var installedPackages = await resolved.InstallAsync(cancelToken);

                return new ExtensionsContext(extensionsDir, installedPackages.LoadExtensionsFromPackages<IExtensionEntryPoint>(logFactory));
            }

            if (resolved.Exception is object)
            {
                throw resolved.Exception;
            }

            throw new ExtensionLoadException(Messages.ExtensionsCouldNotBeLoaded);
        }
    }
}
