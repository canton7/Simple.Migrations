using System.Data;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// Migrator which uses <see cref="IDbConnection"/> connections
    /// </summary>
    public class SimpleMigrator : SimpleMigrator<IDbConnection, IMigration<IDbConnection>>
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator"/> class
        /// </summary>
        /// <param name="migrationProvider">Migration provider to use to find migration classes</param>
        /// <param name="connection">Connection to use to communicate with the database</param>
        /// <param name="versionProvider"><see cref="IVersionProvider{TDatabase}"/> implementation to use</param>
        /// <param name="logger">Logger to use to log progress</param>
        public SimpleMigrator(
            IMigrationProvider migrationProvider,
            IDbConnection connection,
            IVersionProvider<IDbConnection> versionProvider,
            ILogger logger = null)
            : base(migrationProvider, connection, versionProvider, logger)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator"/> class
        /// </summary>
        /// <param name="migrationsAssembly">Assembly to search for migrations</param>
        /// <param name="connection">Connection to use to communicate with the database</param>
        /// <param name="versionProvider"><see cref="IVersionProvider{TDatabase}"/> implementation to use</param>
        /// <param name="logger">Logger to use to log progress</param>
        public SimpleMigrator(
            Assembly migrationsAssembly,
            IDbConnection connection,
            IVersionProvider<IDbConnection> versionProvider,
            ILogger logger = null)
            : base(migrationsAssembly, connection, versionProvider, logger)
        {
        }

        /// <summary>
        /// Load all available migrations, and the current state of the database
        /// </summary>
        /// <remarks>
        /// This opens the database connection if necessary
        /// </remarks>
        public override void Load()
        {
            if (this.Connection.State == ConnectionState.Closed)
                this.Connection.Open();

            base.Load();
        }
    }
}
