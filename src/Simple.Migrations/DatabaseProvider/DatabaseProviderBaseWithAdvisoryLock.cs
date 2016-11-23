using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMigrations.DatabaseProvider
{
    public abstract class DatabaseProviderBaseWithAdvisoryLock : DatabaseProviderBase
    {
        protected DbConnection Connection { get; set; }

        public DatabaseProviderBaseWithAdvisoryLock(Func<DbConnection> connectionFactory)
            : base(connectionFactory)
        {
        }

        public override DbConnection BeginOperation()
        {
            this.Connection = this.ConnectionFactory();
            this.Connection.Open();

            this.AcquireAdvisoryLock(this.Connection);
            return this.Connection;
        }

        public override void EndOperation()
        {
            this.ReleaseAdvisoryLock(this.Connection);

            this.Connection.Dispose();
            this.Connection = null;
        }

        public override long EnsureCreatedAndGetCurrentVersion()
        {
            using (var connection = this.ConnectionFactory())
            {
                connection.Open();

                try
                {
                    this.AcquireAdvisoryLock(connection);

                    return this.EnsureCreatedAndGetCurrentVersion(connection);
                }
                finally
                {
                    this.ReleaseAdvisoryLock(connection);
                }
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
