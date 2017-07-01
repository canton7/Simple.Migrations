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
        protected bool UseTransaction { get; set; }

        protected SQLiteConnection Connection { get; set; }

        protected IMigrationLogger Logger { get; set; }

        public abstract void Down();

        public abstract void Up();

        public void Execute(string sql)
        {
            this.Logger.LogSql(sql);
            this.Connection.Execute(sql);
        }

        void IMigration<SQLiteConnection>.RunMigration(MigrationRunData<SQLiteConnection> data)
        {
            this.Connection = data.Connection;
            this.Logger = data.Logger;

            if (this.UseTransaction)
            {
                try
                {
                    this.Connection.BeginTransaction();

                    if (data.Direction == MigrationDirection.Up)
                        this.Up();
                    else
                        this.Down();

                    this.Connection.Commit();
                }
                catch
                {
                    this.Connection.Rollback();
                    throw;
                }
            }
            else
            {
                if (data.Direction == MigrationDirection.Up)
                    this.Up();
                else
                    this.Down();
            }
        }
    }
}
