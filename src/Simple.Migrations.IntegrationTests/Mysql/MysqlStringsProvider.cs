using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests.Mysql
{
    public class MysqlStringsProvider : IMigrationStringsProvider
    {
        public string CreateUsersTableUp => @"CREATE TABLE Users (
                `Id` INT NOT NULL PRIMARY KEY AUTO_INCREMENT
                `Name` TEXT NOT NULL
            );";

        public string CreateUsersTableDown => "DROP TABLE Users";
    }
}
