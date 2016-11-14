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
        protected abstract IDatabaseProvider<IDbConnection> DatabaseProvider { get; }
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
            var m1 = CreateMigrator("migrator1", typeof(CreateUsersTable));
            var m2 = CreateMigrator("migrator2", typeof(CreateUsersTable));

            Task.WaitAll(
                Task.Run(() => m1.MigrateToLatest()),
                Task.Run(() => m2.MigrateToLatest())
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
            var migrator = new SimpleMigrator(migrationProvider, connection, this.DatabaseProvider, new NUnitLogger(name));
            migrator.Load();
            return migrator;
        }
    }
}
