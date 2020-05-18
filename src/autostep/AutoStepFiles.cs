using System;
using System.Collections.Generic;
using System.Text;
using static AutoStep.CommandLine.CreateProjectCommand;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Base class for all autostep file, I/O related operations.
    /// </summary>
    internal class AutoStepFiles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoStepFiles"/> class.
        /// </summary>
        /// <param name="createBlankProject">A function that creates a blank autostep project.</param>
        /// <param name="createWebProject">A  function that creates a web autostep project.</param>
        public AutoStepFiles(CreateProjectDelegate createBlankProject, CreateProjectDelegate createWebProject)
        {
            CreateBlankProject = createBlankProject;
            CreateWebProject = createWebProject;
        }

        /// <summary>
        /// Gets the CreateBlankProject delegate.
        /// </summary>
        internal CreateProjectDelegate CreateBlankProject { get; }

        /// <summary>
        /// Gets the CreateBlankWebproject delegate.
        /// </summary>
        internal CreateProjectDelegate CreateWebProject { get; }

        /// <summary>
        /// Gets the default instance of AutoStepFiles.
        /// </summary>
        internal static AutoStepFiles Default
        {
            get => new AutoStepFiles(
                createBlankProject: NewProjectFileIO.CreateBlankProjectFiles,
                createWebProject: NewProjectFileIO.CreateWebProjectFiles);
        }
    }
}
