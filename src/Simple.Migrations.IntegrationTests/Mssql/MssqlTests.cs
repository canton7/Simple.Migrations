using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using SimpleMigrations.DatabaseProvider;
using System.Data;

namespace Simple.Migrations.IntegrationTests.Mssql
{
    [TestFixture]
    public class MssqlTests : TestsBase
    {
        protected override IMigrationStringsProvider MigrationStringsProvider { get; } = new MssqlStringsProvider();

        protected override IDbConnection CreateConnection() => new SqlConnection(ConnectionStrings.MSSQL);

        protected override IDatabaseProvider<IDbConnection> CreateDatabaseProvider() => new MssqlDatabaseProvider();

        protected override void Clean()
        {
            var connection = this.CreateConnection();
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"EXEC sp_MSforeachtable @command1 = ""DROP TABLE ?""";
                command.ExecuteNonQuery();
            }
        }
    }
}
