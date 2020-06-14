using System;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.Globalization;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Extension methods to help with writing to a terminal.
    /// </summary>
    internal static class TerminalExtensions
    {
        /// <summary>
        /// Write text to the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The message content.</param>
        public static void Write(this ITerminal terminal, string message)
        {
            terminal.Out.Write(message);
        }

        /// <summary>
        /// Write a line of text to the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The message content.</param>
        public static void WriteLine(this ITerminal terminal, string message)
        {
            terminal.Out.WriteLine(message);
        }

        /// <summary>
        /// Write a line of text to the terminal with a leading indent.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The message content.</param>
        /// <param name="indent">The indent size.</param>
        public static void WriteLine(this ITerminal terminal, string message, int indent)
        {
            terminal.WriteIndent(indent);
            terminal.Out.WriteLine(message);
        }

        /// <summary>
        /// Writes an indentation block to the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="indent">The indent size.</param>
        public static void WriteIndent(this ITerminal terminal, int indent)
        {
            terminal.Write(new string(' ', indent));
        }

        /// <summary>
        /// Write formatted text to the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The formattable message.</param>
        /// <param name="args">Format arguments.</param>
        public static void WriteFormat(this ITerminal terminal, string message, params object[] args)
        {
            terminal.Write(string.Format(CultureInfo.CurrentCulture, message, args));
        }

        /// <summary>
        /// Write a formatted line of text to the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The formattable message.</param>
        /// <param name="args">Format arguments.</param>
        public static void WriteFormatLine(this ITerminal terminal, string message, params object[] args)
        {
            terminal.WriteLine(string.Format(CultureInfo.CurrentCulture, message, args));
        }

        /// <summary>
        /// Write text to the terminal, with success colouring.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The message.</param>
        public static void WriteSuccess(this ITerminal terminal, string message)
        {
            terminal.ForegroundColor = ConsoleColor.Green;
            terminal.Write(message);
            terminal.ResetColor();
        }

        /// <summary>
        /// Write a formatted line of text to the terminal using error colouring.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The formattable message.</param>
        /// <param name="indent">The indent size.</param>
        /// <param name="args">Format arguments.</param>
        public static void WriteFormattedErrorLine(this ITerminal terminal, string message, int indent, params object[] args)
        {
            terminal.ForegroundColor = ConsoleColor.Red;
            terminal.WriteIndent(indent);
            terminal.WriteFormatLine(message, args);
            terminal.ResetColor();
        }

        /// <summary>
        /// Write a line of text to the terminal with a leading indent, using error colouring.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The message content.</param>
        /// <param name="indent">The indent size.</param>
        public static void WriteErrorLine(this ITerminal terminal, string message, int indent)
        {
            terminal.ForegroundColor = ConsoleColor.Red;
            terminal.WriteLine(message, indent);
            terminal.ResetColor();
        }

        /// <summary>
        /// Write text to the terminal using error colouring.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The message content.</param>
        public static void WriteError(this ITerminal terminal, string message)
        {
            terminal.ForegroundColor = ConsoleColor.Red;
            terminal.Write(message);
            terminal.ResetColor();
        }

        /// <summary>
        /// Write text to the terminal with a leading indent, using success colouring.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="message">The message content.</param>
        /// <param name="indent">The indent size.</param>
        public static void WriteSuccessLine(this ITerminal terminal, string message, int indent)
        {
            terminal.ForegroundColor = ConsoleColor.Green;
            terminal.WriteLine(message, indent);
            terminal.ResetColor();
        }

        /// <summary>
        /// Write a blank line to the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        public static void WriteLine(this ITerminal terminal)
        {
            terminal.Out.WriteLine();
        }

        /// <summary>
        /// Write an underlined heading to the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="heading">The heading text.</param>
        /// <param name="underlineCharacter">A character to use to underline the heading.</param>
        public static void WriteHeading(this ITerminal terminal, string heading, char underlineCharacter)
        {
            var underline = new string(underlineCharacter, heading.Length);

            terminal.WriteLine(heading);
            terminal.WriteLine(underline);
            terminal.WriteLine();
        }

        /// <summary>
        /// Write an indented multi-line block of text to the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <param name="block">The block of text to write.</param>
        /// <param name="indentation">The indentation for the entire block.</param>
        public static void WriteIndentedBlock(this ITerminal terminal, string block, int indentation)
        {
            var text = block.AsSpan();

            // First of all, find the first non-whitespace character.
            var currentPos = 0;
            var knownLineStart = 0;
            var determinedSpacing = false;
            var queuedBlankLines = 0;
            var hitText = false;
            bool writtenSomething = false;
            var indent = new string(' ', indentation);

            ReadOnlySpan<char> TerminateLine(ReadOnlySpan<char> text)
            {
                if (determinedSpacing)
                {
                    if (currentPos > text.Length)
                    {
                        return text;
                    }

                    var contentToAppend = text.Slice(0, currentPos);

                    if (contentToAppend.Length == 0 || contentToAppend.IsWhiteSpace())
                    {
                        queuedBlankLines++;
                    }
                    else
                    {
                        if (writtenSomething)
                        {
                            terminal.Out.WriteLine();
                        }

                        while (queuedBlankLines > 0)
                        {
                            terminal.Out.WriteLine();

                            queuedBlankLines--;
                        }

                        // Got the content of the line. Append it up until now.
                        terminal.Out.Write(indent);
                        terminal.Out.Write(text.Slice(0, currentPos).ToString());

                        writtenSomething = true;
                    }

                    text = text.Slice(currentPos);
                    hitText = false;
                }

                return text;
            }

            // Get the whitespace characters.
            while (currentPos < text.Length)
            {
                var currentChar = text[currentPos];

                if (currentChar == '\r' || currentChar == '\n')
                {
                    text = TerminateLine(text);

                    if (text[0] == '\r')
                    {
                        // Move on two characters.
                        text = text.Slice(2);
                    }
                    else
                    {
                        text = text.Slice(1);
                    }

                    currentPos = 0;
                }
                else if (!hitText && (!char.IsWhiteSpace(currentChar) || (determinedSpacing && currentPos == knownLineStart)))
                {
                    hitText = true;

                    if (!determinedSpacing)
                    {
                        knownLineStart = currentPos;
                        determinedSpacing = true;
                    }

                    text = text.Slice(currentPos);
                    currentPos = 0;
                }
                else
                {
                    currentPos++;
                }
            }

            TerminateLine(text);
            terminal.Out.WriteLine();
        }
    }
}
