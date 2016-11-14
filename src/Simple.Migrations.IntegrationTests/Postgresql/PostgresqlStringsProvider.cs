using System;

namespace Simple.Migrations.IntegrationTests.Postgresql
{
    public class PostgresqlStringsProvider : IMigrationStringsProvider
    {
        public string CreateUsersTableDown => @"CREATE TABLE Users (
                Id SERIAL NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            )";
        public string CreateUsersTableUp => "DROP TABLE Users";
    }
}