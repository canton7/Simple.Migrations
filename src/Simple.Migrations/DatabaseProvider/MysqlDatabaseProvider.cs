using System;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an MySQL database
    /// </summary>
    public class MysqlDatabaseProvider : DatabaseProviderBaseWithAdvisoryLock
    {
        private const string lockName = "SimpleMigratorExclusiveLock";

        public MysqlDatabaseProvider(DbConnection connection)
            : base(connection)
        {
        }

        public override void AcquireAdvisoryLock(DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT GET_LOCK('{lockName}', 600)";
                command.ExecuteNonQuery();
            }
        }

        public override void ReleaseAdvisoryLock(DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT RELEASE_LOCK('{lockName}')";
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
                        `Version` INT NOT NULL,
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
