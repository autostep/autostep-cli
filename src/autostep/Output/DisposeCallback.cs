using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStep.CommandLine.Output
{
    /// <summary>
    /// Simple object to call a callback when it is disposed.
    /// </summary>
    internal sealed class DisposeCallback : IDisposable
    {
        private readonly Action callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposeCallback"/> class.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        public DisposeCallback(Action callback)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Dispose the object, invoking the callback.
        /// </summary>
        public void Dispose()
        {
            callback();
        }
    }
}
