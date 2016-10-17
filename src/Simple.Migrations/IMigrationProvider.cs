using System.Collections.Generic;

namespace SimpleMigrations
{
    /// <summary>
    /// Defines a class which loads the list of migration metadata
    /// </summary>
    /// <remarks>
    /// By default this is implemented by <see cref="AssemblyMigrationProvider"/>
    /// </remarks>
    public interface IMigrationProvider
    {
        /// <summary>
        /// Load all migration info. These can be in any order
        /// </summary>
        /// <returns>All migration info</returns>
        IEnumerable<MigrationData> LoadMigrations();
    }
}
