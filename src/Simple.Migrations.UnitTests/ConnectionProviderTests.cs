using Moq;
using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.UnitTests
{
    [TestFixture]
    public class ConnectionProviderTests : AssertionHelper
    {
        private Mock<IDbConnection> connection;
        private ConnectionProvider connectionProvider;

        [SetUp]
        public void SetUp()
        {
            this.connection = new Mock<IDbConnection>();
            this.connectionProvider = new ConnectionProvider(this.connection.Object);
        }

        [Test]
        public void BeginTransactionBeginsAndRecordsTransaction()
        {
            var transaction = new Mock<IDbTransaction>().Object;
            this.connection.Setup(x => x.BeginTransaction()).Returns(transaction).Verifiable();

            this.connectionProvider.BeginTransaction();

            this.connection.VerifyAll();
            Expect(this.connectionProvider.Transaction, EqualTo(transaction));
        }

        [Test]
        public void BeginTransactionThrowsIfATransactionIsAlreadyInProgress()
        {
            var transaction = new Mock<IDbTransaction>().Object;
            this.connection.Setup(x => x.BeginTransaction()).Returns(transaction);

            this.connectionProvider.BeginTransaction();

            Expect(() => this.connectionProvider.BeginTransaction(), Throws.InvalidOperationException);
        }

        [Test]
        public void IConnectionProviderBeginTransactionStartedAsSerializable()
        {
            ((IConnectionProvider<ITransactionAwareDbConnection>)this.connectionProvider).BeginTransaction();

            this.connection.Verify(x => x.BeginTransaction(IsolationLevel.Serializable));
        }

        [Test]
        public void CommitTransactionThrowsIfNoTransactionIsInProgress()
        {
            Expect(() => this.connectionProvider.CommitTransaction(), Throws.InvalidOperationException);
        }

        [Test]
        public void CommitTransactionCommitsAndClearsTransaction()
        {
            var transaction = new Mock<IDbTransaction>();
            this.connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);
            this.connectionProvider.BeginTransaction();

            this.connectionProvider.CommitTransaction();

            transaction.Verify(x => x.Commit());
            Expect(this.connectionProvider.Transaction, Null);
        }

        [Test]
        public void RollbackTransactionThrowsIfNoTransactionIsInProgress()
        {
            Expect(() => this.connectionProvider.RollbackTransaction(), Throws.InvalidOperationException);
        }

        [Test]
        public void RollacbkTransactionRollsBackAndClearsTransaction()
        {
            var transaction = new Mock<IDbTransaction>();
            this.connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);
            this.connectionProvider.BeginTransaction();

            this.connectionProvider.RollbackTransaction();

            transaction.Verify(x => x.Rollback());
            Expect(this.connectionProvider.Transaction, Null);
        }

        [Test]
        public void CreateCommandAssignsTransactionToCommand()
        {
            var transaction = new Mock<IDbTransaction>().Object;
            this.connection.Setup(x => x.BeginTransaction()).Returns(transaction);
            this.connectionProvider.BeginTransaction();

            var command = new Mock<IDbCommand>();
            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

            var createdCommand = this.connectionProvider.CreateCommand();

            Expect(createdCommand, EqualTo(command.Object));
            command.VerifySet(x => x.Transaction = transaction);
        }

        [Test]
        public void DisposeDisposesTransaction()
        {
            var transaction = new Mock<IDbTransaction>();
            this.connection.Setup(x => x.BeginTransaction()).Returns(transaction.Object);
            this.connectionProvider.BeginTransaction();

            this.connectionProvider.Dispose();

            transaction.Verify(x => x.Dispose());
            this.connection.Verify(x => x.Dispose());
        }
    }
}
