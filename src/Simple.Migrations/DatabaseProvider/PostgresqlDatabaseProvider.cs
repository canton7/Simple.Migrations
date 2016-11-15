namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an PostgreSQL database
    /// </summary>
    public class PostgresqlDatabaseProvider : DatabaseProviderBase
    {
        private const long advisoryLockKey = 2609878; // Chosen by fair dice roll

        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        public override string GetCreateVersionTableSql()
        {
            return $@"CREATE TABLE IF NOT EXISTS {this.TableName} (
                    Id SERIAL PRIMARY KEY,
                    Version bigint NOT NULL,
                    AppliedOn timestamp with time zone,
                    Description text NOT NULL
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
            return $@"INSERT INTO {this.TableName} (Version, AppliedOn, Description) VALUES (@Version, CURRENT_TIMESTAMP, @Description)";
        }

        public override void AcquireDatabaseLock()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"SELECT pg_advisory_lock({advisoryLockKey})";
                command.ExecuteNonQuery();
            }
        }

        public override void ReleaseDatabaseLock()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"SELECT pg_advisory_unlock({advisoryLockKey})";
                command.ExecuteNonQuery();
            }
        }
    }
}
