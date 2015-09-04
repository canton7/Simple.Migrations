namespace SimpleMigrations
{
    /// <summary>
    /// Logger used by migrations to log things
    /// </summary>
    public interface IMigrationLogger
    {
        /// <summary>
        /// Invoked when another informative message should be logged
        /// </summary>
        /// <param name="message">Message to be logged</param>
        void Info(string message);

        /// <summary>
        /// Invoked when SQL being executed should be logged
        /// </summary>
        /// <param name="sql">SQL to log</param>
        void LogSql(string sql);
    }
}
