using System.Collections.Generic;

namespace SimpleMigrations
{
    /// <summary>
    /// Interface defining how to interact with a SimpleMigrator
    /// </summary>
    public interface ISimpleMigrator
    {
        /// <summary>
        /// Gets and sets the logger to use. May be null
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Gets the currently-applied migration
        /// </summary>
        MigrationData CurrentMigration { get; }

        /// <summary>
        /// Gets the latest available migration
        /// </summary>
        MigrationData LatestMigration { get; }

        /// <summary>
        /// Gets all available migrations
        /// </summary>
        IReadOnlyList<MigrationData> Migrations { get; }

        /// <summary>
        /// Load all available migrations, and the current state of the database
        /// </summary>
        void Load();

        /// <summary>
        /// Migrate up to the latest version
        /// </summary>
        void MigrateToLatest();

        /// <summary>
        /// Migrate to a specific version
        /// </summary>
        /// <param name="newVersion">Version to migrate to</param>
        void MigrateTo(long newVersion);

        /// <summary>
        /// Pretend that the database is at the given version, without running any migrations.
        /// This is useful for introducing SimpleMigrations to an existing database.
        /// </summary>
        /// <param name="version">Version to introduce</param>
        void Baseline(long version);
    }
}
