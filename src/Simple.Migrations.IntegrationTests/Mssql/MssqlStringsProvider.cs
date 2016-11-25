using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests.Mssql
{
    public class MssqlStringsProvider : IMigrationStringsProvider
    {
        public string CreateUsersTableUp => @"CREATE TABLE Users (
                [Id] [int] IDENTITY(1,1)  PRIMARY KEY NOT NULL,
                Name TEXT NOT NULL
            );";

        public string CreateUsersTableDown => "DROP TABLE Users";
    }
}
