using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SimpleMigrations;
using SimpleMigrations.DatabaseProvider;

namespace Simple.Migrations.IntegrationTests.Mysql
{
    [TestFixture]
    public class MysqlTests
    {
        private MySqlConnection connection;
        private SimpleMigrator migrator;

        [SetUp]
        public void SetUp()
        {
            this.connection = new MySqlConnection(ConnectionStrings.MySQL);
            var migrationProvider = new CustomMigrationProvider(typeof(AddTable));
            this.migrator = new SimpleMigrator(migrationProvider, this.connection, new MysqlDatabaseProvider(), new NUnitLogger());

            this.migrator.Load();
        }

        [TearDown]
        public void TearDown()
        {
            this.migrator.MigrateTo(0);
            new MySqlCommand(@"DROP TABLE VersionInfo", this.connection).ExecuteNonQuery();
        }

        [Test]
        public void RunMigration()
        {
            this.migrator.MigrateToLatest();
        }
    }
}
