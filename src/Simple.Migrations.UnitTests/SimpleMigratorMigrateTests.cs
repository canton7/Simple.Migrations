using Moq;
using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simple.Migrations.UnitTests
{
    [TestFixture]
    public class SimpleMigratorMigrateTests : AssertionHelper
    {
        private class Migration1 : Migration<ITransactionAwareDbConnection>
        {
            public static bool UpCalled;
            public static bool DownCalled;

            public static Exception Exception;
            public static Action<Migration1> Callback;

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

        private class Migration2 : Migration<ITransactionAwareDbConnection>
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

        private Mock<ITransactionAwareDbConnection> connection;
        private Mock<IMigrationProvider> migrationProvider;
        private Mock<IConnectionProvider<ITransactionAwareDbConnection>> connectionProvider;
        private Mock<IVersionProvider<ITransactionAwareDbConnection>> versionProvider;
        private Mock<ILogger> logger;

        private List<MigrationData> migrations;

        private SimpleMigrator<ITransactionAwareDbConnection, Migration<ITransactionAwareDbConnection>> migrator;

        [SetUp]
        public void SetUp()
        {
            Migration1.Reset();
            Migration2.Reset();

            this.connection = new Mock<ITransactionAwareDbConnection>();
            this.migrationProvider = new Mock<IMigrationProvider>();
            this.connectionProvider = new Mock<IConnectionProvider<ITransactionAwareDbConnection>>();
            this.versionProvider = new Mock<IVersionProvider<ITransactionAwareDbConnection>>();
            this.logger = new Mock<ILogger>();

            this.connectionProvider.Setup(x => x.Connection).Returns(this.connection.Object);

            this.migrator = new SimpleMigrator<ITransactionAwareDbConnection, Migration<ITransactionAwareDbConnection>>(this.migrationProvider.Object, this.connectionProvider.Object, this.versionProvider.Object, this.logger.Object);

            this.migrations = new List<MigrationData>()
            {
                new MigrationData(1, "Migration 1", typeof(Migration1).GetTypeInfo(), true),
                new MigrationData(2, "Migration 2", typeof(Migration2).GetTypeInfo(), false),
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
            this.versionProvider.Setup(x => x.GetCurrentVersion()).Returns(0);
            this.migrator.Load();

            Expect(() => this.migrator.Baseline(3), Throws.ArgumentException.With.Property("ParamName").EqualTo("version"));
        }

        [Test]
        public void BaselineBaselinesDatabase()
        {
            this.LoadMigrator(0);

            this.migrator.Baseline(1);

            this.versionProvider.Verify(x => x.UpdateVersion(0, 1, "Migration1 (Migration 1)"));
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
            this.versionProvider.Verify(x => x.UpdateVersion(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void MigrateToMigratesUpCorrectly()
        {
            this.LoadMigrator(0);

            int sequence = 0;
            this.versionProvider.Setup(x => x.UpdateVersion(0, 1, "Migration1 (Migration 1)")).Callback(() =>
            {
                Expect(sequence++, EqualTo(0));
                Expect(Migration1.UpCalled, True);
                Expect(Migration2.UpCalled, False);
            }).Verifiable();
            this.versionProvider.Setup(x => x.UpdateVersion(1, 2, "Migration2 (Migration 2)")).Callback(() =>
            {
                Expect(sequence++, EqualTo(1));
                Expect(Migration2.UpCalled, True);
            }).Verifiable();

            this.migrator.MigrateTo(2);

            this.versionProvider.VerifyAll();

            Expect(Migration1.DownCalled, False);
            Expect(Migration2.DownCalled, False);
        }

        [Test]
        public void MigrateToMigratesDownCorrectly()
        {
            this.LoadMigrator(2);

            int sequence = 0;
            this.versionProvider.Setup(x => x.UpdateVersion(2, 1, "Migration1 (Migration 1)")).Callback(() =>
            {
                Expect(sequence++, EqualTo(0));
                Expect(Migration2.DownCalled, True);
                Expect(Migration1.DownCalled, False);
            }).Verifiable();
            this.versionProvider.Setup(x => x.UpdateVersion(1, 0, "Empty Schema")).Callback(() =>
            {
                Expect(sequence++, EqualTo(1));
                Expect(Migration1.DownCalled, True);
            }).Verifiable();

            this.migrator.MigrateTo(0);

            this.versionProvider.VerifyAll();

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

            this.versionProvider.Verify(x => x.UpdateVersion(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()), Times.Never());
            Expect(this.migrator.CurrentMigration.Version, EqualTo(0));
            Expect(Migration1.DownCalled, False);
        }

        [Test]
        public void MigrateToRunsMigrationInTransaction()
        {
            this.LoadMigrator(0);
            int sequence = 0;

            this.connectionProvider.Setup(x => x.BeginTransaction()).Callback(() => Expect(sequence++, EqualTo(0))).Verifiable();
            Migration1.Callback = _ => Expect(sequence++, EqualTo(1));
            this.versionProvider.Setup(x => x.UpdateVersion(0, 1, "Migration1 (Migration 1)")).Callback(() => Expect(sequence++, EqualTo(2))).Verifiable();
            this.connectionProvider.Setup(x => x.CommitTransaction()).Callback(() => Expect(sequence++, EqualTo(3))).Verifiable();

            this.migrator.MigrateTo(1);

            this.connectionProvider.VerifyAll();
            this.versionProvider.VerifyAll();
        }

        [Test]
        public void MigrateToRollsBackTransactionIfMigrationFails()
        {
            this.LoadMigrator(0);
            int sequence = 0;

            this.connectionProvider.Setup(x => x.BeginTransaction()).Callback(() => Expect(sequence++, EqualTo(0))).Verifiable();
            Migration1.Exception = new Exception("BOOM");
            this.connectionProvider.Setup(x => x.RollbackTransaction()).Callback(() => Expect(sequence++, EqualTo(1))).Verifiable();

            Expect(() => this.migrator.MigrateTo(1), Throws.Exception);

            this.connectionProvider.VerifyAll();
        }

        [Test]
        public void MigrateToDoesNotRunMigrationInTransactionIfNotRequested()
        {
            this.LoadMigrator(1);

            this.migrator.MigrateTo(1);

            this.connectionProvider.Verify(x => x.BeginTransaction(), Times.Never());
            this.connectionProvider.Verify(x => x.CommitTransaction(), Times.Never());
        }

        [Test]
        public void MigrateToAssignsDBAndLogger()
        {
            this.LoadMigrator(0);

            Migration1.Callback = x =>
            {
                Expect(x.DB, EqualTo(this.connection.Object));
                Expect(x.Logger, EqualTo(this.logger.Object));
            };

            this.migrator.MigrateTo(1);
        }

        private void LoadMigrator(long currentVersion)
        {
            this.versionProvider.Setup(x => x.GetCurrentVersion()).Returns(currentVersion);
            this.migrator.Load();
        }
    }
}
