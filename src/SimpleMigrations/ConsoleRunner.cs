using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// Class which provides an easy way to run migrations from a console application
    /// </summary>
    public class ConsoleRunner
    {
        private readonly Assembly migrationsAssembly;
        private readonly IDbConnection database;
        private readonly IVersionProvider<IDbConnection> versionProvider;

        /// <summary>
        /// Instantiates a new instance of the <see cref="ConsoleLogger"/> class
        /// </summary>
        /// <param name="migrationsAssembly">Assembly to search for migrations in</param>
        /// <param name="database">Connection to the database to use</param>
        /// <param name="versionProvider">Version provider to use to fetch/set the current database version</param>
        public ConsoleRunner(Assembly migrationsAssembly, IDbConnection database, IVersionProvider<IDbConnection> versionProvider)
        {
            this.migrationsAssembly = migrationsAssembly;
            this.database = database;
            this.versionProvider = versionProvider;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage: {0} [up|to|reapply] [options]", Path.GetFileName(Assembly.GetExecutingAssembly().Location));
            Console.WriteLine("   up [options]: Migrate to the latest version");
            Console.WriteLine("   to <t> [options]: Migrate up/down to the specific version n");
            Console.WriteLine("   reapply [options]: Re-apply the current migration");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("   -v: Log SQL");
        }

        private static long ParseVersion(string input)
        {
            long migration;
            if (!Int64.TryParse(input, out migration) || migration < 1)
            {
                throw new Exception("Migrations must be positive numbers");
            }

            return migration;
        }

        /// <summary>
        /// Run the <see cref="ConsoleRunner"/> with the given command-line arguments
        /// </summary>
        /// <param name="args">Command-line arguments to use</param>
        public void Run(string[] args)
        {
            var argsList = args.ToList();
            bool verbose = argsList.Remove("-v");

            var logger = new ConsoleLogger()
            {
                EnableSqlLogging = verbose
            };

            var migrator = new SimpleMigrator(Assembly.GetExecutingAssembly(), this.database, this.versionProvider, logger);
            migrator.Load();

            var currentVersion = migrator.CurrentMigration.Version;

            try
            {
                if (argsList.Count == 0)
                {
                    migrator.MigrateUp();
                }
                else
                {
                    switch (argsList[0])
                    {
                        case "up":
                            if (argsList.Count > 1)
                                ShowHelp();
                            else
                                migrator.MigrateUp();
                            break;

                        case "to":
                            if (argsList.Count != 2)
                                ShowHelp();
                            else
                                migrator.MigrateTo(ParseVersion(args[1]));
                            break;

                        case "reapply":
                            if (argsList.Count > 1)
                            {
                                ShowHelp();
                            }
                            else
                            {
                                migrator.MigrateTo(currentVersion - 1);
                                migrator.MigrateTo(currentVersion);
                            }
                            break;

                        default:
                            ShowHelp();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("An error occurred: {0}", e.Message);
            }
        }
    }
}
