using Moq;
using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simple.Migrations.UnitTests
{
    [TestFixture]
    public class SimpleMigratorMigrateTests : AssertionHelper
    {
        private class Migration1 : Migration
        {
            public static bool UpCalled;
            public static bool DownCalled;

            public static Exception Exception;
            public static Action<Migration1> Callback;

            public new DbConnection Connection => base.Connection;
            public new IMigrationLogger Logger => base.Logger;

            public static void Reset()
            {
                UpCalled = false;
                DownCalled = false;
                Exception = null;
                Callback = null;
            }

            public override void Up()
            {
                UpCalled = true;
                Callback?.Invoke(this);
                if (Exception != null)
                {
                    throw Exception;
                }
            }

            public override void Down()
            {
                DownCalled = true;
                Callback?.Invoke(this);
                if (Exception != null)
                {
                    throw Exception;
                }
            }
        }

        private class Migration2 : Migration
        {
            public static bool UpCalled;
            public static bool DownCalled;

            public static void Reset()
            {
                UpCalled = false;
                DownCalled = false;
            }

            public override void Down() { DownCalled = true; }
            public override void Up() { UpCalled = true; }
        }

        private Mock<DbConnection> connection;
        private Mock<IMigrationProvider> migrationProvider;
        private Mock<MockDatabaseProvider> databaseProvider;
        private Mock<ILogger> logger;

        private List<MigrationData> migrations;

        private SimpleMigrator<DbConnection, Migration> migrator;

        [SetUp]
        public void SetUp()
        {
            Migration1.Reset();
            Migration2.Reset();

            this.connection = new Mock<DbConnection>()
            {
                DefaultValue = DefaultValue.Mock,
            };
            this.migrationProvider = new Mock<IMigrationProvider>();
            this.databaseProvider = new Mock<MockDatabaseProvider>()
            {
                CallBase = true,
            };
            this.databaseProvider.Object.Connection = this.connection.Object;

            this.logger = new Mock<ILogger>();

            this.migrator = new SimpleMigrator<DbConnection, Migration>(this.migrationProvider.Object, this.databaseProvider.Object, this.logger.Object);

            this.migrations = new List<MigrationData>()
            {
                new MigrationData(1, "Migration 1", typeof(Migration1).GetTypeInfo()),
                new MigrationData(2, "Migration 2", typeof(Migration2).GetTypeInfo()),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(this.migrations);
        }

        [Test]
        public void BaselineThrowsIfMigrationsHaveAlreadyBeenApplied()
        {
            this.LoadMigrator(1);

            Expect(() => this.migrator.Baseline(1), Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void BaselineThrowsIfRequestedVersionDoesNotExist()
        {
            this.databaseProvider.Object.CurrentVersion = 0;
            this.migrator.Load();

            Expect(() => this.migrator.Baseline(3), Throws.ArgumentException.With.Property("ParamName").EqualTo("version"));
        }

        [Test]
        public void BaselineBaselinesDatabase()
        {
            this.LoadMigrator(0);

            this.migrator.Baseline(1);

            this.databaseProvider.Verify(x => x.UpdateVersion(0, 1, "Migration1 (Migration 1)"));
            Expect(this.migrator.CurrentMigration, EqualTo(this.migrations[0]));
        }

        [Test]
        public void MigrateToThrowsIfVersionNotFound()
        {
            this.LoadMigrator(0);

            Expect(() => this.migrator.MigrateTo(3), Throws.ArgumentException.With.Property("ParamName").EqualTo("newVersion"));
        }

        [Test]
        public void MigrateToDoesNothingIfThereIsNothingToDo()
        {
            this.LoadMigrator(1);

            this.migrator.MigrateTo(1);

            Expect(Migration1.UpCalled, False);
            Expect(Migration1.DownCalled, False);
            Expect(Migration2.UpCalled, False);
            Expect(Migration2.DownCalled, False);
            this.databaseProvider.Verify(x => x.UpdateVersion(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void MigrateToMigratesUpCorrectly()
        {
            this.LoadMigrator(0);

            int sequence = 0;
            this.databaseProvider.Setup(x => x.UpdateVersion(0, 1, "Migration1 (Migration 1)")).Callback(() =>
            {
                Expect(sequence++, EqualTo(0));
                Expect(Migration1.UpCalled, True);
                Expect(Migration2.UpCalled, False);
            }).Verifiable();
            this.databaseProvider.Setup(x => x.UpdateVersion(1, 2, "Migration2 (Migration 2)")).Callback(() =>
            {
                Expect(sequence++, EqualTo(1));
                Expect(Migration2.UpCalled, True);
            }).Verifiable();

            this.migrator.MigrateTo(2);

            this.databaseProvider.VerifyAll();

            Expect(Migration1.DownCalled, False);
            Expect(Migration2.DownCalled, False);
        }

        [Test]
        public void MigrateToMigratesDownCorrectly()
        {
            this.LoadMigrator(2);

            int sequence = 0;
            this.databaseProvider.Setup(x => x.UpdateVersion(2, 1, "Migration1 (Migration 1)")).Callback(() =>
            {
                Expect(sequence++, EqualTo(0));
                Expect(Migration2.DownCalled, True);
                Expect(Migration1.DownCalled, False);
            }).Verifiable();
            this.databaseProvider.Setup(x => x.UpdateVersion(1, 0, "Empty Schema")).Callback(() =>
            {
                Expect(sequence++, EqualTo(1));
                Expect(Migration1.DownCalled, True);
            }).Verifiable();

            this.migrator.MigrateTo(0);

            this.databaseProvider.VerifyAll();

            Expect(Migration1.UpCalled, False);
            Expect(Migration2.UpCalled, False);
        }

        [Test]
        public void MigrateToPropagatesExceptionFromMigration()
        {
            this.LoadMigrator(0);

            var e = new Exception("BOOM");
            Migration1.Exception = e;

            Expect(() => this.migrator.MigrateTo(2), Throws.Exception.EqualTo(e));

            this.databaseProvider.Verify(x => x.UpdateVersion(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()), Times.Never());
            Expect(this.migrator.CurrentMigration.Version, EqualTo(0));
            Expect(Migration1.DownCalled, False);
        }

        [Test]
        public void MigrateToAssignsConnectionAndLogger()
        {
            this.LoadMigrator(0);

            Migration1.Callback = x =>
            {
                Expect(x.Connection, EqualTo(this.connection.Object));
                Expect(x.Logger, EqualTo(this.logger.Object));
            };

            this.migrator.MigrateTo(1);
        }

        private void LoadMigrator(long currentVersion)
        {
            this.databaseProvider.Object.CurrentVersion = currentVersion;
            this.migrator.Load();
        }
    }
}
