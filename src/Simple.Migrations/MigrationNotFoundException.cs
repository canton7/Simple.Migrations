namespace SimpleMigrations
{
    /// <summary>
    /// Exception thrown when the requested migration could not be found
    /// </summary>
    public class MigrationNotFoundException : MigrationException
    {
        /// <summary>
        /// The version of the migration which was requested but not found
        /// </summary>
        public long Version => (long)this.Data[nameof(this.Version)];

        /// <summary>
        /// Initialises a new instance of the <see cref="MigrationNotFoundException"/> class
        /// </summary>
        /// <param name="version">Version of the migration which was requested but not found</param>
        public MigrationNotFoundException(long version)
            : base($"Could not find migration with version {version}")
        {
            this.Data[nameof(this.Version)] = version;
        }
    }
}
