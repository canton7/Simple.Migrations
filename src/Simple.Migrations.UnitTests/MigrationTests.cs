using Moq;
using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.UnitTests
{
    [TestFixture]
    public class MigrationTests : AssertionHelper
    {
        private Mock<Migration> migration;
        private Mock<IMigrationLogger> logger;
        private Mock<DbConnection> connection;

        [SetUp]
        public void SetUp()
        {
            this.logger = new Mock<IMigrationLogger>();
            this.connection = new Mock<DbConnection>();

            this.migration = new Mock<Migration>()
            {
                CallBase = true,
            };
            //this.migration.Object.Logger = this.logger.Object;
            //this.migration.Object.DB = this.connection.Object;
        }

        //[Test]
        //public void ExecuteThrowsIfDbIsNull()
        //{
        //    this.migration.Object.DB = null;
        //    Expect(() => this.migration.Object.Execute("foo"), Throws.InvalidOperationException);
        //}

        //[Test]
        //public void ThrowsIfLoggerIsNull()
        //{
        //    this.migration.Object.Logger = null;
        //    Expect(() => this.migration.Object.Execute("foo"), Throws.InvalidOperationException);
        //}

        //[Test]
        //public void ExecuteLogsSql()
        //{
        //    this.connection.Setup(x => x.CreateCommand()).Returns(new Mock<IDbCommand>().Object);

        //    this.migration.Object.Execute("some sql");

        //    this.logger.Verify(x => x.LogSql("some sql"));
        //}

        //[Test]
        //public void ExecuteExecutesCommand()
        //{
        //    var command = new Mock<IDbCommand>();
        //    this.connection.Setup(x => x.CreateCommand()).Returns(command.Object);

        //    this.migration.Object.Execute("some sql");

        //    command.VerifySet(x => x.CommandText = "some sql");
        //    command.Verify(x => x.ExecuteNonQuery());
        //}
    }
}
