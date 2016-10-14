using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// Base class for migrators, allowing any database type and custom configuration of migrations
    /// </summary>
    /// <typeparam name="TDatabase">Type of database connection to use</typeparam>
    /// <typeparam name="TMigrationBase">Type of migration base class</typeparam>
    /// <typeparam name="TTransaction">Type of transaction connection to use</typeparam>
    public class SimpleMigrator<TDatabase, TTransaction, TMigrationBase>  : ISimpleMigrator
        where TMigrationBase : IMigration<TDatabase, TTransaction>
    {
        /// <summary>
        /// Assembly to search for migrations
        /// </summary>
        protected readonly Assembly MigrationAssembly;

        /// <summary>
        /// Connection provider
        /// </summary>
        protected readonly IConnectionProvider<TDatabase, TTransaction> ConnectionProvider;

        /// <summary>
        /// Version provider, providing access to the version table
        /// </summary>
        protected readonly IVersionProvider<TDatabase, TTransaction> VersionProvider;

        /// <summary>
        /// Gets and sets the logger to use. May be null
        /// </summary>
        public ILogger Logger { get; set; }

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

        private readonly NullLogger nullLogger = new NullLogger();

        /// <summary>
        /// Gets a logger which is not null
        /// </summary>
        protected ILogger NotNullLogger
        {
            get { return this.Logger ?? this.nullLogger; }
        }

        private bool isLoaded;

        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator{TDatabase, TMigrationBase}"/> class
        /// </summary>
        /// <param name="migrationAssembly">Assembly to search for migrations</param>
        /// <param name="connectionProvider">Connection provider to use to communicate with the database</param>
        /// <param name="versionProvider">Version provider to use to get/set the current version from the database</param>
        /// <param name="logger">Logger to use to log progress and messages</param>
        public SimpleMigrator(
            Assembly migrationAssembly,
            IConnectionProvider<TDatabase, TTransaction> connectionProvider,
            IVersionProvider<TDatabase, TTransaction> versionProvider,
            ILogger logger = null)
        {
            this.MigrationAssembly = migrationAssembly;
            this.ConnectionProvider = connectionProvider;
            this.VersionProvider = versionProvider;
            this.Logger = logger;
        }

        /// <summary>
        /// Ensure that .Load() has bene called
        /// </summary>
        protected void EnsureLoaded()
        {
            if (!this.isLoaded)
                throw new InvalidOperationException("You must call .Load() before calling this method");
        }

        /// <summary>
        /// Load all available migrations, and the current state of the database
        /// </summary>
        public virtual void Load()
        {
            if (this.isLoaded)
                return;

            this.VersionProvider.EnsureCreated(this.ConnectionProvider.Connection);

            this.FindAndSetMigrations();
            this.SetCurrentVersion();
            this.LatestMigration = this.Migrations.Last();

            this.isLoaded = true;
        }

        /// <summary>
        /// Set this.Migrations, by scanning this.migrationAssembly for migrations
        /// </summary>
        protected virtual void FindAndSetMigrations()
        {
            var migrations = (from type in this.MigrationAssembly.GetTypes()
                              let attribute = type.GetCustomAttribute<MigrationAttribute>()
                              where attribute != null
                              orderby attribute.Version
                              select new MigrationData(attribute.Version, attribute.Description, type, attribute.UseTransaction)).ToList();

            if (!migrations.Any())
                throw new MigrationException(String.Format("Could not find any migrations in the assembly you provided ({0}). Migrations must be decorated with [Migration]", this.MigrationAssembly.GetName().Name));

            if (migrations.Any(x => !typeof(TMigrationBase).IsAssignableFrom(x.Type)))
                throw new MigrationException(String.Format("All migrations must derive from / implement {0}", typeof(TMigrationBase).Name));

            if (migrations.Any(x => x.Version <= 0))
                throw new MigrationException("Migrations must all have a version > 0");

            var versionLookup = migrations.ToLookup(x => x.Version);
            var firstDuplicate = versionLookup.FirstOrDefault(x => x.Count() > 1);
            if (firstDuplicate != null)
                throw new MigrationException(String.Format("Found more than one migration with version {0}", firstDuplicate.Key));

            var initialMigration = new MigrationData(0, "Empty Schema", null, false);
            this.Migrations = new[] { initialMigration }.Concat(migrations).ToList().AsReadOnly();
        }

        /// <summary>
        /// Set this.CurrentMigration, by inspecting the database
        /// </summary>
        protected virtual void SetCurrentVersion()
        {
            var currentVersion = this.VersionProvider.GetCurrentVersion(this.ConnectionProvider.Connection);
            var currentMigration = this.Migrations.FirstOrDefault(x => x.Version == currentVersion);
            if (currentMigration == null)
                throw new MigrationException(String.Format("Unable to find migration with the current version: {0}", currentVersion));

            this.CurrentMigration = currentMigration;
        }

        /// <summary>
        /// Migrate up to the latest version
        /// </summary>
        public virtual void MigrateToLatest()
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
        /// Pretend that the database is at the given version, without running any migrations.
        /// This is useful for introducing SimpleMigrations to an existing database.
        /// </summary>
        /// <param name="version">Version to introduce</param>
        public virtual void Baseline(long version)
        {
            this.EnsureLoaded();

            if (this.CurrentMigration.Version != 0 && version != 0)
                throw new InvalidOperationException("Cannot baseline a database which has had migrations applied to it");

            var migration = this.Migrations.FirstOrDefault(x => x.Version == version);
            if (migration == null)
                throw new ArgumentException(String.Format("Could not find migration with version {0}", version));

            this.VersionProvider.UpdateVersion(this.ConnectionProvider.Connection, ConnectionProvider.Transaction, 0, version, migration.FullName);
            this.CurrentMigration = migration;

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
            this.NotNullLogger.BeginSequence(originalMigration, migrations.Last());
            try
            {
                foreach (var newMigration in migrations)
                {
                    this.MigrateStep(oldMigration, newMigration, direction);
                    oldMigration = newMigration;
                }

                this.NotNullLogger.EndSequence(originalMigration, this.CurrentMigration);
            }
            catch (Exception e)
            {
                this.NotNullLogger.EndSequenceWithError(e, originalMigration, this.CurrentMigration);
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
                    this.ConnectionProvider.BeginTransaction();

                var migration = this.CreateMigration(migrationToRun);

                this.NotNullLogger.BeginMigration(migrationToRun, direction);

                if (direction == MigrationDirection.Up)
                    migration.Up();
                else
                    migration.Down();

                this.VersionProvider.UpdateVersion(this.ConnectionProvider.Connection, ConnectionProvider.Transaction,oldMigration.Version, newMigration.Version, newMigration.FullName);

                this.NotNullLogger.EndMigration(migrationToRun, direction);

                if (migrationToRun.UseTransaction)
                    this.ConnectionProvider.CommitTransaction();

                this.CurrentMigration = newMigration;
            }
            catch (Exception e)
            {
                if (migrationToRun.UseTransaction)
                    this.ConnectionProvider.RollbackTransaction();

                this.NotNullLogger.EndMigrationWithError(e, migrationToRun, direction);

                throw;
            }
        }

        /// <summary>
        /// Create and configure an instance of a migration
        /// </summary>
        /// <param name="migrationData">Data to create the migration for</param>
        /// <returns>An instantiated and configured migration</returns>
        protected virtual TMigrationBase CreateMigration(MigrationData migrationData)
        {
            var instance = (TMigrationBase)Activator.CreateInstance(migrationData.Type);

            instance.DB = this.ConnectionProvider.Connection;
            instance.Transaction = this.ConnectionProvider.Transaction;
            instance.Logger = this.NotNullLogger;

            return instance;
        }
    }
}
