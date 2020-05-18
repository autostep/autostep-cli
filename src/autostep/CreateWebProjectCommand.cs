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
    /// Base class for creating autostep web projects.
    /// </summary>
    internal class CreateWebProjectCommand : CreateProjectCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWebProjectCommand"/> class.
        /// </summary>
        /// <param name="autoStepFiles">Instance of <see cref="AutoStepFiles"/> that creates the necessary projects files for web project.</param>
        public CreateWebProjectCommand(AutoStepFiles autoStepFiles)
            : base("web", "creates a new web project with an example interactions file(.asi), tests file(.as) file and autostep.config.json with AutoStep.Web extension.", autoStepFiles.CreateWebProject)
        {
        }
    }
}
