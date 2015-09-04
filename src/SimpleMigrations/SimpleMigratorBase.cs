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
        /// Gets or sets an action which is applied to each migration, after it is instantiated but before it is used
        /// </summary>
        public Action<TMigrationBase> Configurator { get; set; }

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

            this.Load();
        }

        private void Load()
        {
            this.versionProvider.EnsureCreated(this.connectionProvider.Connection);
            var currentVersion = this.versionProvider.GetCurrentVersion(this.connectionProvider.Connection);

            var migrations = (from type in this.migrationAssembly.GetTypes()
                             let attribute = type.GetCustomAttribute<MigrationAttribute>()
                             where attribute != null
                             where typeof(TMigrationBase).IsAssignableFrom(type)
                             orderby attribute.Version
                             select new MigrationData(attribute.Version, attribute.Description ?? type.Name, type, attribute.UseTransaction)).ToList();

            if (migrations.Any(x => x.Version <= 0))
                throw new Exception("Migrations must all have a version >= 0");

            var initialMigration = new MigrationData(0, "Empty Schema", null, false);
            this.Migrations = new[] { initialMigration }.Concat(migrations).ToList().AsReadOnly();

            this.CurrentMigration = this.Migrations.SingleOrDefault(x => x.Version == currentVersion);
            if (this.CurrentMigration == null)
                throw new Exception(String.Format("Unable to find migration with the current version: {0}", currentVersion));

            this.LatestMigration = this.Migrations.Last();
        }

        /// <summary>
        /// Migrate up to the latest version
        /// </summary>
        public void MigrateUp()
        {
            this.MigrateTo(this.LatestMigration.Version);
        }

        /// <summary>
        /// Migrate to a specific version
        /// </summary>
        /// <param name="newVersion">Version to migrate to</param>
        public void MigrateTo(long newVersion)
        {
            if (!this.Migrations.Any(x => x.Version == newVersion))
                throw new ArgumentException(String.Format("Could not find migration with version {0}", newVersion));

            List<MigrationData> migrations;
            bool up = newVersion > this.CurrentMigration.Version;
            if (up)
            {
                migrations = this.Migrations.Where(x => x.Version > this.CurrentMigration.Version && x.Version <= newVersion).OrderBy(x => x.Version).ToList();
            }
            else
            {
                migrations = this.Migrations.Where(x => x.Version < this.CurrentMigration.Version && x.Version >= newVersion).OrderByDescending(x => x.Version).ToList();
            }

            // Nothing to do?
            if (!migrations.Any())
                return;

            var oldMigration = this.CurrentMigration;
            var originalMigration = this.CurrentMigration;
            this.logger.BeginSequence(originalMigration, migrations.Last());
            try
            {
                foreach (var migrationData in migrations)
                {
                    this.MigrateStep(oldMigration, migrationData);
                    oldMigration = migrationData;
                }

                this.logger.EndSequence(originalMigration, this.CurrentMigration);
            }
            catch (Exception e)
            {
                this.logger.EndSequenceWithError(e, originalMigration, this.CurrentMigration);
                throw;
            }
        }

        private void MigrateStep(MigrationData oldMigrationData, MigrationData newMigrationData)
        {
            var up = newMigrationData.Version > oldMigrationData.Version;
            var useTransaction = up ? newMigrationData.UseTransaction : oldMigrationData.UseTransaction;

            try
            {
                if (useTransaction)
                    this.connectionProvider.BeginTransaction();

                if (up)
                {
                    this.logger.BeginUp(newMigrationData);
                    this.CreateMigration(newMigrationData).Up();
                    this.versionProvider.UpgradeVersion(this.connectionProvider.Connection, oldMigrationData.Version, newMigrationData.Version, newMigrationData.Description);
                    this.logger.EndUp(newMigrationData);
                }
                else
                {
                    this.logger.BeginDown(oldMigrationData);
                    this.CreateMigration(oldMigrationData).Down();
                    this.versionProvider.DowngradeVersion(this.connectionProvider.Connection, oldMigrationData.Version, newMigrationData.Version, newMigrationData.Description);
                    this.logger.EndDown(oldMigrationData);
                }

                if (useTransaction)
                    this.connectionProvider.CommitTransaction();
                this.CurrentMigration = newMigrationData;
            }
            catch (Exception e)
            {
                if (useTransaction)
                    this.connectionProvider.RollbackTransaction();

                if (up)
                    this.logger.EndUpWithError(e, newMigrationData);
                else
                    this.logger.EndDownWithError(e, oldMigrationData);

                throw;
            }
        }

        private IMigration<TDatabase> CreateMigration(MigrationData migrationData)
        {
            var instance = (TMigrationBase)Activator.CreateInstance(migrationData.Type);

            instance.Database = this.connectionProvider.Connection;
            instance.Logger = this.logger;

            if (this.Configurator != null)
                this.Configurator(instance);

            return instance;
        }
    }
}
