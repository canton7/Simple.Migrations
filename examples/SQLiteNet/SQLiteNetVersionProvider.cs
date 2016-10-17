using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleMigrations;
using SQLite;

namespace SQLiteNet
{
    public class SQLiteNetVersionProvider : IVersionProvider<SQLiteConnection>
    {
        private SQLiteConnection connection;

        public void SetConnection(SQLiteConnection connection)
        {
            this.connection = connection;
        }

        public void EnsureCreated()
        {
            this.connection.CreateTable<SchemaVersion>();
        }

        public long GetCurrentVersion()
        {
            // Return 0 if the table has no entries
            var latestOrNull = this.connection.Table<SchemaVersion>().OrderByDescending(x => x.Id).FirstOrDefault();
            return latestOrNull?.Version ?? 0;
        }

        public void UpdateVersion(long oldVersion, long newVersion, string newDescription)
        {
            this.connection.Insert(new SchemaVersion()
            {
                Version = newVersion,
                AppliedOn = DateTime.Now,
                Description = newDescription,
            });
        }
    }
}
