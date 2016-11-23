using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMigrations.DatabaseProvider
{
    public abstract class DatabaseProviderBaseWithAdvisoryLock : DatabaseProviderBase
    {
        protected DbConnection Connection { get; }

        public DatabaseProviderBaseWithAdvisoryLock(DbConnection connection)
        {
            this.Connection = connection;
            this.Connection.Open();
        }

        public override DbConnection BeginOperation()
        {
            this.AcquireAdvisoryLock(this.Connection);
            return this.Connection;
        }

        public override void EndOperation()
        {
            this.ReleaseAdvisoryLock(this.Connection);
        }

        public override long EnsureCreatedAndGetCurrentVersion()
        {
            try
            {
                this.AcquireAdvisoryLock(this.Connection);

                return this.EnsureCreatedAndGetCurrentVersion(this.Connection);
            }
            finally
            {
                this.ReleaseAdvisoryLock(this.Connection);
            }
        }

        public override long GetCurrentVersion()
        {
            return this.GetCurrentVersion(this.Connection, null);
        }

        public override void UpdateVersion(long oldVersion, long newVersion, string newDescription)
        {
            this.UpdateVersion(oldVersion, newVersion, newDescription, this.Connection, null);
        }

        public abstract void AcquireAdvisoryLock(DbConnection connection);
        public abstract void ReleaseAdvisoryLock(DbConnection connection);
    }
}
