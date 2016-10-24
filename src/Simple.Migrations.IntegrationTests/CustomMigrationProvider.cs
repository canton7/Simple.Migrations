using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace Simple.Migrations.IntegrationTests
{
    public class CustomMigrationProvider : IMigrationProvider
    {
        private readonly List<MigrationData> migrations;

        public CustomMigrationProvider(params Type[] migrationTypes)
        {
            var migrations = from type in migrationTypes
                             let attribute = type.GetCustomAttribute<MigrationAttribute>()
                             where attribute != null
                             select new MigrationData(attribute.Version, attribute.Description, type.GetTypeInfo(), attribute.UseTransaction);

            this.migrations = migrations.ToList();
        }

        public IEnumerable<MigrationData> LoadMigrations()
        {
            return this.migrations.ToList();
        }
    }
}
