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
        private SimpleMigrator migrator;

        [SetUp]
        public void SetUp()
        {
            var connection = new SqlConnection(ConnectionStrings.MSSQL);
            var migrationProvider = new CustomMigrationProvider(typeof(AddTable));
            this.migrator = new SimpleMigrator(migrationProvider, connection, new MSSQLVersionProvider(), new NUnitLogger());

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
