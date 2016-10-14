using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using SimpleMigrations.VersionProvider;

namespace Simple.Migrations.IntegrationTests.MSSQL
{
    [TestFixture]
    public class MSSQLTests
    {
        private SqlConnection connection;
        private SimpleMigrator migrator;

        [SetUp]
        public void SetUp()
        {
            this.connection = new SqlConnection(ConnectionStrings.MSSQL);
            var migrationProvider = new CustomMigrationProvider(typeof(AddTable));
            this.migrator = new SimpleMigrator(migrationProvider, this.connection, new MSSQLVersionProvider(), new NUnitLogger());

            this.migrator.Load();
        }

        [TearDown]
        public void TearDown()
        {
            this.migrator.MigrateTo(0);
            new SqlCommand(@"DROP TABLE VersionInfo", this.connection).ExecuteNonQuery();
        }

        [Test]
        public void RunMigration()
        {
            this.migrator.MigrateToLatest();
        }
    }
}
