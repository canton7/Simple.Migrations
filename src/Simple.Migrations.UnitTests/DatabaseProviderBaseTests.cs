//using Moq;
//using NUnit.Framework;
//using SimpleMigrations;
//using SimpleMigrations.DatabaseProvider;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.Common;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Simple.Migrations.UnitTests
//{
//    [TestFixture]
//    public class DatabaseProviderBaseTests : AssertionHelper
//    {
//        private class DatabaseProvider : DatabaseProviderBase
//        {
//            public new int MaxDescriptionLength
//            {
//                get { return base.MaxDescriptionLength; }
//                set { base.MaxDescriptionLength = value; }
//            }

//            public string CreateTableSql = "Create Table SQL";
//            public override string GetCreateVersionTableSql() => this.CreateTableSql;

//            public string CurrentVersionSql = "Get Current Version SQL";
//            public override string GetCurrentVersionSql() => this.CurrentVersionSql;

//            public string SetVersionSql = "Set Version SQL";
//            public override string GetSetVersionSql() => this.SetVersionSql;

//            public override IDbConnection BeginOperation()
//            {
//                throw new NotImplementedException();
//            }

//            public override void EndOperation()
//            {
//                throw new NotImplementedException();
//            }

//            public override long EnsureCreatedAndGetCurrentVersion()
//            {
//                throw new NotImplementedException();
//            }

//            public override long GetCurrentVersion()
//            {
//                throw new NotImplementedException();
//            }

//            public override void UpdateVersion(long oldVersion, long newVersion, string newDescription)
//            {
//                throw new NotImplementedException();
//            }
//        }

//        private DatabaseProvider databaseProvider;
//        private Mock<DbConnection> connection;

//        [SetUp]
//        public void SetUp()
//        {
//            this.databaseProvider = new DatabaseProvider();

//            this.connection = new Mock<DbConnection>();
//        }

//        [Test]
//        public void GetCurrentVersionReturnsTheCurrentVersion()
//        {
//            var command = new Mock<DbCommand>();
//            this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

//            command.Setup(x => x.ExecuteScalar()).Returns((object)4);

//            long version = this.databaseProvider.GetCurrentVersion();

//            Expect(version, EqualTo(4));
//            command.VerifySet(x => x.CommandText = "Get Current Version SQL");
//        }

//        //[Test]
//        //public void GetCurrentVersionReturnsZeroIfCommandReturnsNull()
//        //{
//        //    this.databaseProvider.SetConnection(this.connection.Object);

//        //    var command = new Mock<IDbCommand>();
//        //    this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

//        //    command.Setup(x => x.ExecuteScalar()).Returns(null).Verifiable();

//        //    this.databaseProvider.UseTransaction = false;
//        //    long version = this.databaseProvider.GetCurrentVersion();

//        //    Expect(version, EqualTo(0));
//        //    command.Verify();
//        //}

//        //[Test]
//        //public void GetCurrentVersionThrowsMigrationExceptionIfResultCannotBeConvertedToLong()
//        //{
//        //    this.databaseProvider.SetConnection(this.connection.Object);

//        //    var command = new Mock<IDbCommand>();
//        //    this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

//        //    command.Setup(x => x.ExecuteScalar()).Returns("not a number");

//        //    this.databaseProvider.UseTransaction = false;
//        //    Expect(() => this.databaseProvider.GetCurrentVersion(), Throws.InstanceOf<MigrationException>());
//        //}

//        //[Test]
//        //public void UpdateVersionThrowsIfConnectionNotSet()
//        //{
//        //    Expect(() => this.databaseProvider.UpdateVersion(1, 2, "Test"), Throws.InvalidOperationException);
//        //}

//        //[Test]
//        //public void UpdateVersionUpdatesVersion()
//        //{
//        //    this.databaseProvider.SetConnection(this.connection.Object);

//        //    var command = new Mock<IDbCommand>();
//        //    this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

//        //    var sequence = new MockSequence();
//        //    var param1 = new Mock<IDbDataParameter>();
//        //    var param2 = new Mock<IDbDataParameter>();
//        //    command.InSequence(sequence).Setup(x => x.CreateParameter()).Returns(param1.Object);
//        //    command.InSequence(sequence).Setup(x => x.CreateParameter()).Returns(param2.Object);

//        //    var commandParameters = new Mock<IDataParameterCollection>();
//        //    command.Setup(x => x.Parameters).Returns(commandParameters.Object);

//        //    this.databaseProvider.UseTransaction = true;
//        //    this.databaseProvider.UpdateVersion(2, 3, "Test Description");

//        //    command.VerifySet(x => x.CommandText = "Set Version SQL");
//        //    command.Verify(x => x.ExecuteNonQuery());

//        //    commandParameters.Verify(x => x.Add(param1.Object));
//        //    commandParameters.Verify(x => x.Add(param2.Object));

//        //    param1.VerifySet(x => x.ParameterName = "Version");
//        //    // Since it's boxed, == doesn't work
//        //    param1.VerifySet(x => x.Value = It.Is<object>(p => (p is long) && (long)p == 3));

//        //    param2.VerifySet(x => x.ParameterName = "Description");
//        //    param2.VerifySet(x => x.Value = "Test Description");
//        //}

//        //[Test]
//        //public void UpdateVersionTruncatesDescriptionIfNecessary()
//        //{
//        //    this.databaseProvider.SetConnection(this.connection.Object);

//        //    this.databaseProvider.MaxDescriptionLength = 10;

//        //    var command = new Mock<IDbCommand>();
//        //    this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

//        //    var sequence = new MockSequence();
//        //    var param1 = new Mock<IDbDataParameter>();
//        //    var param2 = new Mock<IDbDataParameter>();
//        //    command.InSequence(sequence).Setup(x => x.CreateParameter()).Returns(param1.Object);
//        //    command.InSequence(sequence).Setup(x => x.CreateParameter()).Returns(param2.Object);

//        //    var commandParameters = new Mock<IDataParameterCollection>();
//        //    command.Setup(x => x.Parameters).Returns(commandParameters.Object);

//        //    this.databaseProvider.UpdateVersion(2, 3, "Test Description");

//        //    param2.VerifySet(x => x.Value = "Test De...");
//        //}
//    }
//}
