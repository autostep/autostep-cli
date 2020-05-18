using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Custom contracts implemented by autostep commandline commands.
    /// </summary>
    internal interface IAutostepCommand
    {
        /// <summary>
        /// Checks if the command is valid.
        /// </summary>
        /// <returns>Returns true/false depending on the arguments passed.</returns>
        bool CommandIsValid();
    }
}
