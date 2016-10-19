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
    public class VersionProviderBaseTests : AssertionHelper
    {
        private class VersionProvider : VersionProviderBase
        {
            public new bool UseTransaction
            {
                get { return base.UseTransaction; }
                set { base.UseTransaction = value; }
            }

            public string CreateTableSql = "Create Table SQL";
            public override string GetCreateVersionTableSql() => this.CreateTableSql;

            public string CurrentVersionSql = "Get Current Version SQL";
            public override string GetCurrentVersionSql() => this.CurrentVersionSql;

            public string SetVersionSql = "Set Version SQL";
            public override string GetSetVersionSql() => this.SetVersionSql;
        }

        private VersionProvider versionProvider;
        private Mock<IDbConnection> connection;

        [SetUp]
        public void SetUp()
        {
            this.versionProvider = new VersionProvider();

            this.connection = new Mock<IDbConnection>();
        }

        [Test]
        public void SetConnectionThrowsIfConnectionIsNull()
        {
            Expect(() => this.versionProvider.SetConnection(null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("connection"));
        }

        [Test]
        public void EnsureCreatedThrowsIfConnectionNotSet()
        {
            Expect(() => this.versionProvider.EnsureCreated(), Throws.InvalidOperationException);
        }

        [Test]
        public void EnsureCreatedRunsCreateTableSqlInsideATransactionIfConfigured()
        {
            this.versionProvider.SetConnection(this.connection.Object);

            var transaction = new Mock<IDbTransaction>();
            this.connection.Setup(x => x.BeginTransaction(IsolationLevel.Serializable)).Returns(transaction.Object);

            var command = new Mock<IDbCommand>();
            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

            this.versionProvider.UseTransaction = true;

            this.versionProvider.EnsureCreated();

            command.VerifySet(x => x.Transaction = transaction.Object);
            command.VerifySet(x => x.CommandText = "Create Table SQL");
            command.Verify(x => x.ExecuteNonQuery());

            transaction.Verify(x => x.Commit());
        }

        [Test]
        public void EnsureCreatedRunsCreateTableSqlOutsideATransactionIfConfigured()
        {
            this.versionProvider.SetConnection(this.connection.Object);

            var command = new Mock<IDbCommand>();
            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

            this.versionProvider.UseTransaction = false;

            this.versionProvider.EnsureCreated();

            command.VerifySet(x => x.CommandText = "Create Table SQL");
            command.Verify(x => x.ExecuteNonQuery());
        }

        [Test]
        public void GetCurrentVersionThrowsIfConnectionNotSet()
        {
            Expect(() => this.versionProvider.GetCurrentVersion(), Throws.InvalidOperationException);
        }

        [Test]
        public void GetCurrentVersionRunsInATransactionIfRequested()
        {
            this.versionProvider.SetConnection(this.connection.Object);

            var transaction = new Mock<IDbTransaction>();
            this.connection.Setup(x => x.BeginTransaction(IsolationLevel.Serializable)).Returns(transaction.Object);

            var command = new Mock<IDbCommand>();
            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

            command.Setup(x => x.ExecuteScalar()).Returns(4).Verifiable();

            this.versionProvider.UseTransaction = true;
            this.versionProvider.GetCurrentVersion();

            transaction.Verify(x => x.Commit());
            command.VerifySet(x => x.CommandText = "Get Current Version SQL");
            command.VerifySet(x => x.Transaction = transaction.Object);
            command.VerifyAll();
        }

        [Test]
        public void GetCurrentVersionReturnsTheCurrentVersion()
        {
            this.versionProvider.SetConnection(this.connection.Object);

            var command = new Mock<IDbCommand>();
            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

            command.Setup(x => x.ExecuteScalar()).Returns((object)4);

            this.versionProvider.UseTransaction = false;
            long version = this.versionProvider.GetCurrentVersion();

            Expect(version, EqualTo(4));
            command.VerifySet(x => x.CommandText = "Get Current Version SQL");
        }

        [Test]
        public void GetCurrentVersionReturnsZeroIfCommandReturnsNull()
        {
            this.versionProvider.SetConnection(this.connection.Object);

            var command = new Mock<IDbCommand>();
            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

            command.Setup(x => x.ExecuteScalar()).Returns(null).Verifiable();

            this.versionProvider.UseTransaction = false;
            long version = this.versionProvider.GetCurrentVersion();

            Expect(version, EqualTo(0));
            command.Verify();
        }

        [Test]
        public void GetCurrentVersionThrowsMigrationExceptionIfResultCannotBeConvertedToLong()
        {
            this.versionProvider.SetConnection(this.connection.Object);

            var command = new Mock<IDbCommand>();
            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

            command.Setup(x => x.ExecuteScalar()).Returns("not a number");

            this.versionProvider.UseTransaction = false;
            Expect(() => this.versionProvider.GetCurrentVersion(), Throws.InstanceOf<MigrationException>());
        }

        [Test]
        public void UpdateVersionThrowsIfConnectionNotSet()
        {
            Expect(() => this.versionProvider.UpdateVersion(1, 2, "Test"), Throws.InvalidOperationException);
        }

        [Test]
        public void UpdateVersionUpdatesVersion()
        {
            this.versionProvider.SetConnection(this.connection.Object);

            var command = new Mock<IDbCommand>();
            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

            var sequence = new MockSequence();
            var param1 = new Mock<IDbDataParameter>();
            var param2 = new Mock<IDbDataParameter>();
            command.InSequence(sequence).Setup(x => x.CreateParameter()).Returns(param1.Object);
            command.InSequence(sequence).Setup(x => x.CreateParameter()).Returns(param2.Object);

            var commandParameters = new Mock<IDataParameterCollection>();
            command.Setup(x => x.Parameters).Returns(commandParameters.Object);

            this.versionProvider.UseTransaction = true;
            this.versionProvider.UpdateVersion(2, 3, "Test Description");

            command.VerifySet(x => x.CommandText = "Set Version SQL");
            command.Verify(x => x.ExecuteNonQuery());

            commandParameters.Verify(x => x.Add(param1.Object));
            commandParameters.Verify(x => x.Add(param2.Object));

            param1.VerifySet(x => x.ParameterName = "Version");
            // Since it's boxed, == doesn't work
            param1.VerifySet(x => x.Value = It.Is<object>(p => (p is long) && (long)p == 3));

            param2.VerifySet(x => x.ParameterName = "Description");
            param2.VerifySet(x => x.Value = "Test Description");
        }
    }
}
