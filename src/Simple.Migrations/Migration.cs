﻿using System;
using System.Data;
using System.Data.Common;

namespace SimpleMigrations
{
    /// <summary>
    /// Base class, intended to be used by all migrations (although you may implement <see cref="IMigration{TDatabase}"/> directly if you wish).
    /// Migrations MUST apply the <see cref="MigrationAttribute"/> attribute
    /// </summary>
    public abstract class Migration : IMigration<IDbConnection>
    {
        /// <summary>
        /// Gets or sets a value indicating whether calls to <see cref="Execute(string, int?)"/> should be run inside of a transaction.
        /// </summary>
        /// <remarks>
        /// If this is false, <see cref="Transaction"/> will not be set.
        /// </remarks>
        protected virtual bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Gets or sets the database to be used by this migration
        /// </summary>
        protected IDbConnection Connection { get; private set; }

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
        protected IDbTransaction Transaction { get; private set; }

        // Up and Down should really be 'protected', but in the name of backwards compatibility...

        /// <summary>
        /// Invoked when this migration should migrate up
        /// </summary>
        protected abstract void Up();

        /// <summary>
        /// Invoked when this migration should migrate down
        /// </summary>
        protected abstract void Down();

        /// <summary>
        /// Execute and log an SQL query (which returns no data)
        /// </summary>
        /// <param name="sql">SQL to execute</param>
        /// <param name="commandTimeout">The command timeout to use (in seconds), or null to use the default from your ADO.NET provider</param>
        protected virtual void Execute(string sql, int? commandTimeout = null)
        {
            if (this.Logger == null)
                throw new InvalidOperationException("this.Logger has not yet been set. This should have been set by Execute(DbConnection, IMigrationLogger, MigrationDirection)");

            this.Logger.LogSql(sql);

            using (var command = this.CreateCommand(sql, commandTimeout))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Creates a <see cref="DbCommand"/>, which is used by <see cref="Execute(string, int?)"/> to execute SQL commands
        /// </summary>
        /// <remarks>
        /// You can use this method to create commands if you want to set parameters, or access the result of a query.
        /// Override this method if you want to customise how commands are created, e.g. by setting other properties on <see cref="DbCommand"/>.
        /// </remarks>
        /// <param name="sql">SQL to execute</param>
        /// <param name="commandTimeout">The command timeout to use (in seconds), or null to use the default from your ADO.NET provider</param>
        /// <returns>
        /// A <see cref="DbCommand"/> which is configured with the <see cref="DbCommand.CommandText"/>, <see cref="DbCommand.Transaction"/>,
        /// and <see cref="DbCommand.CommandTimeout"/> properties set.
        /// </returns>
        protected virtual IDbCommand CreateCommand(string sql, int? commandTimeout)
        {
            if (this.Connection == null)
                throw new InvalidOperationException("this.Connection has not yet been set. This should have been set by Execute(DbConnection, IMigrationLogger, MigrationDirection)");

            var command = this.Connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = this.Transaction;

            if (commandTimeout != null)
                command.CommandTimeout = commandTimeout.Value;

            return command;
        }

        void IMigration<IDbConnection>.RunMigration(MigrationRunData<IDbConnection> data)
        {
            this.Connection = data.Connection;
            this.Logger = data.Logger;

            if (this.UseTransaction)
            {
                using (this.Transaction = this.Connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        if (data.Direction == MigrationDirection.Up)
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
                if (data.Direction == MigrationDirection.Up)
                    this.Up();
                else
                    this.Down();
            }
        }
    }
}
