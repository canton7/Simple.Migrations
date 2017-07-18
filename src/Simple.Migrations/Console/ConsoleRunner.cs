using System.Linq;
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
        private readonly ISimpleMigrator migrator;

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
        /// <param name="migrator">Migrator to use</param>
        public ConsoleRunner(ISimpleMigrator migrator)
        {
            this.migrator = migrator;

            // If they haven't assigned a logger, do so now
            if (this.migrator.Logger == null)
                this.migrator.Logger = new ConsoleLogger();

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
                new SubCommand()
                {
                    Command = "baseline",
                    Description = "baseline <n>: Move the database to version n, without apply migrations",
                    Action = this.Baseline
                },
            };

            this.DefaultSubCommand = this.SubCommands[0];
        }

        /// <summary>
        /// Show help
        /// </summary>
        public virtual void ShowHelp()
        {
            Console.WriteLine("Usage: <executable> Subcommand [-q]");
            Console.WriteLine();
            Console.WriteLine("Subcommand can be one of:");
            foreach (var subCommand in this.SubCommands)
            {
                Console.WriteLine($"   {subCommand.Description}");
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
        /// Run the <see cref="ConsoleRunner"/> with the given command-line arguments
        /// </summary>
        /// <param name="args">Command-line arguments to use</param>
        public virtual void Run(string[] args)
        {
            var argsList = args.ToList();

            void WriteError(string message)
            {
                var foregroundColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(message);
                Console.ForegroundColor = foregroundColor;
            }

            try
            {
                this.migrator.Load();

                if (argsList.Count == 0)
                {
                    if (this.DefaultSubCommand == null)
                        throw new HelpNeededException();
                    else
                        this.DefaultSubCommand.Action(argsList);
                }
                else
                {
                    var subCommandType = argsList[0];
                    argsList.RemoveAt(0);

                    var subCommand = this.SubCommands.FirstOrDefault(x => x.Command == subCommandType);
                    if (subCommand == null)
                        throw new HelpNeededException();

                    subCommand.Action(argsList);
                }
            }
            catch (HelpNeededException)
            {
                this.ShowHelp();
            }
            catch (MigrationNotFoundException e)
            {
                WriteError($"Error: could not find migration with version {e.Version}");
            }
            catch (MigrationException e)
            {
                WriteError($"Error: {e.Message}");
            }
            catch (Exception e)
            {
                WriteError("An unhandled exception occurred:");
                WriteError(e.ToString());
            }
        }

        private void MigrateUp(List<string> args)
        {
            if (args.Count != 0)
                throw new HelpNeededException();

            if (this.migrator.CurrentMigration.Version == this.migrator.LatestMigration.Version)
                Console.WriteLine("Nothing to do");
            else
                this.migrator.MigrateToLatest();
        }

        private void MigrateTo(List<string> args)
        {
            if (args.Count != 1)
                throw new HelpNeededException();

            long version = ParseVersion(args[0]);
            if (version == this.migrator.CurrentMigration.Version)
                Console.WriteLine("Nothing to do");
            else
                this.migrator.MigrateTo(version);
        }

        private void ReApply(List<string> args)
        {
            if (args.Count != 0)
                throw new HelpNeededException();

            var currentVersion = this.migrator.CurrentMigration.Version;
            var previousVersion = this.migrator.Migrations.TakeWhile(x => x.Version < currentVersion).LastOrDefault();

            if (previousVersion == null)
                throw new MigrationException($"The current version {currentVersion} is the lowest, and so cannot be reapplied");

            this.migrator.MigrateTo(previousVersion.Version);
            this.migrator.MigrateTo(currentVersion);
        }

        private void ListMigrations(List<string> args)
        {
            if (args.Count != 0)
                throw new HelpNeededException();

            Console.WriteLine("Available migrations:");

            foreach (var migration in this.migrator.Migrations)
            {
                var marker = (migration == this.migrator.CurrentMigration) ? "*" : " ";
                Console.WriteLine($" {marker} {migration.Version}: {migration.FullName}");
            }
        }

        private void Baseline(List<string> args)
        {
            if (args.Count != 1)
                throw new HelpNeededException();

            long version = ParseVersion(args[0]);

            this.migrator.Baseline(version);
            Console.WriteLine($"Database versioned at version {version}");
        }
    }
}
