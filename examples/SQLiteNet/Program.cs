using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SimpleMigrations;
using SimpleMigrations.Console;
using SQLite;

namespace SQLiteNet
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var connection = new SQLiteConnection("SQLiteNetdatabase.sqlite"))
            {
                var databaseProvider = new SQLiteNetDatabaseProvider(connection);

                var migrator = new SimpleMigrator<SQLiteConnection, SQLiteNetMigration>(
                    Assembly.GetEntryAssembly(), databaseProvider);
                var runner = new ConsoleRunner(migrator);
                runner.Run(args);
            }
        }
    }
}
