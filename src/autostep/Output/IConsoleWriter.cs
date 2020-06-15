using System;

namespace AutoStep.CommandLine.Output
{
    /// <summary>
    /// Defines an interface for a general console writer, that directs colour instructions as needed.
    /// </summary>
    public interface IConsoleWriter
    {
        /// <summary>
        /// Write text to the console.
        /// </summary>
        /// <param name="message">The message text.</param>
        void Write(string message);

        /// <summary>
        /// Write a line of text to the console.
        /// </summary>
        /// <param name="message">The message text.</param>
        void WriteLine(string message);

        /// <summary>
        /// Write a blank line to the console.
        /// </summary>
        void WriteLine();

        /// <summary>
        /// Start a success colour block.
        /// </summary>
        /// <returns>A disposable that exits the block when disposed.</returns>
        IDisposable EnterSuccessBlock();

        /// <summary>
        /// Start a warn colour block.
        /// </summary>
        /// <returns>A disposable that exits the block when disposed.</returns>
        IDisposable EnterWarnBlock();

        /// <summary>
        /// Start an error colour block.
        /// </summary>
        /// <returns>A disposable that exits the block when disposed.</returns>
        IDisposable EnterErrorBlock();
    }
}
