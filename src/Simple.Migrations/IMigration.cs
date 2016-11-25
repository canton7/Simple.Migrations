namespace SimpleMigrations
{
    /// <summary>
    /// Interface which must be implemented by all migrations, although you probably want to derive from <see cref="Migration"/> instead
    /// </summary>
    /// <typeparam name="TConnection">Type of database connection which this migration will use</typeparam>
    public interface IMigration<TConnection>
    {
        /// <summary>
        /// Execute the migration in the given direction, using the given connection and logger
        /// </summary>
        /// <remarks>
        /// The migration should create a transaction if appropriate
        /// </remarks>
        /// <param name="connection">Connection to use to run the migration</param>
        /// <param name="logger">Logger to use to log SQL statements run, and other messages</param>
        /// <param name="direction">Direction to run the migration in</param>
        void Execute(TConnection connection, IMigrationLogger logger, MigrationDirection direction);
    }
}
