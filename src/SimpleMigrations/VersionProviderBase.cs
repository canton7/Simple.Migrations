using System;
using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Version provider which acts by maintaining a table of applied versions
    /// </summary>
    public abstract class VersionProviderBase : IVersionProvider<IDbConnection>
    {
        /// <summary>
        /// Ensure that the version table exists, creating it if necessary
        /// </summary>
        /// <param name="connection">Connection to use to perform this action</param>
        public void EnsureCreated(IDbConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = this.GetCreateVersionTableSql();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Return the current version from the version table
        /// </summary>
        /// <param name="connection">Connection to use to perform this action</param>
        /// <returns>Current version</returns>
        public long GetCurrentVersion(IDbConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = this.GetCurrentVersionSql();
                var result = cmd.ExecuteScalar();

                if (result == null)
                    return 0;

                long version;
                try
                {
                    version = Convert.ToInt64(result);
                }
                catch
                {
                    throw new MigrationException("Version Provider returns a value for the current version which isn't a long");
                }   

                return version;
            }
        }

        /// <summary>
        /// Upgrade the current version in the version table
        /// </summary>
        /// <param name="connection">Connection to use to perform this action</param>
        /// <param name="oldVersion">Version being upgraded from</param>
        /// <param name="newVersion">Version being upgraded to</param>
        /// <param name="newDescription">Description to associate with the new version</para
        public void UpgradeVersion(IDbConnection connection, long oldVersion, long newVersion, string newDescription)
        {
            this.SetVersionTo(connection, newVersion, newDescription);
        }

        /// <summary>
        /// Downgrade the current version in the version table
        /// </summary>
        /// <param name="connection">Connection to use to perform this action</param>
        /// <param name="oldVersion">Version being upgraded from</param>
        /// <param name="newVersion">Version being upgraded to</param>
        /// <param name="newDescription">Description to associate with the new version</param>
        public void DowngradeVersion(IDbConnection connection, long oldVersion, long newVersion, string newDescription)
        {
            this.SetVersionTo(connection, newVersion, newDescription);
        }

        private void SetVersionTo(IDbConnection connection, long version, string description)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = this.GetSetVersionSql();

                var versionParam = cmd.CreateParameter();
                versionParam.ParameterName = "Version";
                versionParam.Value = version;
                cmd.Parameters.Add(versionParam);

                var nameParam = cmd.CreateParameter();
                nameParam.ParameterName = "Description";
                nameParam.Value = description;
                cmd.Parameters.Add(nameParam);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Should 'CREATE TABLE IF NOT EXISTS', or similar
        /// </summary>
        public abstract string GetCreateVersionTableSql();

        /// <summary>
        /// Should return SQL which selects a single long value - the current version - or 0/NULL if there is no current version
        /// </summary>
        /// <returns></returns>
        public abstract string GetCurrentVersionSql();

        /// <summary>
        /// Returns SQL which upgrades to a particular version.
        /// </summary>
        /// <remarks>
        /// The following parameters should be used:
        ///  - @Version - the long version to set
        ///  - @Description - the description of the version
        /// </remarks>
        /// <returns></returns>
        public abstract string GetSetVersionSql();
    }
}
