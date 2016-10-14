using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Provider which gives access to a database connection. Used to provide the connection to migrations, and to perform transactions
    /// </summary>
    /// <typeparam name="TDatabase">Type of the database being provided</typeparam>
    /// <typeparam name="TTransaction">Type of the transaction being provided</typeparam>
    public interface IConnectionProvider<out TDatabase, out TTransaction>
    {
        /// <summary>
        /// Gets the connection, which will be given to migrations
        /// </summary>
        TDatabase Connection { get; }
        /// <summary>
        /// Gets the transaction, which will be given to migrations
        /// </summary>
        TTransaction Transaction { get; }
        /// <summary>
        /// Begins a transaction
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Commits the ongoing transaction
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// Rolls back the ongoing transaction
        /// </summary>
        void RollbackTransaction();
    }
}
