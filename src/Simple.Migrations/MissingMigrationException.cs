using System;

namespace SimpleMigrations
{
    /// <summary>
    /// Exception thrown when migration data is missing
    /// </summary>
    /// <remarks>
    /// This happens if SimpleMigrator finds that the database is at a particular version, but can't find
    /// a migration with that version.
    /// </remarks>
    public class MissingMigrationException : MigrationException
    {
        /// <summary>
        /// Gets the version that migration data could not be found for
        /// </summary>
        public long MissingVersion => (long)this.Data[nameof(this.MissingVersion)];

        /// <summary>
        /// Instantiates a new instance of the <see cref="MissingMigrationException"/> class with the given missing version
        /// </summary>
        /// <param>Version that migration data could not be found for</param>
        public MissingMigrationException(long missingVersion)
            : base($"Unable to find a migration with the version {missingVersion}")
        {
            this.Data[nameof(this.MissingVersion)] = missingVersion;
        }
    }
}
