using System;

namespace SimpleMigrations.VersionProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an SQLite database
    /// </summary>
    public class SqliteVersionProvider : VersionProviderBase
    {
        /// <summary>
        /// Gets or sets the name of the table to use. Defaults to 'VersionInfo'
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SqliteVersionProvider"/> class
        /// </summary>
        public SqliteVersionProvider()
        {
            this.TableName = "VersionInfo";
        }

        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        public override string GetCreateVersionTableSql()
        {
            return String.Format(
                @"CREATE TABLE IF NOT EXISTS {0} (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Version INTEGER NOT NULL,
                    AppliedOn DATETIME NOT NULL,
                    Description TEXT NOT NULL
                )", this.TableName);
        }

        /// <summary>
        /// Returns SQL to fetch the current version from the version table
        /// </summary>
        /// <returns>SQL to fetch the current version from the version table</returns>
        public override string GetCurrentVersionSql()
        {
            return String.Format(@"SELECT Version FROM {0} ORDER BY Id DESC LIMIT 1", this.TableName);
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        public override string GetSetVersionSql()
        {
            return String.Format(@"INSERT INTO {0} (Version, AppliedOn, Description) VALUES (@Version, datetime('now', 'localtime'), @Description)", this.TableName);
        }
    }
}
