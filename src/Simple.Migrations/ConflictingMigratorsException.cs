using System;

namespace SimpleMigrations
{
    /// <summary>
    /// Exception thrown when two migrators are migrating the same database in different directions
    /// </summary>
    public class ConflictingMigratorsException : Exception
    {
        /// <summary>
        /// Gets the Migration which the migrator attempted to apply
        /// </summary>
        public MigrationData MigrationData { get; }

        /// <summary>
        /// Gets the version which the migrator expected the schema to be a
        /// </summary>
        public long ExpectedVersion { get; }

        /// <summary>
        /// Gets the version which the schema was actually a
        /// </summary>
        public long ActualVersion { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="ConflictingMigratorsException"/> class
        /// </summary>
        /// <param name="migrationData">Migration which the migrator attempted to apply</param>
        /// <param name="expectedVersion">The version which the migrator expected the schema to be at</param>
        /// <param name="actualVersion">The version which the schema was actually at</param>
        public ConflictingMigratorsException(MigrationData migrationData, long expectedVersion, long actualVersion)
            : base($"Tried to apply migration {migrationData} and expected the schema to be at version {expectedVersion}, but it was actually at version {actualVersion}. " +
                  "You probably have two migrators migrating in different directions at the same time. This is not a supported scenario.")
        {
            this.MigrationData = migrationData;
            this.ExpectedVersion = expectedVersion;
            this.ActualVersion = actualVersion;
        }
    }
}
