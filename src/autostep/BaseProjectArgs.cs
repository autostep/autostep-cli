using System.Collections.Generic;
using System.IO;

namespace AutoStep.CommandLine
{
    public class BaseProjectArgs : BaseGlobalArgs
    {
        public DirectoryInfo Directory { get; set; }

        public FileInfo Config { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Option { get; set; }
    }
}
