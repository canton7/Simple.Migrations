using System;
using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Implements <see cref="IConnectionProvider{TDatabase}"/> for <see cref="IDbConnection"/> connections
    /// </summary>
    public class ConnectionProvider : IConnectionProvider<IDbConnection>
    {
        private readonly IDbConnection connection;
        private IDbTransaction transaction;

        /// <summary>
        /// Gets the current connection, set using the constructors
        /// </summary>
        public IDbConnection Connection
        {
            get { return this.connection; }
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="ConnectionProvider/> class
        /// </summary>
        public ConnectionProvider(IDbConnection connection)
        {
            this.connection = connection;
            this.connection.Open();
        }

        /// <summary>
        /// Begins a transaction
        /// </summary>
        public void BeginTransaction()
        {
            if (this.transaction != null)
                throw new InvalidOperationException("Two transactions opened at once");

            this.transaction = this.connection.BeginTransaction(IsolationLevel.Serializable);
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
