using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests.Sqlite
{
    public class SqliteStringsProvider : IMigrationStringsProvider
    {
        public string CreateUsersTableDown => @"CREATE TABLE Users (
                Id SERIAL NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            )";
        public string CreateUsersTableUp => "DROP TABLE Users";
    }
}
