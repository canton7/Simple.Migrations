using System;

namespace SimpleMigrations
{
    /// <summary>
    /// <see cref="ILogger"/> implementation which does nothing
    /// </summary>
    public class NullLogger : ILogger
    {
        public void BeginSequence(MigrationData from, MigrationData to) { }
        public void EndSequence(MigrationData from, MigrationData to) { }
        public void EndSequenceWithError(Exception exception, MigrationData from, MigrationData currentVersion) { }
        public void BeginUp(MigrationData migration) { } 
        public void EndUp(MigrationData migration) { }
        public void EndUpWithError(Exception exception, MigrationData migration) { }
        public void BeginDown(MigrationData migration) { } 
        public void EndDown(MigrationData migration) { }
        public void EndDownWithError(Exception exception, MigrationData migration) { }
        public void Info(string message) { }
        public void LogSql(string message) { }
    }
}
