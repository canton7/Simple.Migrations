using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleMigrations;
using System.Data.Common;
using System.Data;

namespace Simple.Migrations.UnitTests
{
    public abstract class MockDatabaseProvider : IDatabaseProvider<IDbConnection>
    {
        public long CurrentVersion;
        public IDbConnection Connection;

        public IDbConnection BeginOperation()
        {
            return this.Connection;
        }

        public void EndOperation()
        {
        }

        public abstract void EnsureCreated();

        public long EnsurePrerequisitesCreatedAndGetCurrentVersion()
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
