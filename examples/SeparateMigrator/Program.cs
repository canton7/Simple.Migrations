using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SimpleMigrations.Console;
using SimpleMigrations.VersionProvider;

namespace SeparateMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            var db = new SQLiteConnection("DataSource=database.sqlite");
            var runner = new ConsoleRunner(Assembly.GetExecutingAssembly(), db, new SqliteVersionProvider());
            runner.Run(args);
        }
    }
}
