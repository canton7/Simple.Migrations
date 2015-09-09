using SimpleMigrations;

namespace SQLiteNet.Migrations
{
    [Migration(1)]
    public class CreateUsers : SQLiteNetMigration
    {
        public override void Up()
        {
            Execute(@"CREATE TABLE Users (
                Id SERIAL NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            );");
        }

        public override void Down()
        {
            Execute("DROP TABLE Users");
        }
    }
}
