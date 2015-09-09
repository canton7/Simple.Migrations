using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleMigrations;
using SQLite;

namespace SQLiteNet
{
    public abstract class SQLiteNetMigration : IMigration<SQLiteConnection>
    {
        public SQLiteConnection DB { get; set; }

        public IMigrationLogger Logger { get; set; }

        public abstract void Down();

        public abstract void Up();

        public void Execute(string sql)
        {
            this.Logger.LogSql(sql);
            this.DB.Execute(sql);
        }
    }
}
