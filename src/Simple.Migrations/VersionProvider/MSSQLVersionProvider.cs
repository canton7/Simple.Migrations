namespace SimpleMigrations.VersionProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an SQLite database
    /// </summary>
    public class MSSQLVersionProvider : VersionProviderBase
    {
        /// <summary>
        /// Gets or sets the name of the table to use. Defaults to 'VersionInfo'
        /// </summary>
        public string TableName { get; set; } = DefaultTableName;

        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        public override string GetCreateVersionTableSql()
        {
            return $@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[{this.TableName}]') AND type in (N'U'))
                BEGIN
                CREATE TABLE [dbo].[{this.TableName}](
                    [Id] [int] IDENTITY(1,1)  PRIMARY KEY NOT NULL,
                    [Version] [int] NOT NULL,
                    [AppliedOn] [datetime] NOT NULL,
                    [Description] [nvarchar](128) NOT NULL,
                )
                END;";
        }

        /// <summary>
        /// Returns SQL to fetch the current version from the version table
        /// </summary>
        /// <returns>SQL to fetch the current version from the version table</returns>
        public override string GetCurrentVersionSql()
        {
            return $@"SELECT TOP 1 [Version] FROM [dbo].[{this.TableName}] ORDER BY [Id] desc;";
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        public override string GetSetVersionSql()
        {
            return $@"INSERT INTO [dbo].[{this.TableName}] ([Version], [AppliedOn], [Description]) VALUES (@Version, GETDATE(), @Description);";
        }
    }
}
