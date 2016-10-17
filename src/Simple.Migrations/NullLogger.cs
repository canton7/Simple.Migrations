using System;

namespace SimpleMigrations
{
    internal class NullLogger : ILogger
    {
        public static NullLogger Instance { get; } = new NullLogger();

        public void BeginSequence(MigrationData from, MigrationData to) { }
        public void EndSequence(MigrationData from, MigrationData to) { }
        public void EndSequenceWithError(Exception exception, MigrationData from, MigrationData currentVersion) { }

        public void BeginMigration(MigrationData migration, MigrationDirection direction) { }
        public void EndMigration(MigrationData migration, MigrationDirection direction) { }
        public void EndMigrationWithError(Exception exception, MigrationData migration, MigrationDirection direction) { }

        public void Info(string message) { }
        public void LogSql(string message) { }
    }
}
