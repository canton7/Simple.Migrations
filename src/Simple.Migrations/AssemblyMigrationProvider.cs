using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// <see cref="IMigrationProvider"/>  which finds migrations by scanning an assembly
    /// </summary>
    public class AssemblyMigrationProvider : IMigrationProvider
    {
        private readonly Assembly migrationAssembly;

        /// <summary>
        /// Instantiates a new instance of the <see cref="AssemblyMigrationProvider"/> class
        /// </summary>
        /// <param name="migrationAssembly">Assembly to scan for migrations</param>
        public AssemblyMigrationProvider(Assembly migrationAssembly)
        {
            if (migrationAssembly == null)
                throw new ArgumentNullException(nameof(migrationAssembly));

            this.migrationAssembly = migrationAssembly;
        }

        /// <summary>
        /// Load all migration info. These can be in any order
        /// </summary>
        /// <returns>All migration info</returns>
        public IEnumerable<MigrationData> LoadMigrations()
        {
            var migrations = from type in this.migrationAssembly.DefinedTypes
                             let attribute = type.GetCustomAttribute<MigrationAttribute>()
                             where attribute != null
                             select new MigrationData(attribute.Version, attribute.Description, type);

            if (!migrations.Any())
                throw new MigrationException($"Could not find any migrations in the assembly you provided ({this.migrationAssembly.GetName().Name}). Migrations must be decorated with [Migration]");

            return migrations;
        }
    }
}
