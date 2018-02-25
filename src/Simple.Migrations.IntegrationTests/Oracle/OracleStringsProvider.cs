namespace Simple.Migrations.IntegrationTests.Oracle
{
    public class OracleStringsProvider : IMigrationStringsProvider
    {
        public string CreateUsersTableUp => @"CREATE TABLE Users (
                Id INTEGER NOT NULL PRIMARY KEY,
                Name VARCHAR2(100) NOT NULL
            )";
        public string CreateUsersTableDown => "DROP TABLE Users PURGE";
    }
}