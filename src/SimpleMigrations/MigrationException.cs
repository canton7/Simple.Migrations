using System;
using System.Runtime.Serialization;

namespace SimpleMigrations
{
    /// <summary>
    /// An exception relating to migrations occurred
    /// </summary>
    [Serializable]
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
        public MigrationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="MigrationException"/> class
        /// </summary>
        /// <param name="message">Message to use</param>
        /// <param name="innerException">Inner exception to use</param>
        public MigrationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="MigrationException"/> class
        /// </summary>
        /// <param name="info">SerializationInfo to use</param>
        /// <param name="context">StreamingContext to use</param>
        protected MigrationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}