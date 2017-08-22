Changelog
=========

v0.9.16
-------

 - Allow schema creation to be disabled (by setting the SchemaName property of a DatabaseProvider to null) (#24)

v0.9.15
-------

 - Handle loading migrations if all types couldn't be loaded

v0.9.13, 0.9.14
---------------

 - No changes - released needed by the CI system

v0.9.12
-------

 - Don't log confusing info if there are no migrations to run
 - ConsoleRunner's reapply command doesn't assume that migrations are numbered sequentially
 - ConsoleLogger doesn't crash if there's no console window available (#20)
 - ConsoleRunner displays a stack trace for unexpected errors (#20)

v0.9.11
-------

 - Correctly call Logger.EndSequence and Logger.EndSequenceWithError
 - Fix the SQLiteNet example, and the README section which documents it
 - ConsoleRunner shows a nicer error if the requested migration version was not found

v0.9.10
-------

**BREAKING CHANGE**: Change access modifiers of Migration.Up() and Migration.Down() to protected. You will need to modify your migrations!

 - Database providers ensure schema is created, if necessary
 - Add SchemaName property to MssqlDatabaseProvider and PostgresqlDatabaseProvider
 - GetSetVersionSql() has access to @OldVersion
 - Remove erroneous dependency on Microsoft.Data.SqlClient

v0.9.9
------

 - Remove .NET 4.0 support: it hasn't been requested, isn't supported by Microsoft any more, and might get in the way of possible future async support.
 - Allow the command timeout used in migratinos to be configured (#14).
 - Throw a specific exception if migration data is missing (#15).
 - SimpleMigrator.Load() will reload the current version if called multiple times.

v0.9.8
------

**BREAKING CHANGE**: The MssqlDatabaseProvider now uses an advisory lock, so the way you instantiate it has changed from:

```csharp
var databaseProvider = new MssqlDatabaseProvider(() => new SqlConnection("Connection String"));
var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
migrator.Load();
```

To this:

```csharp
using (var connection = new SqlConnection("Connection String"))
{
    var databaseProvider = new MssqlDatabaseProvider(connection);
    var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
    migrator.Load();
}
```

Also add support for .NET 4.0.

v0.9.7
------

**BREAKING CHANGE**: The architecture has been altered reasonably significantly to allow support for concurrent migrators (#7).

In particular:

   - `IConnectionProvider<TConnection>` has gone.
   - `IVersionProvider<TConnection>` has been renamed to `IDatabaseProvider<TConnection>`, and has taken on additional responsibilities: it now owns the database connection (or database connection factory), and can do locking on the database.
   - `Migration<TConnection>` has gone.
   - `Migration` has taken on responsibility for opening a transaction if necessary. If you have a migration that must run outside of a transaction, this is no longer configured in the `[Migration]` attribute: instead set the `UseTransaction` property in the migration itself.

The way in which you instantiate a `SimpleMigrator` has changed. Instead of doing this:

```csharp
using SimpleMigrations;
using SimpleMigrations.VersionProviders;

var migrationsAssembly = Assembly.GetEntryAssembly();
using (var connection = new ConnectionProvider(/* connection string */))
{
    var versionProvider = new SQLiteVersionProvider();

    var migrator = new SimpleMigrator(migrationsAssembly, connection, versionProvider);
    migrator.Load();
}
```

You now do this:

```csharp
using SimpleMigrations;
using SimpleMigrations.DatabaseProvider;

var migrationsAssembly = Assembly.GetEntryAssembly();
using (var connection = new ConnectionProvider(/* connection string */))
{
    var databaseProvider = new SQLiteVersionProvider(connection);

    var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
    migrator.Load();
}
```

See the README for more details.

If you have written your own `IVersionProvider<TConnection>`, this will have to be updated.
Instead of deriving from `VersionProviderBase`, you should derive from either `DatabaseProviderBaseWithAdvisoryLock` or `DatabaseProviderBaseWithVersionTableLock`, depending on whether you want to use an advisory lock or lock on the version table to prevent concurrent migrators, see the README for more details.

If you do not want to worry about concurrent migrators, then derive from `DatabaseProviderBaseWithAdvisoryLock` and override `AcquireAdvisoryLock` and `ReleaseAdvisoryLock` to do nothing, see `SqliteDatabaseProvider` for an example.




v0.9.6
------

 - No code change: version bump required for build server

v0.9.5
------

 - Change nuget package title to 'Simple.Migrations', so NuGet will let us publish again

v0.9.4
------

 - Add support for .NET Core (.NET Standard 1.2)
 - Add MSSQL and MySQL version providers
 - Allow migration discovery to be customised, using `IMigrationProvider`
 - Fix use of transactions with MSSQL
 - General robustness improvements

v0.9.3
------

 - No code change: version bump required for build server

v0.9.2
------

 - Change NuGet ID to Simple.migrations to avoid conflict

v0.9.1
------

 - Add documentation and samples
 - Create NuGet package
 
v0.9.0
------

 - Initial release