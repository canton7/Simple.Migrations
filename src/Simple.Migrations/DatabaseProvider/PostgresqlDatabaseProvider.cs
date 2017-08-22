﻿using System;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an PostgreSQL database
    /// </summary>
    /// <remarks>
    /// PostgreSQL supports advisory locks, so these are used to guard against concurrent migrators.
    /// </remarks>
    public class PostgresqlDatabaseProvider : DatabaseProviderBaseWithAdvisoryLock
    {
        /// <summary>
        /// Gets or sets the schema name used to store the version table.
        /// </summary>
        /// /// <remarks>
        /// If this is set to an empty string, then no schema is created. It is the user's responsibility to create the schema
        /// (if necessary) with the correct name and permissions before running the <see cref="SimpleMigrator"/>. This may be
        /// required if the user which Simple.Migrators is running as does not have the correct permissions to check whether the
        /// schema has been created.
        /// </remarks>
        public string SchemaName { get; set; } = "public";

        /// <summary>
        /// Gets or sets the key to use when acquiring the advisory lock
        /// </summary>
        public int AdvisoryLockKey { get; set; } = 2609878; // Chosen by fair dice roll

        /// <summary>
        /// Initialises a new instance of the <see cref="PostgresqlDatabaseProvider"/> class
        /// </summary>
        /// <param name="connection">Connection to use to run migrations. The caller is responsible for closing this.</param>
        /// <param name="ensurePrerequisitesCreated">Flag provided to ensure that the schema (if appropriate) and version table are created</param>
        public PostgresqlDatabaseProvider(DbConnection connection, bool ensurePrerequisitesCreated = true)
            : base(connection, ensurePrerequisitesCreated)
        {
        }

        /// <summary>
        /// Acquires an advisory lock using Connection
        /// </summary>
        protected override void AcquireAdvisoryLock()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"SELECT pg_advisory_lock({this.AdvisoryLockKey})";
                command.CommandTimeout = (int)this.LockTimeout.TotalSeconds;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Releases the advisory lock held on Connection
        /// </summary>
        protected override void ReleaseAdvisoryLock()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"SELECT pg_advisory_unlock({this.AdvisoryLockKey})";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns SQL to create the schema
        /// </summary>
        /// <returns>SQL to create the schema</returns>
        protected override string GetCreateSchemaTableSql()
        {
            return String.IsNullOrWhiteSpace(this.SchemaName) ? String.Empty : $@"CREATE SCHEMA IF NOT EXISTS {this.SchemaName}";
        }

        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        protected override string GetCreateVersionTableSql()
        {
            return $@"CREATE TABLE IF NOT EXISTS {this.SchemaName}.{this.TableName} (
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
        protected override string GetCurrentVersionSql()
        {
            return $@"SELECT Version FROM {this.SchemaName}.{this.TableName} ORDER BY Id DESC LIMIT 1";
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        protected override string GetSetVersionSql()
        {
            return $@"INSERT INTO {this.SchemaName}.{this.TableName} (Version, AppliedOn, Description) VALUES (@Version, CURRENT_TIMESTAMP, @Description)";
        }
    }
}
