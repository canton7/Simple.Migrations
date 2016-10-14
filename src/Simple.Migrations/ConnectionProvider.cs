using System;
using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Implements <see cref="IConnectionProvider{TDatabase}"/> for <see cref="IDbConnection"/> connections
    /// </summary>
    public class ConnectionProvider : IConnectionProvider<IDbConnection>
    {
        private IDbTransaction transaction;

        /// <summary>
        /// Gets the current connection, set using the constructors
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="ConnectionProvider"/> class
        /// </summary>
        public ConnectionProvider(IDbConnection connection)
        {
            this.Connection = connection;
            if (this.Connection.State == ConnectionState.Closed)
                this.Connection.Open();
        }

        /// <summary>
        /// Begins a transaction
        /// </summary>
        public void BeginTransaction()
        {
            if (this.transaction != null)
                throw new InvalidOperationException("Two transactions opened at once");

            this.transaction = this.Connection.BeginTransaction(IsolationLevel.Serializable);
        }

        /// <summary>
        /// Commits the ongoing transaction
        /// </summary>
        public void CommitTransaction()
        {
            if (this.transaction == null)
                throw new InvalidOperationException("No transaction in progress");

            this.transaction.Commit();
            this.transaction = null;
        }

        /// <summary>
        /// Rolls back the ongoing transaction
        /// </summary>
        public void RollbackTransaction()
        {
            if (this.transaction == null)
                throw new InvalidOperationException("No transaction in progress");

            this.transaction.Rollback();
            this.transaction = null;
        }
    }
}
