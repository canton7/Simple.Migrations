using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests.Migrations
{
    [Migration(1)]
    public class CreateUsersTable : Migration
    {
        private static string upSql;
        private static string downSql;

        public static void Setup(IMigrationStringsProvider strings)
        {
            upSql = strings.CreateUsersTableUp;
            downSql = strings.CreateUsersTableDown;
        }

        protected override void Up()
        {
            this.Execute(upSql);
            Thread.Sleep(200);
        }

        protected override void Down()
        {
            this.Execute(downSql);
            Thread.Sleep(200);
        }
    }
}
