using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleMigrations
{
    /// <summary>
    /// <see cref="IMigrationProvider"/>  which finds migrations by scanning an assembly, optionally
    /// filtering by namespace.
    /// </summary>
    public class AssemblyMigrationProvider : IMigrationProvider
    {
        private readonly Assembly migrationAssembly;
        private readonly string migrationNamespace;

        /// <summary>
        /// Instantiates a new instance of the <see cref="AssemblyMigrationProvider"/> class
        /// </summary>
        /// <param name="migrationAssembly">Assembly to scan for migrations</param>
        /// <param name="migrationNamespace">Optional namespace. If specified, only finds migrations in that namespace</param>
        public AssemblyMigrationProvider(Assembly migrationAssembly, string migrationNamespace = null)
        {
            if (migrationAssembly == null)
                throw new ArgumentNullException(nameof(migrationAssembly));

            this.migrationAssembly = migrationAssembly;
            this.migrationNamespace = migrationNamespace;
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
                             where this.migrationNamespace == null || type.Namespace == this.migrationNamespace
                             select new MigrationData(attribute.Version, attribute.Description, type);

            if (!migrations.Any())
                throw new MigrationException($"Could not find any migrations in the assembly you provided ({this.migrationAssembly.GetName().Name}). Migrations must be decorated with [Migration]");

            return migrations;
        }
    }
}
