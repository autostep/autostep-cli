using System;
using System.Globalization;
using AutoStep.CommandLine.Output;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Extension methods to help with writing to a console writer.
    /// </summary>
    internal static class ConsoleExtensions
    {
        /// <summary>
        /// Write a line of text to the console with a leading indent.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="message">The message content.</param>
        /// <param name="indent">The indent size.</param>
        public static void WriteLine(this IConsoleWriter console, string message, int indent)
        {
            console.WriteIndent(indent);
            console.WriteLine(message);
        }

        /// <summary>
        /// Writes an indentation block to the console.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="indent">The indent size.</param>
        public static void WriteIndent(this IConsoleWriter console, int indent)
        {
            console.Write(new string(' ', indent));
        }

        /// <summary>
        /// Write formatted text to the console.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="message">The formattable message.</param>
        /// <param name="args">Format arguments.</param>
        public static void WriteFormat(this IConsoleWriter console, string message, params object[] args)
        {
            console.Write(string.Format(CultureInfo.CurrentCulture, message, args));
        }

        /// <summary>
        /// Write a formatted line of text to the console.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="message">The formattable message.</param>
        /// <param name="args">Format arguments.</param>
        public static void WriteFormatLine(this IConsoleWriter console, string message, params object[] args)
        {
            console.WriteLine(string.Format(CultureInfo.CurrentCulture, message, args));
        }

        /// <summary>
        /// Write text to the console, with success colouring.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="message">The message.</param>
        public static void WriteSuccess(this IConsoleWriter console, string message)
        {
            using (console.EnterSuccessBlock())
            {
                console.Write(message);
            }
        }

        /// <summary>
        /// Write a formatted line of text to the console using error colouring.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="message">The formattable message.</param>
        /// <param name="indent">The indent size.</param>
        /// <param name="args">Format arguments.</param>
        public static void WriteFormattedErrorLine(this IConsoleWriter console, string message, int indent, params object[] args)
        {
            using (console.EnterErrorBlock())
            {
                console.WriteIndent(indent);
                console.WriteFormatLine(message, args);
            }
        }

        /// <summary>
        /// Write a line of text to the console with a leading indent, using error colouring.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="message">The message content.</param>
        /// <param name="indent">The indent size.</param>
        public static void WriteErrorLine(this IConsoleWriter console, string message, int indent)
        {
            using (console.EnterErrorBlock())
            {
                console.WriteLine(message, indent);
            }
        }

        /// <summary>
        /// Write text to the console using error colouring.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="message">The message content.</param>
        public static void WriteError(this IConsoleWriter console, string message)
        {
            using (console.EnterErrorBlock())
            {
                console.Write(message);
            }
        }

        /// <summary>
        /// Write text to the console with a leading indent, using success colouring.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="message">The message content.</param>
        /// <param name="indent">The indent size.</param>
        public static void WriteSuccessLine(this IConsoleWriter console, string message, int indent)
        {
            using (console.EnterSuccessBlock())
            {
                console.WriteLine(message, indent);
            }
        }

        /// <summary>
        /// Write an underlined heading to the console.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="heading">The heading text.</param>
        /// <param name="underlineCharacter">A character to use to underline the heading.</param>
        public static void WriteHeading(this IConsoleWriter console, string heading, char underlineCharacter)
        {
            var underline = new string(underlineCharacter, heading.Length);

            console.WriteLine(heading);
            console.WriteLine(underline);
            console.WriteLine();
        }

        /// <summary>
        /// Write an indented multi-line block of text to the console.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="block">The block of text to write.</param>
        /// <param name="indentation">The indentation for the entire block.</param>
        public static void WriteIndentedBlock(this IConsoleWriter console, string block, int indentation)
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
                            console.WriteLine();
                        }

                        while (queuedBlankLines > 0)
                        {
                            console.WriteLine();

                            queuedBlankLines--;
                        }

                        // Got the content of the line. Append it up until now.
                        console.Write(indent);
                        console.Write(text.Slice(0, currentPos).ToString());

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
            console.WriteLine();
        }
    }
}
