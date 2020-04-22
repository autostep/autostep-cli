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
        public CreateWebProjectCommand()
            : base("web", "creates a new web project with an example interactions file(.asi), tests file(.as) file and autostep.config.json with AutoStep.Web extension.", NewProjectFileIO.CreateWebProjectFiles)
        {
        }
    }
}
