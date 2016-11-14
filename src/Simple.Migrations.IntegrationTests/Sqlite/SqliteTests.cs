using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleMigrations;
using System.Data;
using SimpleMigrations.DatabaseProvider;
using System.IO;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Simple.Migrations.IntegrationTests.Sqlite
{
    [TestFixture]
    public class SqliteTests : TestsBase
    {
        protected override IDatabaseProvider<IDbConnection> DatabaseProvider => new SqliteDatabaseProvider();

        protected override IMigrationStringsProvider MigrationStringsProvider { get; } = new SqliteStringsProvider();

        protected override void Clean()
        {
            var db = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ConnectionStrings.SQLiteDatabase);
            if (File.Exists(db))
                File.Delete(db);
        }

        protected override IDbConnection CreateConnection() => new SqliteConnection(ConnectionStrings.SQLite);
    }
}
