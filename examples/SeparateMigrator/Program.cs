using System.Data.SQLite;
using System.Reflection;
using SimpleMigrations;
using SimpleMigrations.Console;
using SimpleMigrations.DatabaseProvider;

namespace SeparateMigrator
{
    class Program
    {
        static int Main(string[] args)
        {
            var migrationsAssembly = typeof(Program).Assembly;
            using (var connection = new SQLiteConnection("DataSource=database.sqlite"))
            {
                var databaseProvider = new SqliteDatabaseProvider(connection);

                var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);

                var runner = new ConsoleRunner(migrator);
                return runner.Run(args);
            }
        }
    }
}
