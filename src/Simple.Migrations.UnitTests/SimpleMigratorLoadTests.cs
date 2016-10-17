using Moq;
using NUnit.Framework;
using SimpleMigrations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simple.Migrations.UnitTests
{
    [TestFixture]
    public class SimpleMigratorLoadTests : AssertionHelper
    {
        private class MigrationNotDerivedFromBase : Migration<IDbConnection>
        {
            public override void Down() { }
            public override void Up() { }
        }

        private class ValidMigration1 : Migration<ITransactionAwareDbConnection>
        {
            public override void Down() { }
            public override void Up() { }
        }

        private class ValidMigration2 : Migration<ITransactionAwareDbConnection>
        {
            public override void Down() { }
            public override void Up() { }
        }

        private Mock<ITransactionAwareDbConnection> connection;
        private Mock<IMigrationProvider> migrationProvider;
        private Mock<IConnectionProvider<ITransactionAwareDbConnection>> connectionProvider;
        private Mock<IVersionProvider<ITransactionAwareDbConnection>> versionProvider;

        private SimpleMigrator<ITransactionAwareDbConnection, Migration<ITransactionAwareDbConnection>> migrator;

        [SetUp]
        public void SetUp()
        {
            this.connection = new Mock<ITransactionAwareDbConnection>();
            this.migrationProvider = new Mock<IMigrationProvider>();
            this.connectionProvider = new Mock<IConnectionProvider<ITransactionAwareDbConnection>>();
            this.versionProvider = new Mock<IVersionProvider<ITransactionAwareDbConnection>>();

            this.connectionProvider.Setup(x => x.Connection).Returns(this.connection.Object);

            this.migrator = new SimpleMigrator<ITransactionAwareDbConnection, Migration<ITransactionAwareDbConnection>>(this.migrationProvider.Object, this.connectionProvider.Object, this.versionProvider.Object);
        }

        [Test]
        public void LoadThrowsIfMigrationProviderDoesNotFindAnyMigrations()
        {
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns<List<MigrationData>>(null);
            Expect(() => this.migrator.Load(), Throws.InstanceOf<MigrationException>());

            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(new List<MigrationData>());
            Expect(() => this.migrator.Load(), Throws.InstanceOf<MigrationException>());
        }

        [Test]
        public void LoadThrowsIfMigrationProviderProvidesATypeNotDerivedFromTMigration()
        {
            var migrations = new List<MigrationData>()
            {
                new MigrationData(1, "Migration", typeof(MigrationNotDerivedFromBase).GetTypeInfo(), true),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(migrations);

            Expect(() => this.migrator.Load(), Throws.InstanceOf<MigrationException>());
        }

        [Test]
        public void LoadThrowsIfAnyMigrationHasAnInvalidVersion()
        {
            var migrations = new List<MigrationData>()
            {
                new MigrationData(0, "Migration", typeof(ValidMigration1).GetTypeInfo(), true),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(migrations);

            Expect(() => this.migrator.Load(), Throws.InstanceOf<MigrationException>());
        }

        [Test]
        public void LoadThrowsIfTwoMigrationsHaveTheSameVersion()
        {
            var migrations = new List<MigrationData>()
            {
                new MigrationData(2, "Migration", typeof(ValidMigration1).GetTypeInfo(), true),
                new MigrationData(2, "Migration", typeof(ValidMigration2).GetTypeInfo(), true),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(migrations);

            Expect(() => this.migrator.Load(), Throws.InstanceOf<MigrationException>());
        }

        [Test]
        public void LoadCorrectlyLoadsAndSortsMigrations()
        {
            var migrations = new List<MigrationData>()
            {
                new MigrationData(2, "Migration 2", typeof(ValidMigration2).GetTypeInfo(), true),
                new MigrationData(1, "Migration 1", typeof(ValidMigration1).GetTypeInfo(), true),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(migrations);

            this.migrator.Load();

            Expect(this.migrator.Migrations.Count, EqualTo(3));
            Expect(this.migrator.Migrations[0].Version, EqualTo(0));
            Expect(this.migrator.Migrations[1], EqualTo(migrations[1]));
            Expect(this.migrator.Migrations[2], EqualTo(migrations[0]));

            Expect(this.migrator.LatestMigration, EqualTo(migrations[0]));
        }

        [Test]
        public void LoadThrowsIfCurrentMigrationVersionNotFound()
        {
            var migrations = new List<MigrationData>()
            {
                new MigrationData(2, "Migration 1", typeof(ValidMigration1).GetTypeInfo(), true),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(migrations);

            this.versionProvider.Setup(x => x.GetCurrentVersion()).Returns(1);

            Assert.That(() => this.migrator.Load(), Throws.InstanceOf<MigrationException>());
        }

        [Test]
        public void LoadCorrectlyIdentifiesCurrentMigration()
        {
            var migrations = new List<MigrationData>()
            {
                new MigrationData(1, "Migration 1", typeof(ValidMigration1).GetTypeInfo(), true),
                new MigrationData(2, "Migration 2", typeof(ValidMigration2).GetTypeInfo(), true),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(migrations);
            this.versionProvider.Setup(x => x.GetCurrentVersion()).Returns(2);

            this.migrator.Load();

            Expect(this.migrator.CurrentMigration, EqualTo(migrations[1]));
        }

        [Test]
        public void CallingLoadTwiceDoesNothing()
        {
            var migrations = new List<MigrationData>()
            {
                new MigrationData(1, "Migration 1", typeof(ValidMigration1).GetTypeInfo(), true),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(migrations);

            this.migrator.Load();
            this.migrator.Load();

            this.migrationProvider.Verify(x => x.LoadMigrations(), Times.Exactly(1));
        }

        [Test]
        public void LoadCreatesVersionTable()
        {
            var migrations = new List<MigrationData>()
            {
                new MigrationData(1, "Migration 1", typeof(ValidMigration1).GetTypeInfo(), true),
            };
            this.migrationProvider.Setup(x => x.LoadMigrations()).Returns(migrations);

            this.migrator.Load();

            this.versionProvider.Verify(x => x.EnsureCreated());
        }
    }
}
