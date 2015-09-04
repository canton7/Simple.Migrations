using System;

namespace SimpleMigrations
{
    /// <summary>
    /// <see cref="ILogger"/> implementation which logs to the console
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private ConsoleColor foregroundColor;

        /// <summary>
        /// Gets or sets a value indicating whether SQL should be logged
        /// </summary>
        public bool EnableSqlLogging { get; set; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="ConsoleLogger"/> class
        /// </summary>
        public ConsoleLogger()
        {
            this.EnableSqlLogging = true;
        }

        /// <summary>
        /// Invoked when a sequence of migrations is started
        /// </summary>
        /// <param name="from">Migration being migrated from</param>
        /// <param name="to">Migration being migrated to</param>
        public void BeginSequence(MigrationData from, MigrationData to)
        {
            this.foregroundColor = Console.ForegroundColor;

            this.WriteHeader("Migrating from {0}: {1} to {2}: {3}", from.Version, from.Description, to.Version, to.Description);
        }

        /// <summary>
        /// Invoked when a sequence of migrations is completed successfully
        /// </summary>
        /// <param name="from">Migration which was migrated from</param>
        /// <param name="to">Migration which was migrated to</param>
        public void EndSequence(MigrationData from, MigrationData to)
        {
            this.WriteHeader("Done");
        }

        /// <summary>
        /// Invoked when a sequence of migrations fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="from">Migration which was migrated from</param>
        /// <param name="currentVersion">Last successful migration which was applied</param>
        public void EndSequenceWithError(Exception exception, MigrationData from, MigrationData currentVersion)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Database is curently on version {0}: {1}", currentVersion.Version, currentVersion.Description);
            Console.ForegroundColor = this.foregroundColor;
        }

        /// <summary>
        /// Invoked when an individual "up" migration is started
        /// </summary>
        /// <param name="migration">Migration being started</param>
        public void BeginUp(MigrationData migration)
        {
            this.WriteHeader("{0}: {1} migrating", migration.Version, migration.Description);
        }

        /// <summary>
        /// Invoked when an individual "up" migration is completed successfully
        /// </summary>
        /// <param name="migration">Migration which completed</param>
        public void EndUp(MigrationData migration)
        {
            Console.WriteLine();
        }

        /// <summary>
        /// Invoked when an individual "up" migration fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="migration">Migration which failed</param>
        public void EndUpWithError(Exception exception, MigrationData migration)
        {
            this.WriteError("{0}: {1} ERROR {2}", migration.Version, migration.Description, exception.Message);
        }

        /// <summary>
        /// Invoked when an individual "down" migration is started
        /// </summary>
        /// <param name="migration">Migration being started</param>
        public void BeginDown(MigrationData migration)
        {
            this.WriteHeader("{0}: {1} reverting", migration.Version, migration.Description);
        }

        /// <summary>
        /// Invoked when an individual "down" migration is completed successfully
        /// </summary>
        /// <param name="migration">Migration which completed</param>
        public void EndDown(MigrationData migration)
        {
            Console.WriteLine();
        }

        /// <summary>
        /// Invoked when an individual "down" migration fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="migration">Migration which failed</param>
        public void EndDownWithError(Exception exception, MigrationData migration)
        {
            this.WriteError("{0}: {1} ERROR {2}", migration.Version, migration.Description, exception.Message);
        }

        /// <summary>
        /// Invoked when another informative message should be logged
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Invoked when SQL being executed should be logged
        /// </summary>
        /// <param name="message">SQL to log</param>
        public void LogSql(string message)
        {
            if (this.EnableSqlLogging)
                Console.WriteLine(message);
        }

        private void WriteHeader(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(new String('-', Console.WindowWidth - 1));
            Console.WriteLine(format, args);
            Console.WriteLine(new String('-', Console.WindowWidth - 1));
            Console.WriteLine();
            Console.ForegroundColor = this.foregroundColor;
        }

        private void WriteError(string format, params object[] args)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new String('-', Console.WindowWidth - 1));
            Console.WriteLine(format, args);
            Console.WriteLine(new String('-', Console.WindowWidth - 1));
            Console.ForegroundColor = this.foregroundColor;
        }
    }
}
