using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SimpleMigrations;
using SimpleMigrations.DatabaseProvider;

namespace Simple.Migrations.IntegrationTests.Mysql
{
    [TestFixture]
    public class MysqlTests : TestsBase
    {
        protected override IMigrationStringsProvider MigrationStringsProvider { get; } = new MysqlStringsProvider();

        protected override DbConnection CreateConnection() => new MySqlConnection(ConnectionStrings.MySQL);

        protected override IDatabaseProvider<DbConnection> CreateDatabaseProvider() => new MysqlDatabaseProvider(this.CreateConnection());

        protected override bool SupportConcurrentMigrators => true;

        protected override void Clean()
        {

        }
    }
}
