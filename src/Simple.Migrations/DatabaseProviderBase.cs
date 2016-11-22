using System;
using System.Data;
using System.Data.Common;

namespace SimpleMigrations
{
    /// <summary>
    /// Database provider which acts by maintaining a table of applied versions
    /// </summary>
    public abstract class DatabaseProviderBase : IDatabaseProvider<DbConnection>
    {
        /// <summary>
        /// Table name used to store version info. Defaults to 'VersionInfo'
        /// </summary>
        public string TableName { get; set; } = "VersionInfo";

        /// <summary>
        /// Gets or sets a value indicating whether the database lock should be acquired when invoking
        /// <see cref="EnsureCreatedAndGetCurrentVersion(Func{DbConnection})"/>
        /// </summary>
        protected bool AcquireDatabaseLockForEnsureCreatedAndGetCurrentVersion { get; set; }

        /// <summary>
        /// If > 0, specifies the maximum length of the 'Description' field. Descriptions longer will be truncated
        /// </summary>
        /// <remarks>
        /// Database providers which put a maximum length on the Description field should set this to that length
        /// </remarks>
        protected int MaxDescriptionLength { get; set; }

        protected DbConnection VersionTableConnection { get; set; }

        public virtual DbConnection BeginOperation(Func<DbConnection> connectionFactory)
        {
            this.VersionTableConnection = connectionFactory();
            this.VersionTableConnection.Open();

            var migrationConnection = connectionFactory();
            migrationConnection.Open();

            this.AcquireDatabaseLock(this.VersionTableConnection);

            return migrationConnection;
        }

        public void EndOperation()
        {
            this.ReleaseDatabaseLock(this.VersionTableConnection);

            this.VersionTableConnection?.Dispose();
            this.VersionTableConnection = null;
        }

        protected abstract void AcquireDatabaseLock(DbConnection connection);
        protected abstract void ReleaseDatabaseLock(DbConnection connection);

        public virtual long EnsureCreatedAndGetCurrentVersion(Func<DbConnection> connectionFactory)
        {
            using (var connection = connectionFactory())
            {
                connection.Open();

                try
                {
                    if (this.AcquireDatabaseLockForEnsureCreatedAndGetCurrentVersion)
                        this.AcquireDatabaseLock(connection);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = this.GetCreateVersionTableSql();
                        command.ExecuteNonQuery();
                    }

                    return this.GetCurrentVersion(connection);
                }
                finally
                {
                    if (this.AcquireDatabaseLockForEnsureCreatedAndGetCurrentVersion)
                        this.ReleaseDatabaseLock(connection);
                }
            }
        }

        public long GetCurrentVersion()
        {
            return this.GetCurrentVersion(this.VersionTableConnection);
        }

        protected virtual long GetCurrentVersion(DbConnection connection)
        {
            long version = 0;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = this.GetCurrentVersionSql();
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

        public virtual void UpdateVersion(long oldVersion, long newVersion, string newDescription)
        {
            if (this.MaxDescriptionLength > 0 && newDescription.Length > this.MaxDescriptionLength)
            {
                newDescription = newDescription.Substring(0, this.MaxDescriptionLength - 3) + "...";
            }

            using (var command = this.VersionTableConnection.CreateCommand())
            {
                command.CommandText = this.GetSetVersionSql();

                var versionParam = command.CreateParameter();
                versionParam.ParameterName = "Version";
                versionParam.Value = newVersion;
                command.Parameters.Add(versionParam);

                var nameParam = command.CreateParameter();
                nameParam.ParameterName = "Description";
                nameParam.Value = newDescription;
                command.Parameters.Add(nameParam);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Should 'CREATE TABLE IF NOT EXISTS', or similar
        /// </summary>
        public abstract string GetCreateVersionTableSql();

        /// <summary>
        /// Should return SQL which selects a single long value - the current version - or 0/NULL if there is no current version
        /// </summary>
        /// <returns></returns>
        public abstract string GetCurrentVersionSql();

        /// <summary>
        /// Returns SQL which upgrades to a particular version.
        /// </summary>
        /// <remarks>
        /// The following parameters should be used:
        ///  - @Version - the long version to set
        ///  - @Description - the description of the version
        /// </remarks>
        /// <returns></returns>
        public abstract string GetSetVersionSql();
    }
}
