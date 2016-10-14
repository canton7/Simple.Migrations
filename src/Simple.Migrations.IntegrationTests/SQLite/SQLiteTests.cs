using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SimpleMigrations;
using SimpleMigrations.VersionProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests.SQLite
{
    [TestFixture]
    public class SQLiteTests
    {
        private SimpleMigrator migrator;

        [SetUp]
        public void SetUp()
        {
            var connection = new SqliteConnection(ConnectionStrings.SQLite);
            this.migrator = new SimpleMigrator(typeof(SQLiteTests).Assembly, connection, new SQLiteVersionProvider());
            this.migrator.Load();
            this.migrator.MigrateTo(0);
        }

        [Test]
        public void RunMigration()
        {
            this.migrator.MigrateToLatest();
        }
    }
}
