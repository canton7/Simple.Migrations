using System;

namespace SimpleMigrations
{
    /// <summary>
    /// A logger, which can log migration progress and output
    /// </summary>
    public interface ILogger : IMigrationLogger
    {
        /// <summary>
        /// Invoked when a sequence of migrations is started
        /// </summary>
        /// <param name="from">Migration being migrated from</param>
        /// <param name="to">Migration being migrated to</param>
        void BeginSequence(MigrationData from, MigrationData to);

        /// <summary>
        /// Invoked when a sequence of migrations is completed successfully
        /// </summary>
        /// <param name="from">Migration which was migrated from</param>
        /// <param name="to">Migration which was migrated to</param>
        void EndSequence(MigrationData from, MigrationData to);

        /// <summary>
        /// Invoked when a sequence of migrations fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="from">Migration which was migrated from</param>
        /// <param name="currentVersion">Last successful migration which was applied</param>
        void EndSequenceWithError(Exception exception, MigrationData from, MigrationData currentVersion);

        /// <summary>
        /// Invoked when an individual "up" migration is started
        /// </summary>
        /// <param name="migration">Migration being started</param>
        void BeginUp(MigrationData migration);

        /// <summary>
        /// Invoked when an individual "up" migration is completed successfully
        /// </summary>
        /// <param name="migration">Migration which completed</param>
        void EndUp(MigrationData migration);

        /// <summary>
        /// Invoked when an individual "up" migration fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="migration">Migration which failed</param>
        void EndUpWithError(Exception exception, MigrationData migration);

        /// <summary>
        /// Invoked when an individual "down" migration is started
        /// </summary>
        /// <param name="migration">Migration being started</param>
        void BeginDown(MigrationData migration);

        /// <summary>
        /// Invoked when an individual "down" migration is completed successfully
        /// </summary>
        /// <param name="migration">Migration which completed</param>
        void EndDown(MigrationData migration);

        /// <summary>
        /// Invoked when an individual "down" migration fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="migration">Migration which failed</param>
        void EndDownWithError(Exception exception, MigrationData migration);
    }
}
