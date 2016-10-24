using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SimpleMigrations;
using SimpleMigrations.DatabaseProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests.Sqlite
{
    [TestFixture]
    public class SqliteTests
    {
        private SqliteConnection connection;
        private SimpleMigrator migrator;

        [SetUp]
        public void SetUp()
        {
            this.connection = new SqliteConnection(ConnectionStrings.SQLite);
            var migrationProvider = new CustomMigrationProvider(typeof(AddTable));
            this.migrator = new SimpleMigrator(migrationProvider, this.connection, new SqliteDatabaseProvider(), new NUnitLogger());

            this.migrator.Load();
        }

        [TearDown]
        public void TearDown()
        {
            this.migrator.MigrateTo(0);
            new SqliteCommand(@"DROP TABLE VersionInfo", this.connection).ExecuteNonQuery();
        }

        [Test]
        public void RunMigration()
        {
            this.migrator.MigrateToLatest();
        }
    }
}
