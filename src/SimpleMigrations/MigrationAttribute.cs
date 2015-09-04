using System;

namespace SimpleMigrations
{
    /// <summary>
    /// [Migration(version)] attribute which must be applied to all migrations
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MigrationAttribute : Attribute
    {
        /// <summary>
        /// Version of this migration
        /// </summary>
        public long Version { get; set; }

        /// <summary>
        /// Gets or sets the optional description of this migration
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this migration should be run in a transaction (default to true)
        /// </summary>
        public bool UseTransaction { get; set; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="MigrationAttribute"/> class
        /// </summary>
        /// <param name="version">Version of this migration</param>
        /// <param name="description">Optional description of this migration</param>
        /// <param name="useTransaction">Whether this migration should be run in a transaction</param>
        public MigrationAttribute(long version, string description = null, bool useTransaction = true)
        {
            this.Version = version;
            this.Description = description;
            this.UseTransaction = useTransaction;
        }
    }
}
