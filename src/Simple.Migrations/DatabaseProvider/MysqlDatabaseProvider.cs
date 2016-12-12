using System;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an MySQL database
    /// </summary>
    /// <remarks>
    /// MySQL supports advisory locks, so these are used to guard against concurrent migrators.
    /// </remarks>
    public class MysqlDatabaseProvider : DatabaseProviderBaseWithAdvisoryLock
    {
        /// <summary>
        /// Gets or sets the name of the advisory lock to acquire
        /// </summary>
        public string LockName { get; set; } = "SimpleMigratorExclusiveLock";

        /// <summary>
        /// Initialises a new instance of the <see cref="MysqlDatabaseProvider"/> class
        /// </summary>
        /// <param name="connection">Connection to use to run migrations. The caller is responsible for closing this.</param>
        public MysqlDatabaseProvider(DbConnection connection)
            : base(connection)
        {
        }

        /// <summary>
        /// Acquires an advisory lock using Connection
        /// </summary>
        public override void AcquireAdvisoryLock()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"SELECT GET_LOCK('{this.LockName}', {(int)this.LockTimeout.TotalSeconds})";
                command.CommandTimeout = 0; // The lock will time out by itself
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Releases the advisory lock held on Connection
        /// </summary>
        public override void ReleaseAdvisoryLock()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"SELECT RELEASE_LOCK('{this.LockName}')";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        public override string GetCreateVersionTableSql()
        {
            return $@"CREATE TABLE IF NOT EXISTS {this.TableName} (
                        `Id` INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
                        `Version` BIGINT NOT NULL,
                        `AppliedOn` DATETIME NOT NULL,
                        `Description` TEXT NOT NULL
                    )";
        }

        /// <summary>
        /// Returns SQL to fetch the current version from the version table
        /// </summary>
        /// <returns>SQL to fetch the current version from the version table</returns>
        public override string GetCurrentVersionSql()
        {
            return $@"SELECT `Version` FROM {this.TableName} ORDER BY `Id` DESC LIMIT 1";
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        public override string GetSetVersionSql()
        {
            return $@"INSERT INTO {this.TableName} (`Version`, `AppliedOn`, `Description`) VALUES (@Version, NOW(), @Description)";
        }
    }
}
