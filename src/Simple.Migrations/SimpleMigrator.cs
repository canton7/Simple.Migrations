using System.Data;
using System.Data.Common;
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
        /// <param name="databaseProvider"><see cref="IDatabaseProvider{TDatabase}"/> implementation to use</param>
        /// <param name="logger">Logger to use to log progress</param>
        public SimpleMigrator(
            IMigrationProvider migrationProvider,
            IDatabaseProvider<IDbConnection> databaseProvider,
            ILogger logger = null)
            : base(migrationProvider, databaseProvider, logger)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator"/> class
        /// </summary>
        /// <param name="migrationsAssembly">Assembly to search for migrations</param>
        /// <param name="databaseProvider"><see cref="IDatabaseProvider{TDatabase}"/> implementation to use</param>
        /// <param name="logger">Logger to use to log progress</param>
        public SimpleMigrator(
            Assembly migrationsAssembly,
            IDatabaseProvider<IDbConnection> databaseProvider,
            ILogger logger = null)
            : base(migrationsAssembly, databaseProvider, logger)
        {
        }
    }
}
