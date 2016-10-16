using System;
using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Provider which gives access to a database connection. Used to provide the connection to migrations, and to perform transactions
    /// </summary>
    /// <typeparam name="TConnection">Type of the database being provided</typeparam>
    public interface IConnectionProvider<TConnection>
    {
        /// <summary>
        /// Gets the connection, which will be given to migrations
        /// </summary>
        TConnection Connection { get; }

        /// <summary>
        /// Begin a transaction
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if a transaction is already open</exception>
        void BeginTransaction();

        /// <summary>
        /// Commit the currently-open transaction
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no transaction is open</exception>
        void CommitTransaction();

        /// <summary>
        /// Rolls back the currently-open transaction
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no transaction is open</exception>
        void RollbackTransaction();
    }
}
