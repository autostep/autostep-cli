using System;
using AutoStep.CommandLine.Output;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// Base class for all console writers. Provides non-colouring functionality.
    /// </summary>
    public abstract class BaseConsoleWriter : IConsoleWriter
    {
        /// <inheritdoc/>
        public abstract IDisposable EnterErrorBlock();

        /// <inheritdoc/>
        public abstract IDisposable EnterSuccessBlock();

        /// <inheritdoc/>
        public abstract IDisposable EnterWarnBlock();

        /// <inheritdoc/>
        public virtual void Write(string message)
        {
            Console.Out.Write(message);
        }

        /// <inheritdoc/>
        public virtual void WriteLine(string message)
        {
            Console.Out.WriteLine(message);
        }

        /// <inheritdoc/>
        public virtual void WriteLine()
        {
            Console.Out.WriteLine();
        }
    }
}
