using NUnit.Framework;
using SimpleMigrations;
using SimpleMigrations.VersionProvider;

namespace SimpleMigratorTests
{
    [TestFixture]
    public class SimpleMigratorTests
    {
        [Test]
        public static void Verify_SimpleMigratorTests()
        {
            const string connectionString = @"Server=localhost;Database=simplemigrations;Uid=root;Pwd=Whatever;";
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString) )
            {
                connection.Open();
                var migrationsAssembly = typeof(MsSqlVersionProviderTests).Assembly;
                var versionProvider = new MySqlVersionProvider();
                var migrator = new SimpleMigrator(migrationsAssembly, connection, versionProvider);
                migrator.Load();

                migrator.MigrateToLatest();

            }
        }
    }
}
