using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// Base class for migrators, allowing any database type and custom configuration of migrations
    /// </summary>
    /// <typeparam name="TConnection">Type of database connection to use</typeparam>
    /// <typeparam name="TMigrationBase">Type of migration base class</typeparam>
    public class SimpleMigrator<TConnection, TMigrationBase>  : ISimpleMigrator
        where TMigrationBase : IMigration<TConnection>
    {
        /// <summary>
        /// Assembly to search for migrations
        /// </summary>
        protected IMigrationProvider MigrationProvider { get; }

        /// <summary>
        /// Database provider, providing access to the version table, etc
        /// </summary>
        protected IDatabaseProvider<TConnection> DatabaseProvider { get; }

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
        public MigrationData LatestMigration
        {
            get
            {
                if (this.Migrations == null || this.Migrations.Count == 0)
                    return null;

                return this.Migrations.Last();
            }
        }

        /// <summary>
        /// Gets all available migrations
        /// </summary>
        public IReadOnlyList<MigrationData> Migrations { get; private set; }

        private bool isLoaded;

        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator{TDatabase, TMigrationBase}"/> class
        /// </summary>
        /// <param name="migrationProvider">Migration provider to use to find migration classes</param>
        /// <param name="databaseProvider">Database provider to use to interact with the version table, etc</param>
        /// <param name="logger">Logger to use to log progress and messages</param>
        public SimpleMigrator(
            IMigrationProvider migrationProvider,
            IDatabaseProvider<TConnection> databaseProvider,
            ILogger logger = null)
        {
            this.MigrationProvider = migrationProvider ?? throw new ArgumentNullException(nameof(migrationProvider));
            this.DatabaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
            this.Logger = logger;
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator{TDatabase, TMigrationBase}"/> class
        /// </summary>
        /// <param name="migrationsAssembly">Assembly to search for migrations</param>
        /// <param name="databaseProvider">Database provider to use to interact with the version table, etc</param>
        /// <param name="logger">Logger to use to log progress and messages</param>
        public SimpleMigrator(
            Assembly migrationsAssembly,
            IDatabaseProvider<TConnection> databaseProvider,
            ILogger logger = null)
            : this(new AssemblyMigrationProvider(migrationsAssembly), databaseProvider, logger)
        {
        }

        /// <summary>
        /// Ensure that .Load() has bene called
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Load"/> hasn't yet been called</exception>
        protected void EnsureLoaded()
        {
            if (!this.isLoaded)
                throw new InvalidOperationException("You must call .Load() before calling this method");
        }

        /// <summary>
        /// Load all available migrations, and the current state of the database
        /// </summary>
        /// <exception cref="MissingMigrationException">
        /// No <see cref="IMigration{TConnection}"/> could be found which corresponds to the version in the version table
        /// in your database.
        /// </exception>
        /// <exception cref="MigrationLoadFailedException">
        /// There is a problem with the migrations returned by the <see cref="IMigrationProvider"/>
        /// </exception>
        public virtual void Load()
        {
            // Always reload the current version

            if (!this.isLoaded)
            {
                this.FindAndSetMigrations();
            }

            long currentVersion;

            currentVersion = this.DatabaseProvider.EnsurePrerequisitesCreated
                ? this.DatabaseProvider.EnsurePrerequisitesCreatedAndGetCurrentVersion()
                : this.DatabaseProvider.GetCurrentVersion();

            this.SetCurrentVersion(currentVersion);

            this.isLoaded = true;
        }

        /// <summary>
        /// Load the migrations, and set <see cref="Migrations"/>
        /// </summary>
        /// <exception cref="MigrationLoadFailedException">
        /// There is a problem with the migrations returned by the <see cref="IMigrationProvider"/>
        /// </exception>
        protected virtual void FindAndSetMigrations()
        {
            var migrations = this.MigrationProvider.LoadMigrations()?.ToList();

            if (migrations == null || migrations.Count == 0)
                throw new MigrationLoadFailedException("The configured MigrationProvider did not find any migrations");

            var migrationBaseTypeInfo = typeof(TMigrationBase).GetTypeInfo();
            var firstNotImplementingTMigrationBase = migrations.FirstOrDefault(x => !migrationBaseTypeInfo.IsAssignableFrom(x.TypeInfo));
            if (firstNotImplementingTMigrationBase != null)
                throw new MigrationLoadFailedException($"Migration {firstNotImplementingTMigrationBase.FullName} must derive from / implement {typeof(TMigrationBase).Name}");

            var firstWithInvalidVersion = migrations.FirstOrDefault(x => x.Version <= 0);
            if (firstWithInvalidVersion != null)
                throw new MigrationLoadFailedException($"Migration {firstWithInvalidVersion.FullName} must have a version > 0");

            var versionLookup = migrations.ToLookup(x => x.Version);
            var firstDuplicate = versionLookup.FirstOrDefault(x => x.Count() > 1);
            if (firstDuplicate != null)
                throw new MigrationLoadFailedException($"Found more than one migration with version {firstDuplicate.Key} ({String.Join(", ", firstDuplicate)})");

            this.Migrations = new[] { MigrationData.EmptySchema }.Concat(migrations.OrderBy(x => x.Version)).ToList();
        }

        /// <summary>
        /// Sets <see cref="CurrentMigration"/>, by inspecting the database
        /// </summary>
        /// <exception cref="MissingMigrationException">
        /// No <see cref="IMigration{TConnection}"/> could be found which corresponds to the version in the version table
        /// in your database.
        /// </exception>
        protected virtual void SetCurrentVersion(long currentVersion)
        {
            var currentMigration = this.Migrations.FirstOrDefault(x => x.Version == currentVersion);
            this.CurrentMigration = currentMigration ?? throw new MissingMigrationException(currentVersion);
        }

        /// <summary>
        /// Migrate up to the latest version
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Load"/> has not yet been called</exception>
        /// <exception cref="MissingMigrationException">
        /// After acquiring exclusive access to the database, SimpleMigrator checks its version. This is thrown if
        /// no <see cref="IMigration{TConnection}"/> could be found which corresponds to the new version in the
        /// version table in your database.
        /// </exception>
        public virtual void MigrateToLatest()
        {
            this.EnsureLoaded();

            this.MigrateTo(this.LatestMigration.Version);
        }

        /// <summary>
        /// Migrate to a specific version
        /// </summary>
        /// <param name="newVersion">Version to migrate to</param>
        /// <exception cref="InvalidOperationException"><see cref="Load"/> has not yet been called</exception>
        /// <exception cref="ArgumentException">
        /// The newVersion parameter does not correspond to a migration in <see cref="Migrations"/>
        /// </exception>
        /// <exception cref="MissingMigrationException">
        /// After acquiring exclusive access to the database, SimpleMigrator checks its version. This is thrown if
        /// no <see cref="IMigration{TConnection}"/> could be found which corresponds to the new version in the
        /// version table in your database.
        /// </exception>
        public virtual void MigrateTo(long newVersion)
        {
            this.EnsureLoaded();

            try
            {
                var migrationsConnection = this.DatabaseProvider.BeginOperation();
                // Need to fetch the current version again after we've acquired the database lock, since someone else running
                // concurrently might have changed things.

                long currentVersion = this.DatabaseProvider.GetCurrentVersion();
                this.SetCurrentVersion(currentVersion);

                var toMigration = this.Migrations.FirstOrDefault(x => x.Version == newVersion);
                if (toMigration == null)
                    throw new MigrationNotFoundException(newVersion);
                var fromMigration = this.CurrentMigration;

                var direction = newVersion > this.CurrentMigration.Version ? MigrationDirection.Up : MigrationDirection.Down;
                var migrations = this.FindMigrationsToRun(newVersion, direction).ToList();

                if (migrations.Count > 0)
                {
                    this.Logger?.BeginSequence(fromMigration, toMigration);

                    try
                    {
                        foreach (var migrationDataPair in migrations)
                        {
                            var migrationDataToRun = (direction == MigrationDirection.Up) ? migrationDataPair.To : migrationDataPair.From;

                            try
                            {
                                this.Logger?.BeginMigration(migrationDataToRun, direction);

                                this.RunMigration(direction, migrationDataToRun, migrationsConnection);
                                this.DatabaseProvider.UpdateVersion(migrationDataPair.From.Version, migrationDataPair.To.Version, migrationDataPair.To.FullName);

                                this.Logger?.EndMigration(migrationDataToRun, direction);
                            }
                            catch (Exception e)
                            {
                                this.Logger?.EndMigrationWithError(e, migrationDataToRun, direction);
                                throw;
                            }
                        }

                        this.Logger?.EndSequence(fromMigration, this.CurrentMigration);
                    }
                    catch (Exception e)
                    {
                        this.Logger?.EndSequenceWithError(e, fromMigration, this.CurrentMigration);
                        throw;
                    }
                    finally
                    {
                        long finalVersion = this.DatabaseProvider.GetCurrentVersion();
                        this.SetCurrentVersion(finalVersion);
                    }
                }
            }
            finally
            {
                this.DatabaseProvider.EndOperation();
            }
        }

        /// <summary>
        /// Instantiate and execute a single migration
        /// </summary>
        /// <param name="direction">Diretion to run the migration in</param>
        /// <param name="migrationData"><see cref="MigrationData"/> describing to migration to instantiate and execute</param>
        /// <param name="connection">Connection to use to execute the migration</param>
        protected virtual void RunMigration(MigrationDirection direction, MigrationData migrationData, TConnection connection)
        {
            var migration = this.CreateMigration(migrationData);
            var data = new MigrationRunData<TConnection>(connection, this.Logger ?? NullLogger.Instance, direction);
            migration.RunMigration(data);
        }

        /// <summary>
        /// Pretend that the database is at the given version, without running any migrations.
        /// This is useful for introducing SimpleMigrations to an existing database.
        /// </summary>
        /// <param name="version">Version to introduce</param>
        /// <exception cref="InvalidOperationException">The database has had migrations applied to it</exception>
        /// <exception cref="ArgumentException">
        /// The version parameter does not correspond to a migration in <see cref="Migrations"/>
        /// </exception>
        public virtual void Baseline(long version)
        {
            this.EnsureLoaded();

            if (this.CurrentMigration.Version != 0 && version != 0)
                throw new InvalidOperationException("Cannot baseline a database which has had migrations applied to it");

            var migration = this.Migrations.FirstOrDefault(x => x.Version == version);
            if (migration == null)
                throw new MigrationNotFoundException(version);

            try
            {
                this.DatabaseProvider.BeginOperation();

                this.DatabaseProvider.UpdateVersion(0, version, migration.FullName);
            }
            finally
            {
                this.DatabaseProvider.EndOperation();
            }
                
            this.CurrentMigration = migration;
        }

        /// <summary>
        /// Find a list of migrations to run, to bring the database up to the given version
        /// </summary>
        /// <param name="newVersion">Version to bring the database to</param>
        /// <param name="direction">Direction of migrations</param>
        /// <returns>A sorted list of migrations to run, with the first migration to run being first in the collection</returns>
        protected virtual IEnumerable<MigrationDataPair> FindMigrationsToRun(long newVersion, MigrationDirection direction)
        {
            // We require that 'this.Migrations' is ordered, and a migration with version 'newVersions' exists

            if (direction == MigrationDirection.Up)
            {
                for (int i = 0; i < this.Migrations.Count; i++)
                {
                    if (this.Migrations[i].Version > this.CurrentMigration.Version && this.Migrations[i].Version <= newVersion)
                    {
                        yield return new MigrationDataPair(this.Migrations[i - 1], this.Migrations[i]);
                    }
                }
            }
            else
            {
                for (int i = this.Migrations.Count - 1; i >= 0; i--)
                {
                    if (this.Migrations[i].Version <= this.CurrentMigration.Version && this.Migrations[i].Version > newVersion)
                    {
                        yield return new MigrationDataPair(this.Migrations[i], this.Migrations[i - 1]);
                    }
                }
            }
        }

        /// <summary>
        /// Create and configure an instance of a migration
        /// </summary>
        /// <param name="migrationData">Data to create the migration for</param>
        /// <returns>An instantiated and configured migration</returns>
        protected virtual TMigrationBase CreateMigration(MigrationData migrationData)
        {
            if (migrationData == null)
                throw new ArgumentNullException(nameof(migrationData));

            TMigrationBase instance;
            try
            {
                instance = (TMigrationBase)Activator.CreateInstance(migrationData.TypeInfo.AsType());
            }
            catch (Exception e)
            {
                throw new MigrationException($"Unable to create migration {migrationData.FullName}", e);
            }

            return instance;
        }

        /// <summary>
        /// Helper structure containing a pair of <see cref="MigrationData"/>s, describing a step from one version to another
        /// </summary>
        protected struct MigrationDataPair
        {
            /// <summary>
            /// Gets the <see cref="MigrationData"/> this migration goes from
            /// </summary>
            public MigrationData From { get; }

            /// <summary>
            /// Gets the <see cref="MigrationData"/> this migration goes to
            /// </summary>
            public MigrationData To { get; }

            /// <summary>
            /// Initialises a new instance of the <see cref="MigrationDataPair"/> structure
            /// </summary>
            /// <param name="from"><see cref="MigrationData"/> this migration goes from</param>
            /// <param name="to"><see cref="MigrationData"/> this migration goes to</param>
            public MigrationDataPair(MigrationData from, MigrationData to)
            {
                this.From = from;
                this.To = to;
            }
        }
    }
}
