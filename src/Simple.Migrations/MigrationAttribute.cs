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
        /// Instantiates a new instance of the <see cref="MigrationAttribute"/> class
        /// </summary>
        /// <param name="version">Version of this migration</param>
        /// <param name="description">Optional description of this migration</param>
        public MigrationAttribute(long version, string description = null, bool useTransaction = true)
        {
            this.Version = version;
            this.Description = description;
        }
    }
}
