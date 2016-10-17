using System.Data.SQLite;
using System.Reflection;
using SimpleMigrations;
using SimpleMigrations.Console;
using SimpleMigrations.VersionProvider;

namespace SeparateMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            var migrationsAssembly = typeof(Program).Assembly;
            var db = new SQLiteConnection("DataSource=database.sqlite");
            var versionProvider = new SqliteVersionProvider();

            var migrator = new SimpleMigrator(migrationsAssembly, db, versionProvider);

            var runner = new ConsoleRunner(migrator);
            runner.Run(args);
        }
    }
}
