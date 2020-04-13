using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStep.CommandLine.Results
{
    internal interface IConsoleResultsWriter
    {
        void WriteInfo(string info);

        void WriteFailure(string message);
    }
}
