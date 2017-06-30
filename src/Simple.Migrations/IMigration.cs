namespace SimpleMigrations
{
    /// <summary>
    /// Interface which must be implemented by all migrations, although you probably want to derive from <see cref="Migration"/> instead
    /// </summary>
    /// <typeparam name="TConnection">Type of database connection which this migration will use</typeparam>
    public interface IMigration<TConnection>
    {
        /// <summary>
        /// Run the migration in the given direction, using the given connection and logger
        /// </summary>
        /// <remarks>
        /// The migration should create a transaction if appropriate
        /// </remarks>
        /// <param name="data">Data used by the migration</param>
        void RunMigration(MigrationRunData<TConnection> data);
    }
}
