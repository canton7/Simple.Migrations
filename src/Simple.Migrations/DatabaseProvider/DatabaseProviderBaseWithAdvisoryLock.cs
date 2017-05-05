using System;
using System.Data;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// <see cref="DatabaseProviderBase"/> subclass for databases which use an advisory lock to guard against concurrent
    /// migrators.
    /// </summary>
    /// <remarks>
    /// This uses a single connection, which it does not take ownership of (i.e. it is up to the caller to close it).
    /// </remarks>
    public abstract class DatabaseProviderBaseWithAdvisoryLock : DatabaseProviderBase
    {
        /// <summary>
        /// Gets the connection used for all database operations
        /// </summary>
        protected DbConnection Connection { get; }

        /// <summary>
        /// Gets or sets the timeout when acquiring the advisory lock
        /// </summary>
        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(600);

        /// <summary>
        /// Initialises a new instance of the <see cref="DatabaseProviderBaseWithAdvisoryLock"/> class
        /// </summary>
        /// <param name="connection">Database connection to use for all operations</param>
        public DatabaseProviderBaseWithAdvisoryLock(DbConnection connection)
        {
            this.Connection = connection;
            if (this.Connection.State != ConnectionState.Open)
                this.Connection.Open();
        }

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
        public override long EnsurePrerequisitesCreatedAndGetCurrentVersion()
        {
            try
            {
                this.AcquireAdvisoryLock();

                return this.EnsurePrerequisitesCreatedAndGetCurrentVersion(this.Connection, null);
            }
            finally
            {
                this.ReleaseAdvisoryLock();
            }
        }

        /// <summary>
        /// Called when <see cref="SimpleMigrator{TConnection, TMigrationBase}.MigrateTo(long)"/> or <see cref="SimpleMigrator{TConnection, TMigrationBase}.Baseline(long)"/>
        /// is invoked, before any migrations are run. This invokes <see cref="AcquireAdvisoryLock"/> to acquire the advisory lock.
        /// </summary>
        /// <returns>Connection for the migrations to use</returns>
        public override DbConnection BeginOperation()
        {
            this.AcquireAdvisoryLock();
            return this.Connection;
        }

        /// <summary>
        /// Called after migrations are run, this invokes <see cref="ReleaseAdvisoryLock"/> to release the advisory lock.
        /// </summary>
        public override void EndOperation()
        {
            this.ReleaseAdvisoryLock();
        }

        /// <summary>
        /// Fetch the current database schema version, or 0.
        /// </summary>
        /// <remarks>
        /// This method is always invoked after a call to <see cref="BeginOperation"/>, but before a call to
        /// <see cref="EndOperation"/>. Therefore the advisory lock has already been acquired.
        /// </remarks>
        /// <returns>The current database schema version, or 0</returns>
        public override long GetCurrentVersion()
        {
            return this.GetCurrentVersion(this.Connection, null);
        }

        /// <summary>
        /// Update the VersionInfo table to indicate that the given migration was successfully applied.
        /// </summary>
        /// <remarks>
        /// This is always invoked after a call to <see cref="BeginOperation"/> but before a call to <see cref="EndOperation"/>,
        /// Therefore the advisory lock has already been acquired.
        /// </remarks>
        /// <param name="oldVersion">The previous version of the database schema</param>
        /// <param name="newVersion">The version of the new database schema</param>
        /// <param name="newDescription">The description of the migration which was applied</param>
        public override void UpdateVersion(long oldVersion, long newVersion, string newDescription)
        {
            this.UpdateVersion(oldVersion, newVersion, newDescription, this.Connection, null);
        }

        /// <summary>
        /// Acquires an advisory lock using <see cref="Connection"/>
        /// </summary>
        protected abstract void AcquireAdvisoryLock();

        /// <summary>
        /// Releases the advisory lock held on <see cref="Connection"/>
        /// </summary>
        protected abstract void ReleaseAdvisoryLock();
    }
}
