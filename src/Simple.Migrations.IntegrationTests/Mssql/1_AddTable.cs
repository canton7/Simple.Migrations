using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests.Mssql
{
    [Migration(1)]
    public class AddTable : Migration
    {
        public override void Up()
        {
            Execute(@"CREATE TABLE Users (
                [Id] [int] IDENTITY(1,1)  PRIMARY KEY NOT NULL,
                Name TEXT NOT NULL
            );");
        }

        public override void Down()
        {
            Execute("DROP TABLE Users");
        }
    }
}
