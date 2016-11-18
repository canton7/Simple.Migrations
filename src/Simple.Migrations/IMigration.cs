namespace SimpleMigrations
{
    /// <summary>
    /// Interface which must be implemented by all migrations, although you probably want to derive from <see cref="Migration"/> instead
    /// </summary>
    /// <typeparam name="TConnection">Type of database connection which this migration will use</typeparam>
    public interface IMigration<TConnection>
    {
        void Execute(TConnection connection, IMigrationLogger logger, MigrationDirection direction);
    }
}
