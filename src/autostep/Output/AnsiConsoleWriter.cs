using System;
using System.CommandLine.Rendering;
using AutoStep.CommandLine.Output;

namespace AutoStep.CommandLine
{
    using static Ansi.Color;

    /// <summary>
    /// Implements colour changes using ANSI control sequences.
    /// </summary>
    internal class AnsiConsoleWriter : BaseConsoleWriter
    {
        /// <inheritdoc/>
        public override IDisposable EnterErrorBlock()
        {
            return EnterColorBlock(Foreground.Red);
        }

        /// <inheritdoc/>
        public override IDisposable EnterSuccessBlock()
        {
            return EnterColorBlock(Foreground.Green);
        }

        /// <inheritdoc/>
        public override IDisposable EnterWarnBlock()
        {
            return EnterColorBlock(Foreground.Yellow);
        }

        private IDisposable EnterColorBlock(AnsiControlCode colorCode)
        {
            Console.Write(colorCode.EscapeSequence);

            return new DisposeCallback(() => Console.Write(Foreground.Default.EscapeSequence));
        }
    }
}
