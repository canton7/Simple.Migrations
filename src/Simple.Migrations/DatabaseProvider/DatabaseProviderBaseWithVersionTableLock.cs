using System;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// <see cref="DatabaseProviderBase"/> subclass for databases which use a transaction on the VersionInfo table to guard
    /// against concurrent migrators.
    /// </summary>
    /// <remarks>
    /// This uses two connections for each operation: one which uses a transaction to acquire a lock on the VersionInfo table
    /// and to update it, and another to run migrations on.
    /// </remarks>
    public abstract class DatabaseProviderBaseWithVersionTableLock : DatabaseProviderBase
    {
        /// <summary>
        /// Gets the factory used to create new database connections
        /// </summary>
        protected Func<DbConnection> ConnectionFactory { get; }

        /// <summary>
        /// Gets or sets the connection used to acquire a lock on the VersionInfo table, and to update the VersionInfo
        /// table.
        /// </summary>
        /// <remarks>
        /// This is set by <see cref="BeginOperation"/>, and cleated by <see cref="EndOperation"/>.
        /// </remarks>
        protected DbConnection VersionTableConnection { get; set; }

        /// <summary>
        /// Gets or sets the transaction on the <see cref="VersionTableConnection"/> used to lock it.
        /// </summary>
        /// <remarks>
        /// This is set by <see cref="BeginOperation"/>, and cleated by <see cref="EndOperation"/>.
        /// </remarks>
        protected DbTransaction VersionTableLockTransaction { get; set; }

        /// <summary>
        /// Gets or sets the connection to be used by migrations.
        /// </summary>
        /// <remarks>
        /// This is set by <see cref="BeginOperation"/>, and cleated by <see cref="EndOperation"/>.
        /// </remarks>
        protected DbConnection MigrationsConnection { get; set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="DatabaseProviderBaseWithVersionTableLock"/> class
        /// </summary>
        /// <param name="connectionFactory">Factory to be used to create new connections</param>
        public DatabaseProviderBaseWithVersionTableLock(Func<DbConnection> connectionFactory)
        {
            this.ConnectionFactory = connectionFactory;
        }

        /// <summary>
        /// Ensures that the version table is created, and returns the current version.
        /// </summary>
        /// <remarks>
        /// This is not surrounded by calls to <see cref="BeginOperation"/> or <see cref="EndOperation"/>, so
        /// it should create its own connection.
        /// 
        /// If the version table is empty, this should return 0.
        /// </remarks>
        /// <returns>The current version, or 0</returns>
        public override long EnsurePrerequisitesCreatedAndGetCurrentVersion()
        {
            using (var connection = this.ConnectionFactory())
            {
                connection.Open();

                return this.EnsurePrerequisitesCreatedAndGetCurrentVersion(connection, null);
            }
        }

        /// <summary>
        /// Called when <see cref="SimpleMigrator{TConnection, TMigrationBase}.MigrateTo(long)"/> or <see cref="SimpleMigrator{TConnection, TMigrationBase}.Baseline(long)"/>
        /// is invoked, before any migrations are run. This creates the <see cref="VersionTableConnection"/> and 
        /// <see cref="MigrationsConnection"/>, and invokes <see cref="AcquireVersionTableLock"/> to acquire the VersionInfo table lock.
        /// </summary>
        /// <returns>Connection for the migrations to use</returns>
        public override DbConnection BeginOperation()
        {
            this.VersionTableConnection = this.ConnectionFactory();
            this.VersionTableConnection.Open();

            this.MigrationsConnection = this.ConnectionFactory();
            this.MigrationsConnection.Open();

            this.AcquireVersionTableLock();

            return this.MigrationsConnection;
        }

        /// <summary>
        /// Called after migrations are run, this invokes <see cref="AcquireVersionTableLock"/> to release the VersionInfo table lock, and
        /// then closes <see cref="VersionTableConnection"/> and <see cref="MigrationsConnection"/>"/>
        /// </summary>
        public override void EndOperation()
        {
            this.ReleaseVersionTableLock();

            this.VersionTableConnection.Dispose();
            this.VersionTableConnection = null;

            this.MigrationsConnection.Dispose();
            this.MigrationsConnection = null;
        }

        /// <summary>
        /// Fetch the current database schema version, or 0.
        /// </summary>
        /// <remarks>
        /// This method is always invoked after a call to <see cref="BeginOperation"/>, but before a call to
        /// <see cref="EndOperation"/>. It should use <see cref="VersionTableConnection"/> and <see cref="VersionTableLockTransaction"/>
        /// </remarks>
        /// <returns>The current database schema version, or 0</returns>
        public override long GetCurrentVersion()
        {
            return this.GetCurrentVersion(this.VersionTableConnection, this.VersionTableLockTransaction);
        }

        /// <summary>
        /// Update the VersionInfo table to indicate that the given migration was successfully applied.
        /// </summary>
        /// <remarks>
        /// This is always invoked after a call to <see cref="BeginOperation"/> but before a call to <see cref="EndOperation"/>,
        /// It should use <see cref="VersionTableConnection"/> and <see cref="VersionTableLockTransaction"/>
        /// </remarks>
        /// <param name="oldVersion">The previous version of the database schema</param>
        /// <param name="newVersion">The version of the new database schema</param>
        /// <param name="newDescription">The description of the migration which was applied</param>
        /// <param name="migrationDirection">The direction of the migration which was applied</param>
        public override void UpdateVersion(long oldVersion, long newVersion, string newDescription, MigrationDirection? migrationDirection = null)
        {
            this.UpdateVersion(oldVersion, newVersion, newDescription, this.VersionTableConnection, this.VersionTableLockTransaction);
        }

        /// <summary>
        /// Creates and sets <see cref="VersionTableLockTransaction"/>, and uses it to lock the VersionInfo table
        /// </summary>
        protected abstract void AcquireVersionTableLock();

        /// <summary>
        /// Destroys <see cref="VersionTableLockTransaction"/>, thus releasing the VersionInfo table lock
        /// </summary>
        protected abstract void ReleaseVersionTableLock();
    }
}
