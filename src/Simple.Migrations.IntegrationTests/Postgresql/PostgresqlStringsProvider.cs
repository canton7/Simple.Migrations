using System;

namespace Simple.Migrations.IntegrationTests.Postgresql
{
    public class PostgresqlStringsProvider : IMigrationStringsProvider
    {
        public string CreateUsersTableUp => @"CREATE TABLE Users (
                Id SERIAL NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            )";
        public string CreateUsersTableDown => "DROP TABLE Users";
    }
}