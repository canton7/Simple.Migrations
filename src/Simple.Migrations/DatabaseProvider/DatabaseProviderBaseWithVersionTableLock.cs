using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMigrations.DatabaseProvider
{
    public abstract class DatabaseProviderBaseWithVersionTableLock : DatabaseProviderBase
    {
        protected DbConnection VersionTableConnection { get; set; }
        protected DbTransaction VersionTableLockTransaction { get; set; }

        protected DbConnection MigrationsConnection { get; set; }

        public DatabaseProviderBaseWithVersionTableLock(Func<DbConnection> connectionFactory)
            : base(connectionFactory)
        {
        }

        public override DbConnection BeginOperation()
        {
            this.VersionTableConnection = this.ConnectionFactory();
            this.VersionTableConnection.Open();

            this.MigrationsConnection = this.ConnectionFactory();
            this.MigrationsConnection.Open();

            this.AcquireVersionTableLock();

            return this.MigrationsConnection;
        }

        public override void EndOperation()
        {
            this.ReleaseVersionTableLock();

            this.VersionTableConnection.Dispose();
            this.VersionTableConnection = null;

            this.MigrationsConnection.Dispose();
            this.MigrationsConnection = null;
        }

        public override long EnsureCreatedAndGetCurrentVersion()
        {
            using (var connection = this.ConnectionFactory())
            {
                connection.Open();

                return this.EnsureCreatedAndGetCurrentVersion(connection);
            }
        }

        public override long GetCurrentVersion()
        {
            return this.GetCurrentVersion(this.VersionTableConnection, this.VersionTableLockTransaction);
        }

        public override void UpdateVersion(long oldVersion, long newVersion, string newDescription)
        {
            this.UpdateVersion(oldVersion, newVersion, newDescription, this.VersionTableConnection, this.VersionTableLockTransaction);
        }

        protected abstract void AcquireVersionTableLock();
        protected abstract void ReleaseVersionTableLock();
    }
}
