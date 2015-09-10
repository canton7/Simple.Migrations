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
        public void EnsureCreated(SQLiteConnection connection)
        {
            connection.CreateTable<SchemaVersion>();
        }

        public long GetCurrentVersion(SQLiteConnection connection)
        {
            // Return 0 if the table has no entries
            var latestOrNull = connection.Table<SchemaVersion>().OrderByDescending(x => x.Id).FirstOrDefault();
            return latestOrNull == null ? 0 : latestOrNull.Version;
        }

        public void UpdateVersion(SQLiteConnection connection, long oldVersion, long newVersion, string newDescription)
        {
            connection.Insert(new SchemaVersion()
            {
                Version = newVersion,
                AppliedOn = DateTime.Now,
                Description = newDescription,
            });
        }
    }
}
