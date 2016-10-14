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
        public long Version { get; private set; }

        /// <summary>
        /// Description of this migration
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Type of class implementing this migration
        /// </summary>
        public TypeInfo TypeInfo { get; private set; }

        /// <summary>
        /// Name of the migration, including the type name and description
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// Whether or not this migration should be run inside a transaction
        /// </summary>
        public bool UseTransaction { get; private set; }

        internal MigrationData(long version, string description, TypeInfo typeInfo, bool useTransaction)
        {
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
                var descriptionPart = String.IsNullOrWhiteSpace(this.Description) ? "" : String.Format(" ({0})", this.Description);
                this.FullName = this.TypeInfo.Name + descriptionPart;
            }
        }
    }
}
