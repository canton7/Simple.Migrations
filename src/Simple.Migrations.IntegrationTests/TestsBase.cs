using NUnit.Framework;
using Simple.Migrations.IntegrationTests.Migrations;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests
{
    public abstract class TestsBase
    {
        protected abstract IDbConnection CreateConnection();
        protected abstract IDatabaseProvider<IDbConnection> CreateDatabaseProvider();
        protected abstract IMigrationStringsProvider MigrationStringsProvider { get; }
        protected abstract void Clean();

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
        public void RunsConcurrentMigrations()
        {
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

            var connection = this.CreateConnection();
            var migrationProvider = new CustomMigrationProvider(migrationTypes);
            var migrator = new SimpleMigrator(migrationProvider, connection, this.CreateDatabaseProvider(), new NUnitLogger(name));
            migrator.Load();
            return migrator;
        }
    }
}
