using System.Globalization;

namespace AutoStep.CommandLine.Results
{
    /// <summary>
    /// Helper methods for formatting messages.
    /// </summary>
    public static class FormattingExtensions
    {
        /// <summary>
        /// Format with the current culture.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Format arguments.</param>
        /// <returns>Formatted string.</returns>
        public static string FormatWith(this string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }
    }
}
