using System;
using AutoStep.CommandLine.Output;

namespace AutoStep.CommandLine
{
    /// <summary>
    /// System console writer, used when not redirecting.
    /// </summary>
    public class SystemConsoleWriter : BaseConsoleWriter
    {
        /// <inheritdoc/>
        public override IDisposable EnterErrorBlock()
        {
            return EnterColorBlock(ConsoleColor.Red);
        }

        /// <inheritdoc/>
        public override IDisposable EnterSuccessBlock()
        {
            return EnterColorBlock(ConsoleColor.Green);
        }

        /// <inheritdoc/>
        public override IDisposable EnterWarnBlock()
        {
            return EnterColorBlock(ConsoleColor.DarkYellow);
        }

        private IDisposable EnterColorBlock(ConsoleColor color)
        {
            var originalColor = Console.ForegroundColor;

            Console.ForegroundColor = color;

            return new DisposeCallback(() => Console.ForegroundColor = originalColor);
        }
    }
}
