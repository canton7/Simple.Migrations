﻿using System;

namespace SimpleMigrations.VersionProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an SQLite database
    /// </summary>
    public class MsSqlVersionProvider : VersionProviderBase
    {
        /// <summary>
        /// Gets or sets the name of the table to use. Defaults to 'VersionInfo'
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SQLiteVersionProvider"/> class
        /// </summary>
        public MsSqlVersionProvider()
        {
            this.TableName = "Version";
        }

        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        public override string GetCreateVersionTableSql()
        {
            return string.Format(
                @"IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[{0}]') AND type in (N'U'))
                BEGIN
                CREATE TABLE [dbo].[{0}](
	                [Id] [int] IDENTITY(1,1)  PRIMARY KEY NOT NULL,
	                [Version] [int] NOT NULL,
	                [AppliedOn] [datetime] NOT NULL,
	                [Description] [nvarchar](128) NOT NULL,
                )
                END;", TableName);
        }

        /// <summary>
        /// Returns SQL to fetch the current version from the version table
        /// </summary>
        /// <returns>SQL to fetch the current version from the version table</returns>
        public override string GetCurrentVersionSql()
        {
            return string.Format(@"SELECT TOP 1 [Version] FROM [dbo].[{0}] ORDER BY [Id] desc;", TableName);
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        public override string GetSetVersionSql()
        {
            return string.Format(@"INSERT INTO [dbo].[{0}] ([Version], [AppliedOn], [Description]) VALUES (@Version, GETDATE(), @Description);", TableName);
        }
    }
}
