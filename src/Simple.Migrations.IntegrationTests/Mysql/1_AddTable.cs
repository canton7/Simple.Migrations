using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleMigrations;

namespace Simple.Migrations.IntegrationTests.Mysql
{
    [Migration(1)]
    public class AddTable : Migration
    {
        public override void Up()
        {
            Execute(@"CREATE TABLE Users (
                `Id` INT NOT NULL PRIMARY KEY AUTO_INCREMENT
                `Name` TEXT NOT NULL
            );");
        }

        public override void Down()
        {
            Execute(@"DROP TABLE Users");
        }
    }
}
