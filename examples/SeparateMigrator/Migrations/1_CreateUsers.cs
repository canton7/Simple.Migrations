using SimpleMigrations;

namespace SeparateMigrator.Migrations
{
    [Migration(1)]
    public class CreateUsers : Migration
    {
        protected override void Up()
        {
            Execute(@"CREATE TABLE Users (
                Id SERIAL NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            );");
        }

        protected override void Down()
        {
            Execute("DROP TABLE Users");
        }
    }
}
