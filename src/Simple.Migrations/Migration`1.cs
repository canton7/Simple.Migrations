using System;
using System.Data;
using System.Data.Common;

namespace SimpleMigrations
{
    /// <summary>
    /// Base class, intended to be used by all migrations (although you may implement <see cref="IMigration{TDatabase}"/> directly if you wish).
    /// Migrations MUST apply the <see cref="MigrationAttribute"/> attribute
    /// </summary>
    /// <typeparam name="TConnection">Type of the database connection</typeparam>
    public abstract class Migration<TConnection> : IMigration<TConnection>
        where TConnection : DbConnection
    {
        /// <summary>
        /// Gets or sets the database to be used by this migration
        /// </summary>
        public TConnection Connection { get; private set; }

        /// <summary>
        /// Gets or sets the logger to be used by this migration
        /// </summary>
        public IMigrationLogger Logger { get; private set; }

        public DbTransaction Transaction { get; private set; }

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
        /// <param name="sql">SQL to execute</param>
        public virtual void Execute(string sql)
        {
            if (this.Connection == null)
                throw new InvalidOperationException("this.DB has not yet been set. This should have been set by the SimpleMigrator");
            if (this.Logger == null)
                throw new InvalidOperationException("this.Logger has not yet been set. This should have been set by the SimpleMigrator");

            this.Logger.LogSql(sql);

            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        void IMigration<TConnection>.Execute(TConnection connection, IMigrationLogger logger, MigrationDirection direction)
        {
            this.Connection = connection;
            this.Logger = logger;

            try
            {
                this.Transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                if (direction == MigrationDirection.Up)
                    this.Up();
                else
                    this.Down();

                this.Transaction.Commit();
            }
            catch
            {
                this.Transaction?.Rollback();
                throw;
            }
            finally
            {
                this.Transaction?.Dispose();
            }
        }
    }
}
