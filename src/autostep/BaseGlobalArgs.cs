using Microsoft.Extensions.Logging;

namespace AutoStep.CommandLine
{
    public class BaseGlobalArgs
    {
        public bool Verbose { get; set; }

        public bool Diagnostic { get; set; }
    }
}
