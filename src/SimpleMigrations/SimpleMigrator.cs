using System;
using System.Data;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// Migrator which uses <see cref="IDbConnection"/> connections
    /// </summary>
    public class SimpleMigrator : SimpleMigratorBase<IDbConnection, IMigration<IDbConnection>>
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator"/> class
        /// </summary>
        /// <param name="migrationAssembly">Assembly to search for migrations</param>
        /// <param name="connection">Connection to use to communicate with the database</param>
        /// <param name="versionProvider"><see cref="IVersionProvider{TDatabase}"/> implementation to use</param>
        /// <param name="logger">Logger to use to log progress</param>
        public SimpleMigrator(
            Assembly migrationAssembly,
            IDbConnection connection,
            IVersionProvider<IDbConnection> versionProvider,
            ILogger logger = null)
            : base(migrationAssembly, new ConnectionProvider(connection), versionProvider, logger)
        {
        }
    }

    /// <summary>
    /// Migrator which uses <see cref="IDbConnection"/> connections, but allows custom configuration of migrations
    /// </summary>
    /// <typeparam name="TMigrationBase">Type of migration base class</typeparam>
    public class SimpleMigrator<TMigrationBase> : SimpleMigratorBase<IDbConnection, TMigrationBase> where TMigrationBase : IMigration<IDbConnection>
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator{TMigrationBase}"/> class
        /// </summary>
        /// <param name="migrationAssembly">Assembly to search for migrations</param>
        /// <param name="connection">Connection to use to communicate with the database</param>
        /// <param name="versionProvider"><see cref="IVersionProvider{TDatabase}"/> implementation to use</param>
        /// <param name="configurator">Action which is invoked on every migration</param>
        /// <param name="logger">Logger to use to log progress</param>
        public SimpleMigrator(
            Assembly migrationAssembly,
            IDbConnection connection,
            IVersionProvider<IDbConnection> versionProvider,
            Action<TMigrationBase> configurator,
            ILogger logger = null)
            : base(migrationAssembly, new ConnectionProvider(connection), versionProvider, logger)
        {
            this.Configurator = configurator;
        }
    }
}
