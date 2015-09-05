using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace SimpleMigrations.Console
{
    // Avoid pain with Console.XXX being assumed to be in SimpleMigrations.Console
    using System;

    /// <summary>
    /// Class which provides an easy way to run migrations from a console application
    /// </summary>
    public class ConsoleRunner
    {
        private readonly Assembly migrationsAssembly;
        private readonly IDbConnection database;
        private readonly IVersionProvider<IDbConnection> versionProvider;

        /// <summary>
        /// Gets a list of subcommands which are supported
        /// </summary>
        public List<SubCommand> SubCommands { get; private set; }

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

            this.SubCommands = new List<SubCommand>()
            {
                new SubCommand()
                {
                    Command = "up",
                    Description = "up: Migrate to the latest version",
                    Action = this.MigrateUp,
                },
                new SubCommand()
                {
                    Command = "to",
                    Description = "to <n>: Migrate up/down to the latest version n",
                    Action = this.MigrateTo
                },
                new SubCommand()
                {
                    Command = "reapply",
                    Description = "reapply: Re-apply the current migration",
                    Action = this.ReApply
                }
            };
        }

        /// <summary>
        /// Show help
        /// </summary>
        public void ShowHelp()
        {
            Console.WriteLine("Usage: {0} Subcommand [-v]", Path.GetFileName(Assembly.GetExecutingAssembly().Location));
            Console.WriteLine("Subcommand can be one of:");
            foreach (var subCommand in this.SubCommands)
            {
                Console.WriteLine("   {0}", subCommand.Description);
            }
        }

        /// <summary>
        /// Parse a migration version from a string
        /// </summary>
        /// <param name="input">String to parse</param>
        /// <returns>Parsed migration version</returns>
        public static long ParseVersion(string input)
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
                    this.SubCommands[0].Action(migrator, argsList);
                }
                else
                {
                    var subCommandType = argsList[0];
                    argsList.RemoveAt(0);

                    var subCommand = this.SubCommands.FirstOrDefault(x => x.Command == subCommandType);
                    if (subCommand == null)
                        throw new HelpNeededException();

                    subCommand.Action(migrator, argsList);
                }
            }
            catch (HelpNeededException)
            {
                this.ShowHelp();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("An error occurred: {0}", e.Message);
            }
        }

        private void MigrateUp(SimpleMigrator migrator, List<string> args)
        {
            if (args.Count != 0)
                throw new HelpNeededException();

            migrator.MigrateUp();
        }

        private void MigrateTo(SimpleMigrator migrator, List<string> args)
        {
            if (args.Count != 1)
                throw new HelpNeededException();

            migrator.MigrateTo(ParseVersion(args[0]));
        }

        private void ReApply(SimpleMigrator migrator, List<string> args)
        {
            if (args.Count != 1)
                throw new HelpNeededException();

            var version = ParseVersion(args[0]);
            if (version < 1)
                throw new Exception("Version must be > 0");

            migrator.MigrateTo(version - 1);
            migrator.MigrateTo(version);
        }
    }
}
