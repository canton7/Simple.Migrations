using Moq;
using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.UnitTests
{
    [TestFixture]
    public class SimpleMigratorCtorTests
    {
        private Mock<ITransactionAwareDbConnection> connection;
        private Mock<IMigrationProvider> migrationProvider;
        private Mock<IConnectionProvider<ITransactionAwareDbConnection>> connectionProvider;
        private Mock<IDatabaseProvider<ITransactionAwareDbConnection>> databaseProvider;

        [SetUp]
        public void SetUp()
        {
            this.connection = new Mock<ITransactionAwareDbConnection>();
            this.migrationProvider = new Mock<IMigrationProvider>();
            this.connectionProvider = new Mock<IConnectionProvider<ITransactionAwareDbConnection>>();
            this.databaseProvider = new Mock<IDatabaseProvider<ITransactionAwareDbConnection>>();

            this.connectionProvider.Setup(x => x.Connection).Returns(this.connection.Object);
        }

        [Test]
        public void CtorSetsVersionProviderConnection()
        {
            var migrator = new SimpleMigrator<ITransactionAwareDbConnection, Migration<ITransactionAwareDbConnection>>(this.migrationProvider.Object, this.connectionProvider.Object, this.databaseProvider.Object);

            this.databaseProvider.Verify(x => x.SetConnection(this.connection.Object));
        }
    }
}
