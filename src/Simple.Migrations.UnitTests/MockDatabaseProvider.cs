using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleMigrations;

namespace Simple.Migrations.UnitTests
{
    public abstract class MockDatabaseProvider : IDatabaseProvider<ITransactionAwareDbConnection>
    {
        public long CurrentVersion;

        public abstract void EnsureCreated();

        public virtual long GetCurrentVersion()
        {
            return this.CurrentVersion;
        }

        public abstract void SetConnection(ITransactionAwareDbConnection connection);

        public virtual void UpdateVersion(long oldVersion, long newVersion, string newDescription)
        {
            this.CurrentVersion = newVersion;
        }
    }
}
