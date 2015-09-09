using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleMigrations;
using SQLite;

namespace SQLiteNet
{
    public class SQLiteNetConnectionProvider : IConnectionProvider<SQLiteConnection>
    {
        public SQLiteConnection Connection { get; }

        public SQLiteNetConnectionProvider(SQLiteConnection connection)
        {
            this.Connection = connection;
        }

        public void BeginTransaction()
        {
            this.Connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            this.Connection.Commit();
        }

        public void RollbackTransaction()
        {
            this.Connection.Rollback();
        }
    }
}
