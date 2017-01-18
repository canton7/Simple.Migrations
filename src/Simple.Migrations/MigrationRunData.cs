using System;

namespace SimpleMigrations
{
    /// <summary>
    /// Data passed to <see cref="IMigration{TConnection}.RunMigration"/>
    /// </summary>
    /// <remarks>
    /// This is split out into a separate class for backwards compatibility
    /// </remarks>
    /// <typeparam name="TConnection">Type of database connection which this migration will use</typeparam>
    public class MigrationRunData<TConnection>
    {
        /// <summary>
        /// Connection to use to run the migration
        /// </summary>
        public TConnection Connection { get; }

        /// <summary>
        /// Logger to use to log SQL statements run, and other messages
        /// </summary>
        public IMigrationLogger Logger { get; }

        /// <summary>
        /// Direction to run the migration in
        /// </summary>
        public MigrationDirection Direction { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="MigrationRunData{TConnection}"/> class
        /// </summary>
        /// <param name="connection">Connection to use to run the migration</param>
        /// <param name="logger">Logger to use to log SQL statements run, and other messages</param>
        /// <param name="direction">Direction to run the migration in</param>
        public MigrationRunData(TConnection connection, IMigrationLogger logger, MigrationDirection direction)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.Connection = connection;
            this.Logger = logger;
            this.Direction = direction;
        }
    }
}
