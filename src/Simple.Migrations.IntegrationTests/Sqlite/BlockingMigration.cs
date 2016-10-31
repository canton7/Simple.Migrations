using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleMigrations;

namespace Simple.Migrations.IntegrationTests.Sqlite
{
    [Migration(1)]
    public class BlockingMigration : Migration
    {
        public override void Up()
        {
            Execute(@"CREATE TABLE Users (
                Id SERIAL NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            )");

            Thread.Sleep(100);
        }

        public override void Down()
        {
            Execute("DROP TABLE Users");
        }
    }
}
