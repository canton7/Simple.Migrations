using System.Data.Common;
using Npgsql;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using Simple.Migrations.IntegrationTests.Postgresql;
using SimpleMigrations;
using SimpleMigrations.DatabaseProvider;

namespace Simple.Migrations.IntegrationTests.Oracle
{
    [TestFixture]
    public class OracleTests : TestsBase
    {
        protected override IDatabaseProvider<DbConnection> CreateDatabaseProvider() => new OracleDatabaseProvider(this.CreateConnection);

        protected override IMigrationStringsProvider MigrationStringsProvider { get; } = new OracleStringsProvider();

        protected override bool SupportConcurrentMigrators => true;

        protected override void Clean()
        {
            var connection = this.CreateOraConnection();
            connection.Open();

            using (var cmd = new OracleCommand(@"
BEGIN
    FOR CURTAB IN (SELECT TABLE_NAME FROM USER_TABLES) LOOP
        BEGIN 
                EXECUTE IMMEDIATE 'DROP TABLE '||curtab.table_name||' PURGE';
        EXCEPTION WHEN OTHERS THEN
            IF (SQLCODE = -942) THEN 
                NULL;
            ELSE
                RAISE;
            END IF;
        END;
    END LOOP;

    BEGIN 
        EXECUTE IMMEDIATE 'DROP SEQUENCE SEQ_VERSION_TABLE';
    EXCEPTION WHEN OTHERS THEN
            IF (SQLCODE = -2289) THEN 
                NULL;
            ELSE
                RAISE;
            END IF;
    END;
END;
", connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private OracleConnection CreateOraConnection() => new OracleConnection(ConnectionStrings.Oracle);

        protected override DbConnection CreateConnection() => this.CreateOraConnection();
    }
}
