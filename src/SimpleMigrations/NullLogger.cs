using System;

namespace SimpleMigrations
{
    /// <summary>
    /// <see cref="ILogger"/> implementation which does nothing
    /// </summary>
    public class NullLogger : ILogger
    {
        /// <summary>
        /// Invoked when a sequence of migrations is started
        /// </summary>
        /// <param name="from">Migration being migrated from</param>
        /// <param name="to">Migration being migrated to</param>
        public void BeginSequence(MigrationData from, MigrationData to) { }

        /// <summary>
        /// Invoked when a sequence of migrations is completed successfully
        /// </summary>
        /// <param name="from">Migration which was migrated from</param>
        /// <param name="to">Migration which was migrated to</param>
        public void EndSequence(MigrationData from, MigrationData to) { }

        /// <summary>
        /// Invoked when a sequence of migrations fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="from">Migration which was migrated from</param>
        /// <param name="currentVersion">Last successful migration which was applied</param>
        public void EndSequenceWithError(Exception exception, MigrationData from, MigrationData currentVersion) { }

        /// <summary>
        /// Invoked when an individual "up" migration is started
        /// </summary>
        /// <param name="migration">Migration being started</param>
        public void BeginUp(MigrationData migration) { }

        /// <summary>
        /// Invoked when an individual "up" migration is completed successfully
        /// </summary>
        /// <param name="migration">Migration which completed</param>
        public void EndUp(MigrationData migration) { }

        /// <summary>
        /// Invoked when an individual "up" migration fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="migration">Migration which failed</param>
        public void EndUpWithError(Exception exception, MigrationData migration) { }

        /// <summary>
        /// Invoked when an individual "down" migration is started
        /// </summary>
        /// <param name="migration">Migration being started</param>
        public void BeginDown(MigrationData migration) { }

        /// <summary>
        /// Invoked when an individual "down" migration is completed successfully
        /// </summary>
        /// <param name="migration">Migration which completed</param>
        public void EndDown(MigrationData migration) { }

        /// <summary>
        /// Invoked when an individual "down" migration fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="migration">Migration which failed</param>
        public void EndDownWithError(Exception exception, MigrationData migration) { }

        /// <summary>
        /// Invoked when another informative message should be logged
        /// </summary>
        /// <param name="message">Message to be logged</param>
        public void Info(string message) { }

        /// <summary>
        /// Invoked when SQL being executed should be logged
        /// </summary>
        /// <param name="message">SQL to log</param>
        public void LogSql(string message) { }
    }
}
