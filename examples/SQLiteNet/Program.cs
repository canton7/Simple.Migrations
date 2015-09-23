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
            var connection = new SQLiteNetConnectionProvider(new SQLiteConnection("SQLiteNetdatabase.sqlite"));
            var versionProvider = new SQLiteNetVersionProvider();

            var migrator = new SimpleMigrator<SQLiteConnection, SQLiteNetMigration>(
                Assembly.GetEntryAssembly(), connection, versionProvider);
            var runner = new ConsoleRunner(migrator);
            runner.Run(args);
        }
    }
}
