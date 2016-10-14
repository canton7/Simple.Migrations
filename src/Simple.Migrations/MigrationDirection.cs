namespace SimpleMigrations
{
    /// <summary>
    /// Represents the direction of a migration
    /// </summary>
    public enum MigrationDirection
    {
        /// <summary>
        /// Migration is going up: the migration being applied is newer than the current version
        /// </summary>
        Up,

        /// <summary>
        /// Migration is going down: the migration being applied is older than the current version
        /// </summary>
        Down
    }
}
