using NUnit.Framework;
using Simple.Migrations.IntegrationTests.Migrations;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Data.Common;

namespace Simple.Migrations.IntegrationTests
{
    public abstract class TestsBase
    {
        protected abstract IDbConnection CreateConnection();
        protected abstract IDatabaseProvider<IDbConnection> CreateDatabaseProvider();
        protected abstract IMigrationStringsProvider MigrationStringsProvider { get; }
        protected abstract void Clean();

        protected abstract bool SupportConcurrentMigrators { get; }

        [SetUp]
        public void SetUp()
        {
            this.Clean();
        }

        [Test]
        public void RunMigration()
        {
            var migrator = CreateMigrator(typeof(CreateUsersTable));
            migrator.MigrateToLatest();
        }

        [Test]
        public void RunsConcurrentMigrationsUp()
        {
            if (!this.SupportConcurrentMigrators)
                Assert.Ignore("Concurrent migrations not supported");

            Action<string> action = name =>
            {
                var migrator = CreateMigrator(name, typeof(CreateUsersTable));
                migrator.MigrateToLatest();
            };

            Task.WaitAll(
                Task.Run(() => action("migrator1")),
                Task.Run(() => action("migrator2"))
            );
        }

        [Test]
        public void RunsConcurrentMigrationsDown()
        {
            if (!this.SupportConcurrentMigrators)
                Assert.Ignore("Concurrent migrations not supported");

            var migrator = CreateMigrator("setup", typeof(CreateUsersTable));
            migrator.MigrateToLatest();

            Action<string> action = name =>
            {
                var thisMigrator = CreateMigrator(name, typeof(CreateUsersTable));
                thisMigrator.MigrateTo(0);
            };

            Task.WaitAll(
                Task.Run(() => action("migrator1")),
                Task.Run(() => action("migrator2"))
            );
        }

        protected SimpleMigrator CreateMigrator(params Type[] migrationTypes)
        {
            return this.CreateMigrator("migrator", migrationTypes);
        }

        protected SimpleMigrator CreateMigrator(string name, params Type[] migrationTypes)
        {
            foreach (var migrationType in migrationTypes)
            {
                var setupMethod = migrationType.GetMethod("Setup", BindingFlags.Static | BindingFlags.Public);
                setupMethod.Invoke(null, new object[] { this.MigrationStringsProvider });
            }

            var migrationProvider = new CustomMigrationProvider(migrationTypes);
            var migrator = new SimpleMigrator(migrationProvider, this.CreateDatabaseProvider(), new NUnitLogger(name));
            migrator.Load();
            return migrator;
        }
    }
}
