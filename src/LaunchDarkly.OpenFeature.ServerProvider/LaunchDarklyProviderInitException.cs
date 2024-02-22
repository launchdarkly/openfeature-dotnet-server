using System;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// This exception is used to indicate that the provider has encountered a permanent exception, or has been
    /// shutdown, during initialization.
    /// </summary>
    public class LaunchDarklyProviderInitException: Exception
    {
        /// <summary>
        /// Construct an exception with the given message.
        /// </summary>
        /// <param name="message">The exception message</param>
        public LaunchDarklyProviderInitException(string message)
            : base(message)
        {
        }
    }
}
