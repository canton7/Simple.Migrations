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
        public readonly long Version;

        /// <summary>
        /// Description of this migration
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Type of class implementing this migration
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Whether or not this migration should be run inside a transaction
        /// </summary>
        public readonly bool UseTransaction;

        internal MigrationData(long version, string description, Type type, bool useTransaction)
        {
            this.Version = version;
            this.Description = description;
            this.Type = type;
            this.UseTransaction = useTransaction;
        }
    }
}
