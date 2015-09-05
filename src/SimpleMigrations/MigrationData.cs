using System;

namespace SimpleMigrations
{
    /// <summary>
    /// Class representing data about a migration
    /// </summary>
    public class MigrationData
    {
        /// <summary>
        /// Version of this migration
        /// </summary>
        public long Version { get; private set; }

        /// <summary>
        /// Description of this migration
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Type of class implementing this migration
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Whether or not this migration should be run inside a transaction
        /// </summary>
        public bool UseTransaction { get; private set; }

        internal MigrationData(long version, string description, Type type, bool useTransaction)
        {
            this.Version = version;
            this.Description = description;
            this.Type = type;
            this.UseTransaction = useTransaction;
        }
    }
}
