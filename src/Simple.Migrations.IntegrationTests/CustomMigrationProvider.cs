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
        public Type[] MigrationTypes { get; set; } = new Type[0];

        public CustomMigrationProvider()
        {
        }

        public CustomMigrationProvider(params Type[] migrationTypes)
        {
            this.MigrationTypes = migrationTypes;
        }

        public IEnumerable<MigrationData> LoadMigrations()
        {
            var migrations = from type in this.MigrationTypes
                             let attribute = type.GetCustomAttribute<MigrationAttribute>()
                             where attribute != null
                             select new MigrationData(attribute.Version, attribute.Description, type.GetTypeInfo());
            return migrations;
        }
    }
}
