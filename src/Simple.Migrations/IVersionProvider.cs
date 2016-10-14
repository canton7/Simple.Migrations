namespace SimpleMigrations
{
    /// <summary>
    /// Interface representing the ability to interact with the Version table in the database
    /// </summary>
    /// <typeparam name="TDatabase">Type of database connection</typeparam>
    public interface IVersionProvider<TDatabase>
    {
        /// <summary>
        /// Ensure that the version table exists, creating it if necessary
        /// </summary>
        /// <param name="connection">Connection to use to perform this action</param>
        void EnsureCreated(TDatabase connection);

        /// <summary>
        /// Return the current version from the version table
        /// </summary>
        /// <param name="connection">Connection to use to perform this action</param>
        /// <returns>Current version</returns>
        long GetCurrentVersion(TDatabase connection);

        /// <summary>
        /// Update the current version in the version table
        /// </summary>
        /// <param name="connection">Connection to use to perform this action</param>
        /// <param name="oldVersion">Version being upgraded from</param>
        /// <param name="newVersion">Version being upgraded to</param>
        /// <param name="newDescription">Description to associate with the new version</param>
        void UpdateVersion(TDatabase connection, long oldVersion, long newVersion, string newDescription);
    }
}
