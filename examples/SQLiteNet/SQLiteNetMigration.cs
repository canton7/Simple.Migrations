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

        void IMigration<SQLiteConnection>.Execute(SQLiteConnection connection, IMigrationLogger logger, MigrationDirection direction)
        {
            this.Connection = connection;
            this.Logger = logger;

            if (this.UseTransaction)
            {
                try
                {
                    connection.BeginTransaction();

                    if (direction == MigrationDirection.Up)
                        this.Up();
                    else
                        this.Down();

                    connection.Commit();
                }
                catch
                {
                    connection.Rollback();
                    throw;
                }
            }
            else
            {
                if (direction == MigrationDirection.Up)
                    this.Up();
                else
                    this.Down();
            }
        }
    }
}
