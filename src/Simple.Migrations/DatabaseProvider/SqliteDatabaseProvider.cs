using System;
using System.Data;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an SQLite database
    /// </summary>
    /// <remarks>
    /// SQLite does not support advisory locks, and its transaction model means that we cannot use a transaction on the
    /// VersionInfo table to guard against concurrent migrators. Therefore this database does not provide support for
    /// concurrent migrators.
    /// </remarks>
    public class SqliteDatabaseProvider : DatabaseProviderBaseWithAdvisoryLock
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SqliteDatabaseProvider"/> class
        /// </summary>
        /// <param name="connection">Connection to use to run migrations. The caller is responsible for closing this.</param>
        public SqliteDatabaseProvider(DbConnection connection)
            : base(connection)
        {
        }

        /// <summary>
        /// No-op: SQLite does not support advisory locks
        /// </summary>
        public override void AcquireAdvisoryLock() { }

        /// <summary>
        /// No-op: SQLite does not support advisory locks
        /// </summary>
        public override void ReleaseAdvisoryLock() { }

        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        public override string GetCreateVersionTableSql()
        {
            return $@"CREATE TABLE IF NOT EXISTS {this.TableName} (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Version INTEGER NOT NULL,
                    AppliedOn DATETIME NOT NULL,
                    Description TEXT NOT NULL
                )";
        }

        /// <summary>
        /// Returns SQL to fetch the current version from the version table
        /// </summary>
        /// <returns>SQL to fetch the current version from the version table</returns>
        public override string GetCurrentVersionSql()
        {
            return $@"SELECT Version FROM {this.TableName} ORDER BY Id DESC LIMIT 1";
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        public override string GetSetVersionSql()
        {
            return $@"INSERT INTO {this.TableName} (Version, AppliedOn, Description) VALUES (@Version, datetime('now', 'localtime'), @Description)";
        }
    }
}
