using System;

namespace SimpleMigrations.VersionProvider
{
    /// <summary>
    /// Class which can read from / write to a version table in an SQLite database
    /// </summary>
    public class MySqlVersionProvider : VersionProviderBase
    {
        /// <summary>
        /// Gets or sets the name of the table to use. Defaults to 'VersionInfo'
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SQLiteVersionProvider"/> class
        /// </summary>
        public MySqlVersionProvider()
        {
            this.TableName = "VersionInfo";
        }

        /// <summary>
        /// Returns SQL to create the version table
        /// </summary>
        /// <returns>SQL to create the version table</returns>
        public override string GetCreateVersionTableSql()
        {
            return string.Format(
                @"CREATE TABLE IF NOT EXISTS {0} (
	                Id INT NOT NULL AUTO_INCREMENT,
	                `Version` INT NOT NULL,
	                AppliedOn DATETIME NOT NULL,
                   Description TEXT NOT NULL,
	                PRIMARY KEY (Id)
                )", TableName);
        }

        /// <summary>
        /// Returns SQL to fetch the current version from the version table
        /// </summary>
        /// <returns>SQL to fetch the current version from the version table</returns>
        public override string GetCurrentVersionSql()
        {
            return string.Format(@"SELECT `Version` FROM {0} ORDER BY Id DESC LIMIT 1;", this.TableName);
        }

        /// <summary>
        /// Returns SQL to update the current version in the version table
        /// </summary>
        /// <returns>SQL to update the current version in the version table</returns>
        public override string GetSetVersionSql()
        {
            return string.Format(@"INSERT INTO {0} (`Version`, AppliedOn, Description) VALUES (@Version, NOW(), @Description);", this.TableName);
        }
    }
}
