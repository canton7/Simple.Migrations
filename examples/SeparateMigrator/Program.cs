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
            var db = new SQLiteConnection("DataSource=database.sqlite");
            var migrator = new SimpleMigrator(Assembly.GetEntryAssembly(), db, new SqliteVersionProvider());
            var runner = new ConsoleRunner(migrator);
            runner.Run(args);
        }
    }
}
