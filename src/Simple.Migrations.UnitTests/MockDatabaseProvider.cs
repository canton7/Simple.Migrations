using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleMigrations;
using System.Data.Common;

namespace Simple.Migrations.UnitTests
{
    public abstract class MockDatabaseProvider : IDatabaseProvider<DbConnection>
    {
        public long CurrentVersion;
        public DbConnection Connection;

        public DbConnection BeginOperation()
        {
            return this.Connection;
        }

        public void EndOperation()
        {
        }

        public abstract void EnsureCreated();

        public long EnsureCreatedAndGetCurrentVersion()
        {
            return this.CurrentVersion;
        }

        public virtual long GetCurrentVersion()
        {
            return this.CurrentVersion;
        }

        public virtual void UpdateVersion(long oldVersion, long newVersion, string newDescription)
        {
            this.CurrentVersion = newVersion;
        }
    }
}
