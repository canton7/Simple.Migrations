using System;

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
        public Type Type { get; }

        /// <summary>
        /// Name of the migration, including the type name and description
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="MigrationData"/> class
        /// </summary>
        /// <param name="version">Version of this migration</param>
        /// <param name="description">Description of this migration. May be null</param>
        /// <param name="type">Type of class implementing this migration</param>
        public MigrationData(long version, string description, Type type)
        {
            // 'version' is verified to be >0 in SimpleMigrator
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            this.Version = version;
            this.Description = description;
            this.Type = type;

            var descriptionPart = String.IsNullOrWhiteSpace(this.Description) ? "" : $" ({this.Description})";
            this.FullName = this.Type.Name + descriptionPart;
        }

        /// <summary>
        /// Creates the empty scheme migration
        /// </summary>
        private MigrationData()
        {
            this.Version = 0;
            this.Description = "Empty Schema";
            this.FullName = "Empty Schema";
            this.Type = null;
        }

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        /// <returns>A string representation of the object</returns>
        public override string ToString()
        {
            return $"<{nameof(MigrationData)} Version={this.Version} Description={this.Description} FullName={this.FullName} TypeInfo={this.Type}>";
        }
    }
}
