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
            connection.CreateTable<VersionInfo>();
        }

        public long GetCurrentVersion(SQLiteConnection connection)
        {
            return connection.Table<VersionInfo>().OrderByDescending(x => x.Id).FirstOrDefault()?.Version ?? 0;
        }

        public void UpdateVersion(SQLiteConnection connection, long oldVersion, long newVersion, string newDescription)
        {
            connection.Insert(new VersionInfo()
            {
                Version = newVersion,
                AppliedOn = DateTime.Now,
                Description = newDescription,
            });
        }
    }
}
