using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Specialized <see cref="IDbConnection"/> which has the notion of a single transaction, and which
    /// associates that transaction with all created commands.
    /// </summary>
    public interface ITransactionAwareDbConnection : IDbConnection
    {
        /// <summary>
        /// Gets the currently-open transaction, if any
        /// </summary>
        IDbTransaction Transaction { get; }
    }
}
