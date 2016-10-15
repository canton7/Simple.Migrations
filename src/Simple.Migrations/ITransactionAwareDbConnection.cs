using System.Data;

namespace SimpleMigrations
{
    /// <summary>
    /// Encapsulates the notion of an <see cref="ITransactionProvider"/> implemented for
    /// an <see cref="IDbConnection"/>
    /// </summary>
    /// <remarks>
    /// Things which implement this interface are <see cref="IDbConnection"/>s which
    /// SimpleMigrator knows how to create/commit transactions for
    /// </remarks>
    public interface ITransactionAwareDbConnection : IDbConnection, ITransactionProvider
    {
        /// <summary>
        /// Gets the currently-open transaction, if any
        /// </summary>
        IDbTransaction Transaction { get; }
    }
}
