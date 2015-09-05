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
        /// Gets or sets the subcommand to run if no subcommand is given. If null, help will be printed
        /// </summary>
        public SubCommand DefaultSubCommand { get; set; }

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

            this.CreateSubCommands();
        }

        /// <summary>
        /// Assign <see cref="SubCommands"/> (and optionally <see cref="DefaultSubCommand"/>)
        /// </summary>
        protected virtual void CreateSubCommands()
        {
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
                    Description = "to <n>: Migrate up/down to the version n",
                    Action = this.MigrateTo
                },
                new SubCommand()
                {
                    Command = "reapply",
                    Description = "reapply: Re-apply the current migration",
                    Action = this.ReApply
                },
                new SubCommand()
                {
                    Command = "list",
                    Description = "list: List all available migrations",
                    Action = this.ListMigrations
                },
            };

            this.DefaultSubCommand = this.SubCommands[0];
        }

        /// <summary>
        /// Show help
        /// </summary>
        public virtual void ShowHelp()
        {
            Console.WriteLine("Usage: {0} Subcommand [-q]", Path.GetFileName(Assembly.GetEntryAssembly().Location));
            Console.WriteLine();
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
            if (!Int64.TryParse(input, out migration) || migration < 0)
            {
                throw new Exception("Migrations must be positive numbers");
            }

            return migration;
        }

        /// <summary>
        /// Create a new <see cref="SimpleMigrator"/>
        /// </summary>
        /// <param name="args">Command-line args, which may be mutated</param>
        /// <returns>The instantiated migrator</returns>
        protected virtual SimpleMigrator CreateMigrator(List<string> args)
        {
            bool quiet = args.Remove("-q");

            var logger = new ConsoleLogger()
            {
                EnableSqlLogging = !quiet,
            };

            return new SimpleMigrator(this.migrationsAssembly, this.database, this.versionProvider, logger);
        }

        /// <summary>
        /// Run the <see cref="ConsoleRunner"/> with the given command-line arguments
        /// </summary>
        /// <param name="args">Command-line arguments to use</param>
        public virtual void Run(string[] args)
        {
            var argsList = args.ToList();

            var migrator = this.CreateMigrator(argsList);

            try
            {
                migrator.Load();

                if (argsList.Count == 0)
                {
                    if (this.DefaultSubCommand == null)
                        throw new HelpNeededException();
                    else
                        this.DefaultSubCommand.Action(migrator, argsList);
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

            if (migrator.CurrentMigration.Version == migrator.LatestMigration.Version)
                Console.WriteLine("Nothing to do");
            else
                migrator.MigrateUp();
        }

        private void MigrateTo(SimpleMigrator migrator, List<string> args)
        {
            if (args.Count != 1)
                throw new HelpNeededException();

            long version = ParseVersion(args[0]);
            if (version == migrator.CurrentMigration.Version)
                Console.WriteLine("Nothing to do");
            else
                migrator.MigrateTo(version);
        }

        private void ReApply(SimpleMigrator migrator, List<string> args)
        {
            if (args.Count != 0)
                throw new HelpNeededException();

            if (migrator.CurrentMigration == null)
                throw new Exception("No migrations have been applied");

            var currentVersion = migrator.CurrentMigration.Version;

            migrator.MigrateTo(currentVersion - 1);
            migrator.MigrateTo(currentVersion);
        }

        private void ListMigrations(SimpleMigrator migrator, List<string> args)
        {
            if (args.Count != 0)
                throw new HelpNeededException();

            Console.WriteLine("Available migrations:");

            foreach (var migration in migrator.Migrations)
            {
                var marker = (migration == migrator.CurrentMigration) ? "*" : " ";
                Console.WriteLine(" {0} {1}: {2}", marker, migration.Version, migration.Description);
            }
        }
    }
}
