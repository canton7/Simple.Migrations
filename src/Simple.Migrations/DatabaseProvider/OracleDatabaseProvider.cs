using System;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an PostgreSQL database
    /// </summary>
    /// <remarks>
    /// PostgreSQL supports advisory locks, so these are used to guard against concurrent migrators.
    /// </remarks>
    public class OracleDatabaseProvider : DatabaseProviderBaseWithVersionTableLock
    {
        /// <summary>
        /// Name of Oracle Sequence for generating Id in version table
        /// 
        /// </summary>
        public string VersionSequenceName { get; set; } = "SEQ_VERSION_TABLE";
        
        /// <summary>
        /// Controls whether or not to try and create the schema if it does not exist.
        /// </summary>
        /// <remarks>
        /// If this is set to false then no schema is created. It is the user's responsibility to create the schema
        /// (if necessary) with the correct name and permissions before running the <see cref="SimpleMigrator"/>. This may be
        /// required if the user which Simple.Migrator is running as does not have the correct permissions to check whether the
        /// schema has been created.
        /// </remarks>
        public bool CreateSchema { get; set; } = false;
        

        /// <summary>
        /// Initialises a new instance of the <see cref="OracleDatabaseProvider"/> class
        /// </summary>
        /// <param name="connectionFactory">Connection to use to run migrations. The caller is responsible for closing this.</param>
        public OracleDatabaseProvider(Func<DbConnection> connectionFactory)
            : base(connectionFactory)
        {
            TableName = "VERSION_INFO";
        }


        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        protected override string GetCreateVersionTableSql()
        {
            return $@"DECLARE
    PROCEDURE CREATE_IF_NOT_EXISTS(p_object VARCHAR2, p_sql VARCHAR2)
    IS
    c_object user_objects.object_name%type;
    BEGIN
        BEGIN
          SELECT object_name INTO c_object FROM user_objects WHERE object_name=p_object;
          EXCEPTION WHEN no_data_found then          
          BEGIN 
              EXECUTE IMMEDIATE p_sql;        
           EXCEPTION WHEN OTHERS THEN 
            IF (SQLCODE = -955) THEN 
                NULL;
             ELSE
                RAISE;
             END IF;   
        END;
    END;
    END;
BEGIN        
       CREATE_IF_NOT_EXISTS('{this.VersionSequenceName}','CREATE SEQUENCE {this.VersionSequenceName} START WITH 1 NOCACHE');
    
       CREATE_IF_NOT_EXISTS('{this.TableName}','CREATE TABLE {this.TableName}(
        ID INTEGER PRIMARY KEY,
        VERSION INTEGER NOT NULL,
        APPLIED_ON timestamp with time zone,
        DESCRIPTION varchar2(4000) NOT NULL
        )');                  
END;
";
        }

        


        /// <summary>
        /// Returns SQL to fetch the current version from the version table
        /// </summary>
        /// <returns>SQL to fetch the current version from the version table</returns>
        protected override string GetCurrentVersionSql()
        {
            return $@"select version from (select version from {this.TableName} order by id desc) where rownum=1";
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        protected override string GetSetVersionSql()
        {
            return $@"INSERT INTO {this.TableName} (ID, VERSION, APPLIED_ON, DESCRIPTION) VALUES ({this.VersionSequenceName}.NEXTVAL, :Version, CURRENT_TIMESTAMP, :Description)";
        }

        protected override void AcquireVersionTableLock()
        {
            VersionTableLockTransaction = this.VersionTableConnection.BeginTransaction();
            using (var cmd = this.VersionTableConnection.CreateCommand())
            {
                cmd.Transaction = VersionTableLockTransaction;
                cmd.CommandText = $"LOCK TABLE {TableName} IN EXCLUSIVE MODE";
                cmd.ExecuteNonQuery();
            }
        }

        protected override void ReleaseVersionTableLock()
        {
            VersionTableLockTransaction.Commit();
            VersionTableLockTransaction.Dispose();
            VersionTableLockTransaction = null;
        }
    }
}
