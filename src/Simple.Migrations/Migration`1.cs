using System;
using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Base class, intended to be used by all migrations (although you may implement <see cref="IMigration{TDatabase}"/> directly if you wish).
    /// Migrations MUST apply the <see cref="MigrationAttribute"/> attribute
    /// </summary>
    /// <typeparam name="TDatabase">Type of the database</typeparam>
    public abstract class Migration<TDatabase> : IMigration<TDatabase>
        where TDatabase : IDbConnection
    {
        /// <summary>
        /// Gets or sets the database to be used by this migration
        /// </summary>
        public TDatabase DB { get; set; }

        /// <summary>
        /// Gets or sets the logger to be used by this migration
        /// </summary>
        public IMigrationLogger Logger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to run <see cref="Up"/> and <see cref="Down"/> in a transaction.
        /// </summary>
        /// <remarks>
        /// This is set to the value of 'useTransaction' on the [Migration] attribute by default, but can be overridden
        /// </remarks>
        public bool UseTransaction { get; set; }

        /// <summary>
        /// Gets the transaction currently in effect, or null if no transaction is in effect
        /// </summary>
        protected IDbTransaction Transaction { get; private set; }

        /// <summary>
        /// Invoked when this migration should migrate up
        /// </summary>
        public abstract void Up();

        /// <summary>
        /// Invoked when this migration should migrate down
        /// </summary>
        public abstract void Down();

        void IMigration<TDatabase>.Up()
        {
            if (this.UseTransaction)
            {
                InvokeInTransaction(this.Up);
            }
            else
            {
                this.Up();
            }
        }

        void IMigration<TDatabase>.Down()
        {
            if (this.UseTransaction)
            {
                InvokeInTransaction(this.Down);
            }
            else
            {
                this.Down();
            }
        }

        private void InvokeInTransaction(Action action)
        {
            this.Transaction = this.DB.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                action();

                this.Transaction.Commit();
            }
            catch
            {
                this.Transaction.Rollback();
                throw;
            }
            finally
            {
                this.Transaction = null;
            }
        }

        /// <summary>
        /// Execute and log an SQL query (which returns no data)
        /// </summary>
        /// <param name="sql">SQL to execute</param>
        public virtual void Execute(string sql)
        {
            if (this.DB == null)
                throw new InvalidOperationException("this.DB has not yet been set. This should have been set by the SimpleMigrator");
            if (this.Logger == null)
                throw new InvalidOperationException("this.Logger has not yet been set. This should have been set by the SimpleMigrator");

            this.Logger.LogSql(sql);

            using (var command = this.DB.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = this.Transaction;
                command.ExecuteNonQuery();
            }
        }
    }
}
