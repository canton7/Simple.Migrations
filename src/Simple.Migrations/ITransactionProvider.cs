using System;
using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Interface for objects which can be transactional
    /// </summary>
    /// <remarks>
    /// SimpleMigrator uses this to control transactions, even when it doesn't
    /// know what sort of connection it's using
    /// </remarks>
    public interface ITransactionProvider
    {
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
