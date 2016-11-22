using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using SimpleMigrations;
using NUnit.Framework;
using SimpleMigrations.DatabaseProvider;
using Npgsql;
using System.Data.Common;

namespace Simple.Migrations.IntegrationTests.Postgresql
{
    [TestFixture]
    public class PostgresqlTests : TestsBase
    {
        protected override IDatabaseProvider<DbConnection> CreateDatabaseProvider() => new PostgresqlDatabaseProvider();

        protected override IMigrationStringsProvider MigrationStringsProvider { get; } = new PostgresqlStringsProvider();

        protected override bool SupportConcurrentMigrators => true;

        protected override void Clean()
        {
            var connection = this.CreatePgConnection();
            connection.Open();

            using (var cmd = new NpgsqlCommand(@"drop schema public cascade", connection))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new NpgsqlCommand(@"create schema public authorization ""SimpleMigrator""", connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private NpgsqlConnection CreatePgConnection() => new NpgsqlConnection(ConnectionStrings.PostgreSQL);

        protected override DbConnection CreateConnection() => this.CreatePgConnection();
    }
}
