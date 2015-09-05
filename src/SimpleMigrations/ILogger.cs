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
        /// Invoked when an individual migration is started
        /// </summary>
        /// <param name="migration">Migration being started</param>
        /// <param name="direction">Direction of the migration</param>
        void BeginMigration(MigrationData migration, MigrationDirection direction);

        /// <summary>
        /// Invoked when an individual migration is completed successfully
        /// </summary>
        /// <param name="migration">Migration which completed</param>
        /// <param name="direction">Direction of the migration</param>
        void EndMigration(MigrationData migration, MigrationDirection direction);

        /// <summary>
        /// Invoked when an individual migration fails with an error
        /// </summary>
        /// <param name="exception">Exception which was encountered</param>
        /// <param name="migration">Migration which failed</param>
        /// <param name="direction">Direction of the migration</param>
        void EndMigrationWithError(Exception exception, MigrationData migration, MigrationDirection direction);
    }
}
