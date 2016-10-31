using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SimpleMigrations;
using SimpleMigrations.DatabaseProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;

namespace Simple.Migrations.IntegrationTests.Sqlite
{
    [TestFixture]
    public class SqliteTests
    {
        private static SimpleMigrator CreateMigrator(params Type[] migrationTypes)
        {
            var connection = new SqliteConnection(ConnectionStrings.SQLite);
            var migrationProvider = new CustomMigrationProvider(migrationTypes);
            var migrator = new SimpleMigrator(migrationProvider, connection, new SqliteDatabaseProvider(), new NUnitLogger());
            migrator.Load();
            return migrator;
        }

        [SetUp]
        public void SetUp()
        {
            var db = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ConnectionStrings.SQLiteDatabase);
            if (File.Exists(db))
                File.Delete(db);
        }

        [Test]
        public void RunMigration()
        {
            var migrator = CreateMigrator(typeof(AddTable));
            migrator.MigrateToLatest();
        }

        [Test]
        public void RunsConcurrentMigrations()
        {
           var m1 = CreateMigrator(typeof(BlockingMigration));
           var m2 = CreateMigrator(typeof(BlockingMigration));

            Task.WaitAll(
                Task.Run(() => m1.MigrateToLatest()),
                Task.Run(() => m2.MigrateToLatest())
            );
        }
    }
}
