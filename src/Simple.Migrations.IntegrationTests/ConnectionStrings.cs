using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests
{
    public static class ConnectionStrings
    {
        public static readonly string SQLite = "DataSource=database.sqlite";
        public static readonly string MSSQL = @"Server=.;Database=SimpleMigratorTests;Trusted_Connection=True;";
        public static readonly string MySQL = @"Server=localhost;Database=SimpleMigrator;Uid=SimpleMigrator;Pwd=SimpleMigrator;";
    }
}
