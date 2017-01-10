namespace SimpleMigrations
{
    /// <summary>
    /// Thrown if there was a problem loading the migrations
    /// </summary>
    public class MigrationLoadFailedException : MigrationException
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="MigrationLoadFailedException"/> class
        /// </summary>
        /// <param name="message">Message to use</param>
        public MigrationLoadFailedException(string message)
            : base(message)
        {
        }
    }
}
