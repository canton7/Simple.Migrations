using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// Base class for migrators, allowing any database type and custom configuration of migrations
    /// </summary>
    /// <typeparam name="TDatabase">Type of database connection to use</typeparam>
    /// <typeparam name="TMigrationBase">Type of migration base class</typeparam>
    public abstract class SimpleMigratorBase<TDatabase, TMigrationBase> where TMigrationBase : IMigration<TDatabase>
    {
        private readonly Assembly migrationAssembly;
        private readonly IConnectionProvider<TDatabase> connectionProvider;
        private readonly IVersionProvider<TDatabase> versionProvider;
        private readonly ILogger logger;

        /// <summary>
        /// Gets the currently-applied migration
        /// </summary>
        public MigrationData CurrentMigration { get; private set; }

        /// <summary>
        /// Gets the latest available migration
        /// </summary>
        public MigrationData LatestMigration { get; private set; }

        /// <summary>
        /// Gets all available migrations
        /// </summary>
        public IReadOnlyList<MigrationData> Migrations { get; private set; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigratorBase{TDatabase, TMigrationBase}"/> class
        /// </summary>
        /// <param name="migrationAssembly">Assembly to search for migrations</param>
        /// <param name="connectionProvider">Connection provider to use to communicate with the database</param>
        /// <param name="versionProvider">Version provider to use to get/set the current version from the database</param>
        /// <param name="logger">Logger to use to log progress and messages</param>
        public SimpleMigratorBase(
            Assembly migrationAssembly,
            IConnectionProvider<TDatabase> connectionProvider,
            IVersionProvider<TDatabase> versionProvider,
            ILogger logger = null)
        {
            this.migrationAssembly = migrationAssembly;
            this.connectionProvider = connectionProvider;
            this.versionProvider = versionProvider;
            this.logger = logger ?? new NullLogger();
        }

        /// <summary>
        /// Ensure that .Load() has bene called
        /// </summary>
        protected void EnsureLoaded()
        {
            if (this.Migrations == null)
                throw new InvalidOperationException("You must call .Load() before calling this method");
        }

        /// <summary>
        /// Load all available migrations, and the current state of the database
        /// </summary>
        public virtual void Load()
        {
            this.versionProvider.EnsureCreated(this.connectionProvider.Connection);

            this.SetMigrations();
            this.SetCurrentVersion();
            this.LatestMigration = this.Migrations.Last();
        }

        /// <summary>
        /// Set this.Migrations, by scanning this.migrationAssembly for migrations
        /// </summary>
        protected virtual void SetMigrations()
        {
            var migrations = (from type in this.migrationAssembly.GetTypes()
                              let attribute = type.GetCustomAttribute<MigrationAttribute>()
                              where attribute != null
                              where typeof(TMigrationBase).IsAssignableFrom(type)
                              orderby attribute.Version
                              select new MigrationData(attribute.Version, attribute.Description ?? type.Name, type, attribute.UseTransaction)).ToList();

            if (migrations.Any(x => x.Version <= 0))
                throw new MigrationException("Migrations must all have a version > 0");

            var initialMigration = new MigrationData(0, "Empty Schema", null, false);
            this.Migrations = new[] { initialMigration }.Concat(migrations).ToList().AsReadOnly();
        }

        /// <summary>
        /// Set this.CurrentMigration, by inspecting the database
        /// </summary>
        protected virtual void SetCurrentVersion()
        {
            var currentVersion = this.versionProvider.GetCurrentVersion(this.connectionProvider.Connection);
            var currentMigrationCandidates = this.Migrations.Where(x => x.Version == currentVersion);
            if (!currentMigrationCandidates.Any())
                throw new MigrationException(String.Format("Unable to find migration with the current version: {0}", currentVersion));

            this.CurrentMigration = currentMigrationCandidates.First();
        }

        /// <summary>
        /// Migrate up to the latest version
        /// </summary>
        public virtual void MigrateUp()
        {
            this.EnsureLoaded();

            this.MigrateTo(this.LatestMigration.Version);
        }

        /// <summary>
        /// Migrate to a specific version
        /// </summary>
        /// <param name="newVersion">Version to migrate to</param>
        public virtual void MigrateTo(long newVersion)
        {
            this.EnsureLoaded();

            if (!this.Migrations.Any(x => x.Version == newVersion))
                throw new ArgumentException(String.Format("Could not find migration with version {0}", newVersion));

            var direction = newVersion > this.CurrentMigration.Version ? MigrationDirection.Up : MigrationDirection.Down;
            var migrations = this.FindMigrationsToRun(newVersion, direction).ToList();

            // Nothing to do?
            if (!migrations.Any())
                return;

            this.RunMigrations(migrations, direction);
        }

        /// <summary>
        /// Find a list of migrations to run, to bring the database up to the given version
        /// </summary>
        /// <param name="newVersion">Version to bring the database to</param>
        /// <param name="direction">Direction of migrations</param>
        /// <returns>A sorted list of migrations to run, with the first migration to run being first in the collection</returns>
        protected virtual IEnumerable<MigrationData> FindMigrationsToRun(long newVersion, MigrationDirection direction)
        {
            IEnumerable<MigrationData> migrations;
            if (direction == MigrationDirection.Up)
            {
                migrations = this.Migrations.Where(x => x.Version > this.CurrentMigration.Version && x.Version <= newVersion).OrderBy(x => x.Version);
            }
            else
            {
                migrations = this.Migrations.Where(x => x.Version < this.CurrentMigration.Version && x.Version >= newVersion).OrderByDescending(x => x.Version);
            }

            return migrations;
        }

        /// <summary>
        /// Run the given list of migrations
        /// </summary>
        /// <param name="migrations">Migrations to run, must be sorted</param>
        /// <param name="direction">Direction of mgirations</param>
        protected virtual void RunMigrations(IEnumerable<MigrationData> migrations, MigrationDirection direction)
        {
            // Migrations must be sorted

            // this.CurrentMigration is updated by MigrateStep
            var originalMigration = this.CurrentMigration;

            var oldMigration = this.CurrentMigration;
            this.logger.BeginSequence(originalMigration, migrations.Last());
            try
            {
                foreach (var newMigration in migrations)
                {
                    this.MigrateStep(oldMigration, newMigration, direction);
                    oldMigration = newMigration;
                }

                this.logger.EndSequence(originalMigration, this.CurrentMigration);
            }
            catch (Exception e)
            {
                this.logger.EndSequenceWithError(e, originalMigration, this.CurrentMigration);
                throw;
            }
        }

        /// <summary>
        /// Perform a single migration
        /// </summary>
        /// <param name="oldMigration">Migration to migrate from</param>
        /// <param name="newMigration">Migration to migrate to</param>
        /// <param name="direction">Direction of the migration</param>
        protected virtual void MigrateStep(MigrationData oldMigration, MigrationData newMigration, MigrationDirection direction)
        {
            var migrationToRun = direction == MigrationDirection.Up ? newMigration : oldMigration;

            try
            {
                if (migrationToRun.UseTransaction)
                    this.connectionProvider.BeginTransaction();

                var migration = this.CreateMigration(migrationToRun);

                this.logger.BeginMigration(migrationToRun, direction);

                if (direction == MigrationDirection.Up)
                    migration.Up();
                else
                    migration.Down();

                this.versionProvider.UpgradeVersion(this.connectionProvider.Connection, oldMigration.Version, newMigration.Version, newMigration.Description);

                this.logger.EndMigration(migrationToRun, direction);

                if (migrationToRun.UseTransaction)
                    this.connectionProvider.CommitTransaction();

                this.CurrentMigration = newMigration;
            }
            catch (Exception e)
            {
                if (migrationToRun.UseTransaction)
                    this.connectionProvider.RollbackTransaction();

                this.logger.EndMigrationWithError(e, migrationToRun, direction);

                throw;
            }
        }

        /// <summary>
        /// Create and configure an instance of a migration
        /// </summary>
        /// <param name="migrationData">Data to create the migration for</param>
        /// <returns>An instantiated and configured migration</returns>
        protected virtual IMigration<TDatabase> CreateMigration(MigrationData migrationData)
        {
            var instance = (TMigrationBase)Activator.CreateInstance(migrationData.Type);

            instance.Database = this.connectionProvider.Connection;
            instance.Logger = this.logger;

            return instance;
        }
    }
}
