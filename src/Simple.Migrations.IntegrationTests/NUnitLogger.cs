using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests
{
    public class NUnitLogger : ILogger
    {
        private readonly string name;

        public NUnitLogger(string name)
        {
            this.name = name;
        }

        public bool EnableSqlLogging { get; set; } = true;

        /// <summary>
        /// Invoked when a sequence of migrations is started
        /// </summary>
        /// <param name="from">Migration being migrated from</param>
        /// <param name="to">Migration being migrated to</param>
        public void BeginSequence(MigrationData from, MigrationData to)
        {
            this.WriteHeader($"{this.name}: Migrating from {from.Version}: {from.FullName} to {to.Version}: {to.FullName}");
        }

        /// <summary>
        /// Invoked when a sequence of migrations is completed successfully
        /// </summary>
        /// <param name="from">Migration which was migrated from</param>
        /// <param name="to">Migration which was migrated to</param>
        public void EndSequence(MigrationData from, MigrationData to)
        {
            this.WriteHeader($"{this.name}: Done");
        }

        /// <summary>
        /// Invoked when a sequence of migrations fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="from">Migration which was migrated from</param>
        /// <param name="currentVersion">Last successful migration which was applied</param>
        public void EndSequenceWithError(Exception exception, MigrationData from, MigrationData currentVersion)
        {
            TestContext.WriteLine($"{this.name}:");
            TestContext.WriteLine($"{this.name}: ERROR: {exception.Message}. Database is currently on version {currentVersion.Version}: {currentVersion.FullName}");
        }

        /// <summary>
        /// Invoked when the migrator skips a migration, because it was already applied, probably by a migrator running in parallel
        /// </summary>
        /// <param name="migration">Migration which was skipped</param>
        /// <param name="direction">Direction of the migration</param>
        public void SkipMigrationBecauseAlreadyApplied(MigrationData migration, MigrationDirection direction)
        {
            this.WriteWarning($"{this.name}: {migration.Version}: {migration.FullName} SKIPPED as it has already been applied, probably by a migrator running in parallel");
        }

        /// <summary>
        /// Invoked when an individual migration is started
        /// </summary>
        /// <param name="migration">Migration being started</param>
        /// <param name="direction">Direction of the migration</param>
        public void BeginMigration(MigrationData migration, MigrationDirection direction)
        {
            var term = direction == MigrationDirection.Up ? "migrating" : "reverting";
            this.WriteHeader($"{this.name}: {migration.Version}: {migration.FullName} {term}");
        }

        /// <summary>
        /// Invoked when an individual migration is completed successfully
        /// </summary>
        /// <param name="migration">Migration which completed</param>
        /// <param name="direction">Direction of the migration</param>
        public void EndMigration(MigrationData migration, MigrationDirection direction)
        {
            TestContext.WriteLine($"{this.name}:");
        }

        /// <summary>
        /// Invoked when an individual migration fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="migration">Migration which failed</param>
        /// <param name="direction">Direction of the migration</param>
        public void EndMigrationWithError(Exception exception, MigrationData migration, MigrationDirection direction)
        {
            this.WriteError($"{this.name}: {migration.Version}: {migration.FullName} ERROR {exception.Message}");
        }

        /// <summary>
        /// Invoked when the migrator ran a migration outside of a transaction and then attempted to update the schema version,
        /// but found that it had already been updated, probably by a migrator running in parallel
        /// </summary>
        /// <param name="migration">Migration which was run, but the version
        public void EndMigrationWithSkippedVersionTableUpdate(MigrationData migration, MigrationDirection direction)
        {
            this.WriteWarning($"{this.name}: {migration.Version}: {migration.FullName} PARTIAL Migration was applied outside of a transaction, but another migrator " +
                "running in parallel updated the version table at the same time, so it was not updated after this migration.");
        }

        /// <summary>
        /// Invoked when another informative message should be logged
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public void Info(string message)
        {
            TestContext.WriteLine($"{this.name}: {message}");
        }

        /// <summary>
        /// Invoked when SQL being executed should be logged
        /// </summary>
        /// <param name="message">SQL to log</param>
        public void LogSql(string message)
        {
            if (this.EnableSqlLogging)
                TestContext.WriteLine($"{this.name}: {message}");
        }

        private void WriteHeader(string message)
        {
            TestContext.WriteLine($"{this.name}: {new String('-', 20)}");
            TestContext.WriteLine($"{this.name}: {message}");
            TestContext.WriteLine($"{this.name}: {new String('-', 20)}");
            TestContext.WriteLine($"{this.name}:");
        }

        private void WriteError(string message)
        {
            TestContext.WriteLine($"{this.name}:");
            TestContext.WriteLine($"{this.name}: {new String('-', 20)}");
            TestContext.WriteLine($"{this.name}: {message}");
            TestContext.WriteLine($"{this.name}: {new String('-', 20)}");
        }

        private void WriteWarning(string message)
        {
            Console.WriteLine($"{this.name}: {new String('-', Console.WindowWidth - 1)}");
            Console.WriteLine($"{this.name}: {message}");
            Console.WriteLine($"{this.name}: {new String('-', Console.WindowWidth - 1)}");
        }
    }
}
