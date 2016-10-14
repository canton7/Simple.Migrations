namespace SimpleMigrations
{
    /// <summary>
    /// Interface which must be implemented by all migrations, although you probably want to derive from <see cref="Migration"/> instead
    /// </summary>
    /// <typeparam name="TDatabase">Type of database connection which this migration will use</typeparam>
    public interface IMigration<TDatabase>
    {
        /// <summary>
        /// Gets or sets the database to be used by this migration
        /// </summary>
        TDatabase DB { get; set; }

        /// <summary>
        /// Gets or sets the logger to be used by this migration
        /// </summary>
        IMigrationLogger Logger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to run <see cref="Up"/> and <see cref="Down"/> in a transaction.
        /// </summary>
        /// <remarks>
        /// This is set to the value of 'useTransaction' on the [Migration] attribute by default, but can be overridden
        /// </remarks>
        bool UseTransaction { get; set; }

        /// <summary>
        /// Invoked when this migration should migrate up
        /// </summary>
        void Up();

        /// <summary>
        /// Invoked when this migration should migrate down
        /// </summary>
        void Down();
    }
}
