using SimpleMigrations;

namespace SimpleMigratorTests.TestMigrations
{
    [Migration(1)]
    public class AddUser : Migration
    {
        public override void Up()
        {
            Execute(@"CREATE TABLE Users (
	            Id INT NOT NULL AUTO_INCREMENT,
	            Name TEXT NULL,
	            PRIMARY KEY (Id));");
        }

        public override void Down()
        {
            Execute("DROP TABLE Users");
        }
    }
}
