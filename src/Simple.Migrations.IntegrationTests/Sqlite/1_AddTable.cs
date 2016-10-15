using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests.Sqlite
{
    [Migration(1)]
    public class AddTable : Migration
    {
        public override void Up()
        {
            Execute(@"CREATE TABLE Users (
                Id SERIAL NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            )");
        }

        public override void Down()
        {
            Execute("DROP TABLE Users");
        }
    }
}
