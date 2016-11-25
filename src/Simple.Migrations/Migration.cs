using System;
using System.Data;
using System.Data.Common;

namespace SimpleMigrations
{
    /// <summary>
    /// Base class, intended to be used by all migrations (although you may implement <see cref="IMigration{TDatabase}"/> directly if you wish).
    /// Migrations MUST apply the <see cref="MigrationAttribute"/> attribute
    /// </summary>
    public abstract class Migration : IMigration<DbConnection>
    {
        /// <summary>
        /// Gets or sets a value indicating whether calls to <see cref="Execute(string)"/> should be run inside of a transaction.
        /// </summary>
        /// <remarks>
        /// If this is false, <see cref="Transaction"/> will not be set.
        /// </remarks>
        protected virtual bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Gets or sets the database to be used by this migration
        /// </summary>
        protected DbConnection Connection { get; private set; }

        /// <summary>
        /// Gets or sets the database to be used by this migration
        /// </summary>
        /// <remarks>
        /// This is obselete - use <see cref="Connection"/> instead
        /// </remarks>
        [Obsolete("Use this.Connection instead")]
        protected DbConnection DB => this.Connection;

        /// <summary>
        /// Gets or sets the logger to be used by this migration
        /// </summary>
        protected IMigrationLogger Logger { get; private set; }

        /// <summary>
        /// Gets the transaction to use when running comments.
        /// </summary>
        /// <remarks>
        /// This is set only if <see cref="UseTransaction"/> is true.
        /// </remarks>
        protected DbTransaction Transaction { get; private set; }

        // Up and Down should really be 'protected', but in the name of backwards compatibility...

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
                throw new InvalidOperationException("this.Connection has not yet been set. This should have been set by Execute(DbConnection, IMigrationLogger, MigrationDirection)");
            if (this.Logger == null)
                throw new InvalidOperationException("this.Logger has not yet been set. This should have been set by Execute(DbConnection, IMigrationLogger, MigrationDirection)");

            this.Logger.LogSql(sql);

            using (var command = this.Connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = this.Transaction;
                command.ExecuteNonQuery();
            }
        }

        void IMigration<DbConnection>.Execute(DbConnection connection, IMigrationLogger logger, MigrationDirection direction)
        {
            this.Connection = connection;
            this.Logger = logger;

            if (this.UseTransaction)
            {
                using (this.Transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
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
                        this.Transaction = null;
                    }
                }
            }
            else
            {
                if (direction == MigrationDirection.Up)
                    this.Up();
                else
                    this.Down();
            }
        }
    }
}
