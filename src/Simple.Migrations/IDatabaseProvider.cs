using SimpleMigrations.DatabaseProvider;
using System;

namespace SimpleMigrations
{
    /// <summary>
    /// Interface representing database-type-specific operations which needs to be performed
    /// </summary>
    /// <remarks>
    /// Methods on this interface are called according to a strict sequence.
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
    /// <typeparam name="TConnection">Type of database connection</typeparam>
    public interface IDatabaseProvider<TConnection>
    {
        /// <summary>
        /// Flag to ensure that the schema (if appropriate) and version table are created.
        /// </summary>
        /// <remarks>
        /// Setting this to true will make the migrator to create the schema and version table,
        /// otherwise it will be assumed they exist already.
        /// </remarks>
        bool EnsurePrerequisitesCreated { get; }

        /// <summary>
        /// Called when <see cref="SimpleMigrator{TConnection, TMigrationBase}.MigrateTo(long)"/> or <see cref="SimpleMigrator{TConnection, TMigrationBase}.Baseline(long)"/>
        /// is invoked, before any migrations are run. This should create connections and/or transactions and/or locks if necessary,
        /// and return the connection for the migrations to use.
        /// </summary>
        /// <returns>Connection for the migrations to use</returns>
        TConnection BeginOperation();

        /// <summary>
        /// Cleans up any connections and/or transactions and/or locks created by <see cref="BeginOperation"/>
        /// </summary>
        /// <remarks>
        /// This is always paired with a call to <see cref="BeginOperation"/>: it is called exactly once for every time that
        /// <see cref="BeginOperation"/> is called.
        /// </remarks>
        void EndOperation();

        /// <summary>
        /// Ensures that the version table is created, and returns the current version.
        /// </summary>
        /// <remarks>
        /// This is not surrounded by calls to <see cref="BeginOperation"/> or <see cref="EndOperation"/>, so
        /// it should do whatever locking is appropriate to guard against concurrent migrators.
        /// 
        /// If the version table is empty, this should return 0.
        /// </remarks>
        /// <returns>The current version, or 0</returns>
        long EnsurePrerequisitesCreatedAndGetCurrentVersion();

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
        long GetCurrentVersion();

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
        void UpdateVersion(long oldVersion, long newVersion, string newDescription);
    }
}
