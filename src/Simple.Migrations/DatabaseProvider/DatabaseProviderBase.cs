using System;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// Database provider which acts by maintaining a table of applied versions
    /// </summary>
    /// <remarks>
    /// Methods on this class are called according to a strict sequence.
    /// 
    /// When <see cref="SimpleMigrator{TConnection, TMigrationBase}.Load"/> is called:
    ///     1. <see cref="EnsurePrerequisitesCreatedAndGetCurrentVersion()"/> is invoked
    /// 
    /// 
    /// When <see cref="SimpleMigrator{TConnection, TMigrationBase}.MigrateTo(long)"/> or 
    /// <see cref="SimpleMigrator{TConnection, TMigrationBase}.Baseline(long)"/> is called:
    ///     1. <see cref="BeginOperation"/>  is called.
    ///     2. <see cref="GetCurrentVersion()"/> is called.
    ///     3. <see cref="UpdateVersion(long, long, string)"/>is called (potentially multiple times)
    ///     4. <see cref="GetCurrentVersion()"/> is called.
    ///     5. <see cref="EndOperation"/>is called.
    ///     
    /// Different databases require different locking strategies to guard against concurrent migrators.
    /// 
    /// Databases which support advisory locks typically have a single connection, and use this for obtaining
    /// the advisory lock, running migrations, and updating the VersionInfo table. The advisory lock is obtained
    /// when creating the VersionInfo table also.
    /// 
    /// Databases which do not support advisory locks typically have a connection factory. Inside
    /// <see cref="BeginOperation"/> they will create two connections. One creates a transaction and uses it to
    /// lock the VersionInfo table, and also to update the VersionInfo table, while the other is used to run migrations.
    /// 
    /// The subclasses <see cref="DatabaseProviderBaseWithAdvisoryLock"/> and <see cref="DatabaseProviderBaseWithVersionTableLock"/>
    /// encapsulate these concepts.
    /// </remarks>
    public abstract class DatabaseProviderBase : IDatabaseProvider<DbConnection>
    {
        /// <summary>
        /// Table name used to store version info. Defaults to 'VersionInfo'
        /// </summary>
        public string TableName { get; set; } = "VersionInfo";

        /// <summary>
        /// If > 0, specifies the maximum length of the 'Description' field. Descriptions longer will be truncated
        /// </summary>
        /// <remarks>
        /// Database providers which put a maximum length on the Description field should set this to that length
        /// </remarks>
        protected int MaxDescriptionLength { get; set; }

        /// <summary>
        /// Ensures that the schema (if appropriate) and version table are created, and returns the current version.
        /// </summary>
        /// <remarks>
        /// This is not surrounded by calls to <see cref="BeginOperation"/> or <see cref="EndOperation"/>, so
        /// it should do whatever locking is appropriate to guard against concurrent migrators.
        /// 
        /// If the version table is empty, this should return 0.
        /// </remarks>
        /// <returns>The current version, or 0</returns>
        public abstract long EnsurePrerequisitesCreatedAndGetCurrentVersion();

        /// <summary>
        /// Helper method which ensures that the schema and VersionInfo table are created, and fetches the current version from it,
        /// using the given connection and transaction.
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="transaction">Transaction to use</param>
        /// <returns>The current version, or 0</returns>
        protected virtual long EnsurePrerequisitesCreatedAndGetCurrentVersion(DbConnection connection, DbTransaction transaction)
        {
            var createSchemaSql = this.GetCreateSchemaTableSql();
            if (!string.IsNullOrEmpty(createSchemaSql))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createSchemaSql;
                    command.Transaction = transaction;
                    command.ExecuteNonQuery();
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = this.GetCreateVersionTableSql();
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }

            return this.GetCurrentVersion(connection, transaction);
        }

        /// <summary>
        /// Called when <see cref="SimpleMigrator{TConnection, TMigrationBase}.MigrateTo(long)"/> or <see cref="SimpleMigrator{TConnection, TMigrationBase}.Baseline(long)"/>
        /// is invoked, before any migrations are run. This should create connections and/or transactions and/or locks if necessary,
        /// and return the connection for the migrations to use.
        /// </summary>
        /// <returns>Connection for the migrations to use</returns>
        public abstract DbConnection BeginOperation();

        /// <summary>
        /// Cleans up any connections and/or transactions and/or locks created by <see cref="BeginOperation"/>
        /// </summary>
        /// <remarks>
        /// This is always paired with a call to <see cref="BeginOperation"/>: it is called exactly once for every time that
        /// <see cref="BeginOperation"/> is called.
        /// </remarks>
        public abstract void EndOperation();

        /// <summary>
        /// Fetch the current database schema version, or 0.
        /// </summary>
        /// <remarks>
        /// This method is always invoked after a call to <see cref="BeginOperation"/>, but before a call to
        /// <see cref="EndOperation"/>. Therefore it may use a connection created by <see cref="BeginOperation"/>.
        /// 
        /// If this databases uses locking on the VersionInfo table to guard against concurrent migrators, this
        /// method should use the connection that lock was acquired on.
        /// </remarks>
        /// <returns>The current database schema version, or 0</returns>
        public abstract long GetCurrentVersion();

        /// <summary>
        /// Helper method to fetch the current database version, using the given connection and transaction.
        /// </summary>
        /// <remarks>
        /// The transaction may be null.
        /// </remarks>
        /// <param name="connection">Connection to use</param>
        /// <param name="transaction">transaction to use, may be null</param>
        /// <returns>The current database schema version, or 0</returns>
        protected virtual long GetCurrentVersion(DbConnection connection, DbTransaction transaction)
        {
            long version = 0;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = this.GetCurrentVersionSql();
                command.Transaction = transaction;
                var result = command.ExecuteScalar();

                if (result != null)
                {
                    try
                    {
                        version = Convert.ToInt64(result);
                    }
                    catch
                    {
                        throw new MigrationException("Database Provider returns a value for the current version which isn't a long");
                    }
                }
            }

            return version;
        }

        /// <summary>
        /// Update the VersionInfo table to indicate that the given migration was successfully applied.
        /// </summary>
        /// <remarks>
        /// This is always invoked after a call to <see cref="BeginOperation"/> but before a call to <see cref="EndOperation"/>,
        /// Therefore it may use a connection created by <see cref="BeginOperation"/>.
        /// </remarks>
        /// <param name="oldVersion">The previous version of the database schema</param>
        /// <param name="newVersion">The version of the new database schema</param>
        /// <param name="newDescription">The description of the migration which was applied</param>
        public abstract void UpdateVersion(long oldVersion, long newVersion, string newDescription);

        /// <summary>
        /// Helper method to update the VersionInfo table to indicate that the given migration was successfully applied,
        /// using the given connectoin and transaction.
        /// </summary>
        /// <param name="oldVersion">The previous version of the database schema</param>
        /// <param name="newVersion">The version of the new database schema</param>
        /// <param name="newDescription">The description of the migration which was applied</param>
        /// <param name="connection">Connection to use</param>
        /// <param name="transaction">Transaction to use, may be null</param>
        protected virtual void UpdateVersion(long oldVersion, long newVersion, string newDescription, DbConnection connection, DbTransaction transaction)
        {
            if (this.MaxDescriptionLength > 0 && newDescription.Length > this.MaxDescriptionLength)
            {
                newDescription = newDescription.Substring(0, this.MaxDescriptionLength - 3) + "...";
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = this.GetSetVersionSql();

                var versionParam = command.CreateParameter();
                versionParam.ParameterName = "Version";
                versionParam.Value = newVersion;
                command.Parameters.Add(versionParam);

                //var oldVersionParam = command.CreateParameter();
                //oldVersionParam.ParameterName = "OldVersion";
                //oldVersionParam.Value = oldVersion;
                //command.Parameters.Add(oldVersionParam);

                var nameParam = command.CreateParameter();
                nameParam.ParameterName = "Description";
                nameParam.Value = newDescription;
                command.Parameters.Add(nameParam);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Should return 'CREATE SCHEMA IF NOT EXISTS', or similar
        /// </summary>
        /// <remarks>
        /// Don't override if the database has no concept of schemas
        /// </remarks>
        protected virtual string GetCreateSchemaTableSql() => null;

        /// <summary>
        /// Should return 'CREATE TABLE IF NOT EXISTS', or similar
        /// </summary>
        protected abstract string GetCreateVersionTableSql();

        /// <summary>
        /// Should return SQL which selects a single long value - the current version - or 0/NULL if there is no current version
        /// </summary>
        protected abstract string GetCurrentVersionSql();

        /// <summary>
        /// Returns SQL which upgrades to a particular version.
        /// </summary>
        /// <remarks>
        /// The following parameters may be used:
        ///  - @Version - the long version to set
        ///  - @Description - the description of the version
        ///  - @OldVersion - the long version being migrated from
        /// </remarks>
        protected abstract string GetSetVersionSql();
    }
}
