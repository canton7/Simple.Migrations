using System;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// Class representing data about a migration
    /// </summary>
    public class MigrationData
    {
        internal static MigrationData EmptySchema { get; } = new MigrationData();

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

        /// <summary>
        /// Initialises a new instance of the <see cref="MigrationData"/> class
        /// </summary>
        /// <param name="version">Version of this migration</param>
        /// <param name="description">Description of this migration. May be null</param>
        /// <param name="typeInfo">Type of class implementing this migration</param>
        /// <param name="useTransaction">Whether or not this migration should be run inside a transactio</param>
        public MigrationData(long version, string description, TypeInfo typeInfo, bool useTransaction)
        {
            // 'version' is verified to be >0 in SimpleMigrator
            if (typeInfo == null)
                throw new ArgumentNullException(nameof(typeInfo));

            this.Version = version;
            this.Description = description;
            this.TypeInfo = typeInfo;
            this.UseTransaction = useTransaction;

            var descriptionPart = String.IsNullOrWhiteSpace(this.Description) ? "" : $" ({this.Description})";
            this.FullName = this.TypeInfo.Name + descriptionPart;
        }

        /// <summary>
        /// Creates the empty scheme migration
        /// </summary>
        private MigrationData()
        {
            this.Version = 0;
            this.Description = "Empty Schema";
            this.FullName = "Empty Schema";
            this.TypeInfo = null;
            this.UseTransaction = false;
        }

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        /// <returns>A string representation of the object</returns>
        public override string ToString()
        {
            return $"<{nameof(MigrationData)} Version={this.Version} Description={this.Description} FullName={this.FullName} TypeInfo={this.TypeInfo} UseTransaction={this.UseTransaction}>";
        }
    }
}
