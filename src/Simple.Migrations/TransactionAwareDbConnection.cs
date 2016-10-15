using System;
using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Wrapper around an <see cref="IDbConnection"/> which adds the notion of a current transaction,
    /// and automatically associates new <see cref="IDbCommand"/>s with it.
    /// </summary>
    public class TransactionAwareDbConnection : ITransactionAwareDbConnection
    {
        private readonly IDbConnection connection;

        /// <summary>
        /// Gets the currently-open transaction, if any
        /// </summary>
        public IDbTransaction Transaction { get; private set; }

        /// <summary>
        /// Gets or sets the string used to open a database.
        /// </summary>
        public string ConnectionString
        {
            get { return this.connection.ConnectionString; }
            set { this.connection.ConnectionString = value; }
        }

        /// <summary>
        /// Gets the time to wait while trying to establish a connection before terminating
        /// the attempt and generating an error.
        /// </summary>
        public int ConnectionTimeout => this.connection.ConnectionTimeout;

        /// <summary>
        /// Gets the name of the current database or the database to be used after a connection
        /// is opened.
        /// </summary>
        public string Database => this.connection.Database;

        /// <summary>
        /// Gets the current state of the connection.
        /// </summary>
        public ConnectionState State => this.connection.State;

        /// <summary>
        /// Changes the current database for an open Connection object
        /// </summary>
        /// <param name="databaseName"></param>
        public void ChangeDatabase(string databaseName) => this.connection.ChangeDatabase(databaseName);

        /// <summary>
        /// Opens a database connection with the settings specified by the ConnectionString
        /// property of the provider-specific Connection object.
        /// </summary>
        public void Open() => this.connection.Open();

        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public void Close() => this.connection.Close();

        /// <summary>
        /// Instantiates a new instance of the <see cref="TransactionAwareDbConnection"/> class
        /// </summary>
        /// <param name="connection">Connection to wrap</param>
        public TransactionAwareDbConnection(IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            this.connection = connection;
        }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <returns>An object representing the new transaction.</returns>
        public IDbTransaction BeginTransaction()
        {
            if (this.Transaction != null)
                throw new InvalidOperationException("Nested transactions are not supported");

            this.Transaction = this.connection.BeginTransaction();
            return this.Transaction;
        }

        void ITransactionProvider.BeginTransaction() => this.BeginTransaction(IsolationLevel.Serializable);

        /// <summary>
        /// Begins a database transaction with the specified <see cref="IsolationLevel"/> value.
        /// </summary>
        /// <param name="il">One of the System.Data.IsolationLevel values.</param>
        /// <returns>An object representing the new transaction.</returns>
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            if (this.Transaction != null)
                throw new InvalidOperationException("Nested transactions are not supported");

            this.Transaction = this.connection.BeginTransaction(il);
            return this.Transaction;
        }

        /// <summary>
        /// Commit the currently-open transaction
        /// </summary>
        public void CommitTransaction()
        {
            if (this.Transaction == null)
                throw new InvalidOperationException("No transaction in progress");

            this.Transaction.Commit();
            this.Transaction = null;
        }

        /// <summary>
        /// Rolls back the currently-open transaction
        /// </summary>
        public void RollbackTransaction()
        {
            if (this.Transaction == null)
                throw new InvalidOperationException("No transaction in progress");

            this.Transaction.Rollback();
            this.Transaction = null;
        }

        /// <summary>
        /// Creates and returns a Command object associated with the connection
        /// </summary>
        /// <returns>A Command object associated with the connection</returns>
        public IDbCommand CreateCommand()
        {
            var command = this.connection.CreateCommand();
            command.Transaction = this.Transaction;
            return command;
        }

        /// <summary>
        /// Dispose the current connection, and the current transaction if any
        /// </summary>
        public void Dispose()
        {
            this.Transaction?.Dispose();
            this.connection.Dispose();
        }
    }
}
