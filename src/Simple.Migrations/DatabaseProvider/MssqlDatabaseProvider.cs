using System;
using System.Data;
using System.Data.Common;

namespace SimpleMigrations.DatabaseProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an MSSQL database
    /// </summary>
    /// <remarks>
    /// MSSQL supports advisory locks, so these are used to guard against concurrent migrators.
    /// </remarks>
    public class MssqlDatabaseProvider : DatabaseProviderBaseWithAdvisoryLock
    {
        /// <summary>
        /// Gets or sets the schema name used to store the version table.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        /// <summary>
        /// Gets or sets the name of the advisory lock to acquire
        /// </summary>
        public string LockName { get; set; } = "SimpleMigratorExclusiveLock";

        /// <summary>
        /// Initialises a new instance of the <see cref="MssqlDatabaseProvider"/> class
        /// </summary>
        /// <param name="connection">Connection to use to run migrations. The caller is responsible for closing this.</param>
        public MssqlDatabaseProvider(DbConnection connection)
            : base(connection)
        {
            this.MaxDescriptionLength = 256;
        }

        /// <summary>
        /// Acquires an advisory lock using Connection
        /// </summary>
        public override void AcquireAdvisoryLock()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"sp_getapplock @Resource = '{this.LockName}', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = '{(int)this.LockTimeout.TotalMilliseconds}'";
                command.CommandTimeout = 0; // The lock will time out by itself
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Releases the advisory lock held on Connection
        /// </summary>
        public override void ReleaseAdvisoryLock()
        {
            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = $"sp_releaseapplock @Resource = '{this.LockName}', @LockOwner = 'Session'";
                command.ExecuteNonQuery();
            }
        }
        
        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        public override string GetCreateVersionTableSql()
        {
            return $@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[{this.SchemaName}].[{this.TableName}]') AND type in (N'U'))
                BEGIN
                CREATE TABLE [{this.SchemaName}].[{this.TableName}](
                    [Id] [int] IDENTITY(1,1) PRIMARY KEY NOT NULL,
                    [Version] [bigint] NOT NULL,
                    [AppliedOn] [datetime] NOT NULL,
                    [Description] [nvarchar]({this.MaxDescriptionLength}) NOT NULL,
                )
                END;";
        }

        /// <summary>
        /// Returns SQL to fetch the current version from the version table
        /// </summary>
        /// <returns>SQL to fetch the current version from the version table</returns>
        public override string GetCurrentVersionSql()
        {
            return $@"SELECT TOP 1 [Version] FROM [{this.SchemaName}].[{this.TableName}] ORDER BY [Id] desc;";
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        public override string GetSetVersionSql()
        {
            return $@"INSERT INTO [{this.SchemaName}].[{this.TableName}] ([Version], [AppliedOn], [Description]) VALUES (@Version, GETDATE(), @Description);";
        }
    }
}
