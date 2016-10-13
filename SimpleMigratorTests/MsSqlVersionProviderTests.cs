using System.Reflection;
using NUnit.Framework;
using SimpleMigrations;
using SimpleMigrations.Console;
using SimpleMigrations.VersionProvider;

namespace SimpleMigratorTests
{
    [TestFixture]
    public class MsSqlVersionProviderTests
    {
        [Test]
        public static void Verify_MsSqlMigration()
        {
            const string connectionString = @"Server=.\SQLEXPRESS;Database=SimpleMigratorTests;Trusted_Connection=True;";
            using (var connection = new System.Data.SqlClient.SqlConnection(connectionString) )
            {
                connection.Open();
                var migrationsAssembly = typeof(MsSqlVersionProviderTests).Assembly;
                var versionProvider = new MsSqlVersionProvider();
                var migrator = new SimpleMigrator(migrationsAssembly, connection, versionProvider);
                migrator.Load();

                migrator.MigrateToLatest();

            }
        }
    }
}
