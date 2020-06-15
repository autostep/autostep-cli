using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStep.CommandLine.Output
{
    /// <summary>
    /// Console writer that does no colorisation.
    /// </summary>
    internal class PlainTextWriter : BaseConsoleWriter
    {
        /// <inheritdoc/>
        public override IDisposable EnterErrorBlock()
        {
            return new DisposeCallback(() => { });
        }

        /// <inheritdoc/>
        public override IDisposable EnterSuccessBlock()
        {
            return new DisposeCallback(() => { });
        }

        /// <inheritdoc/>
        public override IDisposable EnterWarnBlock()
        {
            return new DisposeCallback(() => { });
        }
    }
}
