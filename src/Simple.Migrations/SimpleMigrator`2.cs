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
        where TConnection : IDisposable
    {
        /// <summary>
        /// Assembly to search for migrations
        /// </summary>
        protected IMigrationProvider MigrationProvider { get; }

        /// <summary>
        /// Connection provider
        /// </summary>
        protected Func<TConnection> ConnectionFactory { get; }

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
        public MigrationData LatestMigration { get; private set; }

        /// <summary>
        /// Gets all available migrations
        /// </summary>
        public IReadOnlyList<MigrationData> Migrations { get; private set; }

        private bool isLoaded;

        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator{TDatabase, TMigrationBase}"/> class
        /// </summary>
        /// <param name="migrationProvider">Migration provider to use to find migration classes</param>
        /// <param name="connectionProvider">Connection provider to use to communicate with the database</param>
        /// <param name="databaseProvider">Database provider to use to interact with the version table, etc</param>
        /// <param name="logger">Logger to use to log progress and messages</param>
        public SimpleMigrator(
            IMigrationProvider migrationProvider,
            Func<TConnection> connectionFactory,
            IDatabaseProvider<TConnection> databaseProvider,
            ILogger logger = null)
        {
            if (migrationProvider == null)
                throw new ArgumentNullException(nameof(migrationProvider));
            if (connectionFactory == null)
                throw new ArgumentNullException(nameof(connectionFactory));
            if (databaseProvider == null)
                throw new ArgumentNullException(nameof(databaseProvider));

            this.MigrationProvider = migrationProvider;
            this.ConnectionFactory = connectionFactory;
            this.DatabaseProvider = databaseProvider;
            this.Logger = logger;
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SimpleMigrator{TDatabase, TMigrationBase}"/> class
        /// </summary>
        /// <param name="migrationsAssembly">Assembly to search for migrations</param>
        /// <param name="connectionProvider">Connection provider to use to communicate with the database</param>
        /// <param name="databaseProvider">Database provider to use to interact with the version table, etc</param>
        /// <param name="logger">Logger to use to log progress and messages</param>
        public SimpleMigrator(
            Assembly migrationsAssembly,
            Func<TConnection> connectionFactory,
            IDatabaseProvider<TConnection> databaseProvider,
            ILogger logger = null)
            : this(new AssemblyMigrationProvider(migrationsAssembly), connectionFactory, databaseProvider, logger)
        {
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

            long currentVersion;
            using (var connection = this.ConnectionFactory())
            {
                currentVersion = this.DatabaseProvider.EnsureCreatedAndGetCurrentVersion(connection);
            }

            this.FindAndSetMigrations();
            this.SetCurrentVersion(currentVersion);
            this.LatestMigration = this.Migrations.Last();

            this.isLoaded = true;
        }

        /// <summary>
        /// Load the migrations, and set <see cref="Migrations"/>
        /// </summary>
        protected virtual void FindAndSetMigrations()
        {
            var migrations = this.MigrationProvider.LoadMigrations()?.ToList();

            if (migrations == null || migrations.Count == 0)
                throw new MigrationException("The configured MigrationProvider did not find any migrations");

            var migrationBaseTypeInfo = typeof(TMigrationBase).GetTypeInfo();
            var firstNotImplementingTMigrationBase = migrations.FirstOrDefault(x => !migrationBaseTypeInfo.IsAssignableFrom(x.TypeInfo));
            if (firstNotImplementingTMigrationBase != null)
                throw new MigrationException($"Migration {firstNotImplementingTMigrationBase.FullName} must derive from / implement {typeof(TMigrationBase).Name}");

            var firstWithInvalidVersion = migrations.FirstOrDefault(x => x.Version <= 0);
            if (firstWithInvalidVersion != null)
                throw new MigrationException($"Migration {firstWithInvalidVersion.FullName} must have a version > 0");

            var versionLookup = migrations.ToLookup(x => x.Version);
            var firstDuplicate = versionLookup.FirstOrDefault(x => x.Count() > 1);
            if (firstDuplicate != null)
                throw new MigrationException($"Found more than one migration with version {firstDuplicate.Key} ({String.Join(", ", firstDuplicate)})");

            this.Migrations = new[] { MigrationData.EmptySchema }.Concat(migrations.OrderBy(x => x.Version)).ToList();
        }

        /// <summary>
        /// Set this.CurrentMigration, by inspecting the database
        /// </summary>
        protected virtual void SetCurrentVersion(long currentVersion)
        {
            var currentMigration = this.Migrations.FirstOrDefault(x => x.Version == currentVersion);
            if (currentMigration == null)
                throw new MigrationException($"Unable to find migration with the current version: {currentVersion}");

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

            var toMigration = this.Migrations.FirstOrDefault(x => x.Version == newVersion);
            if (toMigration == null)
                throw new ArgumentException($"Could not find migration with version {newVersion}", nameof(newVersion));

            var direction = newVersion > this.CurrentMigration.Version ? MigrationDirection.Up : MigrationDirection.Down;
            var originalMigration = this.CurrentMigration;

            var migrations = this.FindMigrationsToRun(newVersion, direction);

            this.Logger?.BeginSequence(originalMigration, toMigration);

            //// This is the last migration which was run (that we know about)
            //var lastMigrationData = this.CurrentMigration;

            using (var versionConnection = this.ConnectionFactory())
            using (var updatesConnection = this.ConnectionFactory())
            {
                try
                {
                    this.DatabaseProvider.AcquireDatabaseLock(updatesConnection);

                    foreach (var migrationDataPair in migrations)
                    {
                        var migrationDataToRun = (direction == MigrationDirection.Up) ? migrationDataPair.To : migrationDataPair.From;

                        try
                        {
                            this.Logger?.BeginMigration(migrationDataToRun, direction);

                            this.RunMigration(direction, migrationDataToRun, updatesConnection);
                            this.DatabaseProvider.UpdateVersion(versionConnection, migrationDataPair.From.Version, migrationDataPair.To.Version, migrationDataPair.To.FullName);

                            this.Logger?.EndMigration(migrationDataToRun, direction);
                        }
                        catch (Exception e)
                        {
                            this.Logger?.EndMigrationWithError(e, migrationDataToRun, direction);
                            throw;
                        }
                    }
                }
                finally
                {
                    long currentVersion = this.DatabaseProvider.GetCurrentVersion(versionConnection);
                    this.SetCurrentVersion(currentVersion);

                    this.DatabaseProvider.ReleaseDatabaseLock(updatesConnection);
                }
            }

            //try
            //{
            //    this.DatabaseProvider.AcquireDatabaseLock();

            //    foreach (var migrationDataPair in migrations)
            //    {
            //        var migrationDataToRun = (direction == MigrationDirection.Up) ? migrationDataPair.To : migrationDataPair.From;

            //        try
            //        {
            //            this.ConnectionProvider.BeginAndRecordTransaction();

            //            var currentVersion = this.DatabaseProvider.GetCurrentVersion();

            //            if (this.ShouldSkipMigrationOrThrowIfConflictingMigrators(direction, migrationDataPair, currentVersion))
            //                continue;

            //            try
            //            {
            //                this.Logger?.BeginMigration(migrationDataToRun, direction);

            //                // If the migration doesn't want a transaction, complete the current one
            //                if (!migrationDataToRun.UseTransaction)
            //                    this.ConnectionProvider.CommitRecordedTransaction();

            //                this.RunMigration(direction, migrationDataToRun);

            //                // If we're in a transaction, we can just update the version table and commit.
            //                // If we're not, we have to open a new one, and do a read-modify-write
            //                if (migrationDataToRun.UseTransaction)
            //                {
            //                    this.DatabaseProvider.UpdateVersion(currentVersion, migrationDataPair.To.Version, migrationDataPair.To.FullName);
            //                    this.ConnectionProvider.CommitRecordedTransaction();

            //                    this.Logger?.EndMigration(migrationDataToRun, direction);
            //                }
            //                else
            //                {
            //                    this.ConnectionProvider.BeginAndRecordTransaction();

            //                    var newCurrentVersion = this.DatabaseProvider.GetCurrentVersion();

            //                    // newCurrentVersion should be == currentVersion. If it's gone in the opposite direction to the migration
            //                    // direction, then that's an error. If it's gone in the same direction, log a warning but skip it

            //                    if (newCurrentVersion == currentVersion)
            //                    {
            //                        this.DatabaseProvider.UpdateVersion(currentVersion, migrationDataPair.To.Version, migrationDataPair.To.FullName);
            //                        this.ConnectionProvider.CommitRecordedTransaction();

            //                        this.Logger?.EndMigration(migrationDataToRun, direction);
            //                    }
            //                    else
            //                    {
            //                        this.ConnectionProvider.RollbackTransaction();

            //                        if (direction == MigrationDirection.Up)
            //                        {
            //                            if (newCurrentVersion > currentVersion)
            //                                this.Logger?.EndMigrationWithSkippedVersionTableUpdate(migrationDataToRun, direction);
            //                            else
            //                                throw new ConflictingMigratorsException(migrationDataToRun, currentVersion, newCurrentVersion);
            //                        }
            //                        else
            //                        {
            //                            if (newCurrentVersion < currentVersion)
            //                                this.Logger?.EndMigrationWithSkippedVersionTableUpdate(migrationDataToRun, direction);
            //                            else
            //                                throw new ConflictingMigratorsException(migrationDataToRun, currentVersion, newCurrentVersion);
            //                        }
            //                    }
            //                }
            //            }
            //            catch (Exception e)
            //            {
            //                this.Logger?.EndMigrationWithError(e, migrationDataToRun, direction);
            //                throw;
            //            }
            //        }
            //        finally
            //        {
            //            // Finally block for individual migrations: make sure transactions are sorted out

            //            if (this.ConnectionProvider.HasOpenTransaction)
            //                this.ConnectionProvider.RollbackTransaction();
            //        }
            //    }

            //    // Once all migrations are complete, tidy up
            //    this.SetCurrentVersion();
            //    this.Logger?.EndSequence(originalMigration, this.CurrentMigration);
            //}
            //catch (Exception e)
            //{
            //    // If the whole sequence failed somewhere, try and tidy up, and log an error

            //    try
            //    {
            //        this.SetCurrentVersion();
            //    }
            //    catch { }

            //    this.Logger?.EndSequenceWithError(e, originalMigration, this.CurrentMigration);
            //    throw;
            //}
            //finally
            //{
            //    this.DatabaseProvider.ReleaseDatabaseLock();
            //}
        }

        protected virtual void RunMigration(MigrationDirection direction, MigrationData migrationData, TConnection connection)
        {
            var migration = this.CreateMigration(migrationData);
            migration.Execute(connection, this.Logger ?? NullLogger.Instance, direction);
        }

        //protected virtual bool ShouldSkipMigrationOrThrowIfConflictingMigrators(MigrationDirection direction, MigrationDataPair migrationDataPair, long currentVersion)
        //{
        //    // If the database is already at this migration (or further on), skip it.
        //    // If the database has gone in the opposite direction, abort with an error

        //    bool shouldSkip = false;

        //    if (direction == MigrationDirection.Up)
        //    {
        //        // currentVersion should == from version
        //        if (currentVersion < migrationDataPair.From.Version)
        //        {
        //            throw new ConflictingMigratorsException(migrationDataPair.To, migrationDataPair.From.Version, currentVersion);
        //        }
        //        else if (currentVersion > migrationDataPair.From.Version)
        //        {
        //            this.Logger?.SkipMigrationBecauseAlreadyApplied(migrationDataPair.To, direction);
        //            shouldSkip = true;
        //        }
        //    }
        //    else
        //    {
        //        // currentVersion should == to version
        //        if (currentVersion > migrationDataPair.From.Version)
        //        {
        //            throw new ConflictingMigratorsException(migrationDataPair.From, migrationDataPair.From.Version, currentVersion);
        //        }
        //        else if (currentVersion < migrationDataPair.From.Version)
        //        {
        //            this.Logger?.SkipMigrationBecauseAlreadyApplied(migrationDataPair.To, direction);
        //            shouldSkip = true;
        //        }
        //    }

        //    return shouldSkip;
        //}

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
                throw new ArgumentException($"Could not find migration with version {version}", nameof(version));

            using (var connection = this.ConnectionFactory())
            {
                try
                {
                    this.DatabaseProvider.AcquireDatabaseLock(connection);

                    this.DatabaseProvider.UpdateVersion(connection, 0, version, migration.FullName);
                }
                finally
                {
                    this.DatabaseProvider.ReleaseDatabaseLock(connection);
                }
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

        protected struct MigrationDataPair
        {
            public MigrationData From { get; }
            public MigrationData To { get; }

            public MigrationDataPair(MigrationData from, MigrationData to)
            {
                this.From = from;
                this.To = to;
            }
        }
    }
}
