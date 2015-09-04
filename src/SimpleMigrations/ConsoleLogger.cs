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

        public ConsoleLogger()
        {
            this.EnableSqlLogging = true;
        }

        public void BeginSequence(MigrationData from, MigrationData to)
        {
            this.foregroundColor = Console.ForegroundColor;

            this.WriteHeader("Migrating from {0}: {1} to {2}: {3}", from.Version, from.Description, to.Version, to.Description);
        }

        public void EndSequence(MigrationData from, MigrationData to)
        {
            this.WriteHeader("Done");
        }

        public void EndSequenceWithError(Exception exception, MigrationData from, MigrationData currentVersion)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Database is curently on version {0}: {1}", currentVersion.Version, currentVersion.Description);
            Console.ForegroundColor = this.foregroundColor;
        }

        public void BeginUp(MigrationData migration)
        {
            this.WriteHeader("{0}: {1} migrating", migration.Version, migration.Description);
        }

        public void EndUp(MigrationData migration)
        {
            Console.WriteLine();
        }

        public void EndUpWithError(Exception exception, MigrationData migration)
        {
            this.WriteError("{0}: {1} ERROR {2}", migration.Version, migration.Description, exception.Message);
        }

        public void BeginDown(MigrationData migration)
        {
            this.WriteHeader("{0}: {1} reverting", migration.Version, migration.Description);
        }

        public void EndDown(MigrationData migration)
        {
            Console.WriteLine();
        }

        public void EndDownWithError(Exception exception, MigrationData migration)
        {
            this.WriteError("{0}: {1} ERROR {2}", migration.Version, migration.Description, exception.Message);
        }

        public void Info(string message)
        {
            Console.WriteLine(message);
        }

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
