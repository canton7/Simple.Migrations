using System;
using System.Reflection;

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
        public long Version { get; }

        /// <summary>
        /// Description of this migration
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Type of class implementing this migration
        /// </summary>
        public TypeInfo TypeInfo { get; }

        /// <summary>
        /// Name of the migration, including the type name and description
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Whether or not this migration should be run inside a transaction
        /// </summary>
        public bool UseTransaction { get; }

        internal MigrationData(long version, string description, TypeInfo typeInfo, bool useTransaction)
        {
            // 'version' is verified to be >0 in SimpleMigrator
            if (typeInfo == null)
                throw new ArgumentNullException(nameof(typeInfo));

            this.Version = version;
            this.Description = description;
            this.TypeInfo = typeInfo;
            this.UseTransaction = useTransaction;

            if (this.TypeInfo == null)
            {
                this.FullName = this.Description;
            }
            else
            {
                var descriptionPart = String.IsNullOrWhiteSpace(this.Description) ? "" : $" ({this.Description})";
                this.FullName = this.TypeInfo.Name + descriptionPart;
            }
        }
    }
}
