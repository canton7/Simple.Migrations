using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Base class, intended to be used by all migrations (although you may implement <see cref="IMigration{TDatabase}"/> directly if you wish).
    /// Migrations MUST apply the <see cref="MigrationAttribute"/> attribute
    /// </summary>
    public abstract class Migration : IMigration<IDbConnection>
    {
        /// <summary>
        /// Gets or sets the database to be used by this migration
        /// </summary>
        public IDbConnection Database { get; set; }

        /// <summary>
        /// Gets or sets the logger to be used by this migration
        /// </summary>
        public IMigrationLogger Logger { get; set; }

        /// <summary>
        /// Invoked when this migration should migrate up
        /// </summary>
        public abstract void Up();

        /// <summary>
        /// Invoked when this migration should migrate down
        /// </summary>
        public abstract void Down();

        /// <summary>
        /// Execute and log an SQL query (which returns no data)
        /// </summary>
        /// <param name="sql"></param>
        public virtual void Execute(string sql)
        {
            this.Logger.LogSql(sql);

            using (var command = this.Database.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
    }
}
