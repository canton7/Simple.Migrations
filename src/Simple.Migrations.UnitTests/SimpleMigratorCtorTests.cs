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
        private Mock<IVersionProvider<ITransactionAwareDbConnection>> versionProvider;

        [SetUp]
        public void SetUp()
        {
            this.connection = new Mock<ITransactionAwareDbConnection>();
            this.migrationProvider = new Mock<IMigrationProvider>();
            this.connectionProvider = new Mock<IConnectionProvider<ITransactionAwareDbConnection>>();
            this.versionProvider = new Mock<IVersionProvider<ITransactionAwareDbConnection>>();

            this.connectionProvider.Setup(x => x.Connection).Returns(this.connection.Object);
        }

        [Test]
        public void CtorSetsVersionProviderConnection()
        {
            var migrator = new SimpleMigrator<ITransactionAwareDbConnection, Migration<ITransactionAwareDbConnection>>(this.migrationProvider.Object, this.connectionProvider.Object, this.versionProvider.Object);

            this.versionProvider.Verify(x => x.SetConnection(this.connection.Object));
        }
    }
}
