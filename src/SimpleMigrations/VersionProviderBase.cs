using System;
using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Version provider which acts by maintaining a table of applied versions
    /// </summary>
    public abstract class VersionProviderBase : IVersionProvider<IDbConnection>
    {
        public void EnsureCreated(IDbConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = this.GetCreateVersionTableSql();
                cmd.ExecuteNonQuery();
            }
        }

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

        public void UpgradeVersion(IDbConnection connection, long oldVersion, long newVersion, string newDescription)
        {
            this.SetVersionTo(connection, newVersion, newDescription);
        }

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
