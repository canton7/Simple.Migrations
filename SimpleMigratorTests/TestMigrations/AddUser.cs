using SimpleMigrations;

namespace SimpleMigratorTests.TestMigrations
{
    [Migration(1, useTransaction:false)]
    public class AddUser : Migration
    {
        public override void Up()
        {
            Execute(@"CREATE TABLE Users (
                [Id] [int] IDENTITY(1,1)  PRIMARY KEY NOT NULL,
                Name TEXT NOT NULL);");
        }

        public override void Down()
        {
            Execute("DROP TABLE Users");
        }
    }
}
