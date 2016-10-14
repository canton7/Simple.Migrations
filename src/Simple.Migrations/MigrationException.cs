using System;

namespace SimpleMigrations
{
    /// <summary>
    /// An exception relating to migrations occurred
    /// </summary>
    public class MigrationException : Exception
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="MigrationException"/> class
        /// </summary>
        public MigrationException()
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="MigrationException"/> class
        /// </summary>
        /// <param name="message">Message to use</param>
        public MigrationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="MigrationException"/> class
        /// </summary>
        /// <param name="message">Message to use</param>
        /// <param name="innerException">Inner exception to use</param>
        public MigrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}