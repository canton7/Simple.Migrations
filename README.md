![Project Icon](icon.png) Simple.Migrations
===========================================

[![NuGet](https://img.shields.io/nuget/v/Simple.Migrations.svg)](https://www.nuget.org/packages/Simple.Migrations/)
[![Build status](https://ci.appveyor.com/api/projects/status/iub4g6p0qs7onn2b?svg=true)](https://ci.appveyor.com/project/canton7/simplemigrations)

Simple.Migrations is a simple bare-bones migration framework for .NET Core (.NET Standard 1.2 and .NET 4.5). 
It doesn't provide SQL generation, or an out-of-the-box command-line tool, or other fancy features.
It does however provide a set of simple, extendable, and composable tools for integrating migrations into your application.

**WARNING: Until Simple.Migrations reaches version 1.0, I reserve the right to make minor backwards-incompatible changes to the API.**
API breakages are documented in [the CHANGELOG](CHANGELOG.md).

### Table of Contents

1. [Installation](#installation)
2. [Quick Start](#quick-start)
3. [Migrations](#migrations)
4. [Database Providers](#database-providers)
5. [SimpleMigrator](#simplemigrator)
6. [ConsoleRunner](#consolerunner)
7. [Finding Migrations](#finding-migrations)
8. [Writing your own Database Provider](#writing-your-own-database-provider)
9. [Example: Using sqlite-net](#example-using-sqlite-net)

Installation
------------

[Simple.Migrations is available on NuGet](https://www.nuget.org/packages/Simple.Migrations).

Either open the package console and type:

```
PM> Install-Package Simple.Migrations
```

Or right-click your project -> Manage NuGet Packages... -> Online -> search for Simple.Migrations in the top right.

I also publish symbols on [SymbolSource](http://www.symbolsource.org/Public), so you can use the NuGet package but still have access to RestEase's source when debugging.
If you haven't yet set up Visual Studio to use SymbolSource, do that now:

1. Go to Tools -> Options -> Debugger -> General.
2. Uncheck "Enable Just My Code (Managed only)".
3. Uncheck "Enable .NET Framework source stepping". Yes, it is misleading, but if you don't, then Visual Studio will ignore your custom server order (see further on) and only use it's own servers.
4. Check "Enable source server support".
5. Uncheck "Require source files to exactly match the original version"
6. Go to Tools -> Options -> Debugger -> Symbols.
7. Select a folder for the local symbol/source cache. You may experience silent failures in getting symbols if it doesn't exist or is read-only for some reason.
8. Add `http://srv.symbolsource.org/pdb/Public` under "Symbol file (.pdb) locations".


Quick Start
-----------

I'll introduce SimpleMigrator by walking through a basic example.

Here, we'll create a separate console application, which contains all of our migrations and can be invoked in order to migrate the database between different versions.
If you want your application to automatically migrate to the latest version when it's started, or you want to put your migrations in another library, etc, that's easy too: we'll get on to those [a bit later](#finding-migrations).

First, create a new Console Application.

The first task is to create (at least one) migration.
Migrations are classes which derive from `Migration`, and are decorated with the `[Migration(versionNumber)]` attribute.
How you number your migrations is up to you: some people like to number them sequentially, while others like to use the current date and time (e.g. `20150105164402`).

In `Migrations/01_CreateUsers.cs`:

```csharp
using SimpleMigrations;

namespace Migrations
{
    [Migration(1)]
    public class CreateUsers : Migration
    {
        public override void Up()
        {
            Execute(@"CREATE TABLE Users (
                Id SERIAL NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            )");
        }

        public override void Down()
        {
            Execute("DROP TABLE Users");
        }
    }
}
```

Let's take a look at all of the features of this migration:

 - There are `Up` and `Down` methods, which are overriden from `Migration`. These are invoked when the migration should be applied (`Up`) or reverted (`Down`). The `Down` method should undo the changes made by the `Up` method.
 - There's an `Execute` method, which is inherited from `Migration`, which you call to execute SQL.

We'll go into `Migration` in more detail below.

Finally, in your `Program.cs`:

```csharp
class Program
{
    static void Main(string[] args)
    {
        var migrationsAssembly = typeof(Program).Assembly;
        using (var db = new SQLiteConnection("DataSource=database.sqlite"))
        {
            var databaseProvider = new SqliteDatabaseProvider(db);
            var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);

            // Either interact directly with the migrator...

            migrator.Load();
            migrator.MigrateTo(1);
            migrator.MigrateToLatest();
            migrator.Baseline();

            // Or turn it into a console application

            // Note: ConsoleRunner is only available in .NET Standard 1.3 and .NET 4.5 (not .NET Standard 1.2)
            var consoleRunner = new ConsoleRunner(migrator);
            consoleRunner.Run(args);
        }
    }
}
```

What's going on here? 
Well, we create a new `SimpleMigrator`, passing in the assembly to search for migrations, the database connection to use to talk to the database, and the version provider to use.

What's a database provider?
Simple.Migrations uses a table inside your database to track which version the database is at.
Since difference databases (SQLite, PostgreSQL, etc) will need different commands to create and modify that table, Simple.Migrations uses a database provider to provide that ability. 
There are a few built-in database providers, or you can easily write your own, see below.

Finally, we create and run a `ConsoleRunner`. 
This is a little helper class which gives you a nice command-line interface for running migrations.
It's not necessary to use this by any means (you can call methods on `SimpleMigrator` directly), but it saves time when you just want a simple command-line application to run your migrations.

If you don't want a command-line interface (or you're using .NET Standard 1.2, which doesn't provide a Console), then you can [call methods on `SimpleMigrator` directly](#simplemigrator).

And we're done!
Build your project, and run it with the command-line argument `help` to see what's available.


Migrations
----------

Let's discuss migrations in detail.

A migration is a class which implements `IMigration<TConnection>`, and is decorated with the `[Migration]` attribute.
`IMigration<TConnection>` is database-agnostic: it doesn't care what sort of database you're talking to.

`Migration` is an abstract class which implements `IMigration<DbConnection>`, providing a base class for all migrations which use an ADO.NET database connection.
It has `Up()` and `Down()` methods, which you override to migrate up and down, a `Connection` property to access the database connection, and a `Logger` property to log the SQL which is being run and any other messages.
It also has an `Execute` method, which logs the SQL it's given (using the `Logger`) and runs it (using the `Connection`).

Migrations are executed inside a transaction by default.
This means that either the migration is applied in full, or not at all.
If you need to disable this (some SQL statements in some databases cannot be run inside a transaction, for example), set `this.UseTransaction = false` in your `Migration` subclass.
It is strongly recommended that migrations which do not execute inside of a transaction do only a single thing: use multiple migrations if necessary.

If you want to perform more complicated operations on the database (e.g selecting data, or inserting variable data) you'll have to use the `Connection` property directly.
[`Dapper`](https://github.com/StackExchange/dapper-dot-net) may make your life easier here (it works by providing extension methods on `DbConnection`), or you can create your own migration base class which uses a specific type of database connection, and add your own helpers methods to it.


Database Providers
------------------

Simple.Migrations aims to be as database-agnostic as possible.
However, there are of course places where Simple.Migrations needs to interact with the database, for example to fetch or update the current schema version, or acquire a lock so that multiple concurrent migrators don't collide with each other.
To do this, it uses a database provider.

A database provider is a class which implements `IDatabaseProvider<TConnection>`.
This interface describes methods which:

 - Acquire an exclusive lock on the database, so that multiple concurrent migrators can work together
 - Ensure that the version table is created
 - Fetch the current schema version from the database
 - Update the current schema version (up and down)

A few implementations of `DatabaseProviderBase` have been provided, which provide access to the schema version table for a small number of database engines.
If you're not using one of these database engines, you'll have to write your own database provider.
Subissions to support more database engines are readily accepted!

### Build-in database providers

The currently supported database engines are:

#### MSSQL

Usage:

```csharp
using (var connection = new SqlConnection("Connection String"))
{
    var databaseProvider = new MssqlDatabaseProvider(connection);
    var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
    migrator.Load();
}
```

MSSQL provides full support for concurrent migrators, by using app locks.
#### SQLite

Usage:

```csharp
using (var connection = new SqliteConnection("Connection String"))
{
    var databaseProvider = new SqliteDatabaseProvider(connection);
    var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
    migrator.Load();
}
```

SQLite does not support concurrent migrators.

#### PostgreSQL

Usage:

```csharp
using (var connection = new NpgsqlConnection("Connection String"))
{
    var databaseProvider = new PostgresqlDatabaseProvider(connection);
    var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
    migrator.Load();
}
```

PostgreSLQ provides full support for concurrent migrators, by using advisory locks.

#### MySQL

```csharp
using (var connection = new MySqlConnection("Connection String"))
{
    var databaseProvider = new MysqlDatabaseProvider(connection);
    var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
    migrator.Load();
}
```

MySQL provides full support for concurrent migrators, by using advisory locks.


SimpleMigrator
--------------

`SimpleMigrator` is where it all kicks off.
This class provides access to the list of all migrations, the current migration, and methods to migrate to a particular version (or straight to the latest version).
It is also responsible for finding migrations, and for instantiating and configuring each migration.

`SimpleMigrator` comes in two flavours: `SimpleMigrator<TConnection, TMigrationBase>` which allows you to use any sort of database connection (not just ADO.NET) and any migration base class, and `SimpleMigrator`, which assumes you're using ADO.NET and `Migration`.

You'll want to use this class directly in any scenario where you're not using the `ConsoleRunner` to provide a command-line interface for you, [see below](#consolerunner).

After instantiating a `SimpleMigrator`, but before doing anything else with it, you'll need to call `.Load()`.
This discovers your migrations, and queries your database to find the current version.

`SimpleMigrator` offers the following important properties and methods:

 - `Load()` - must be called before anything else. Populates the properties documented below.
 - `CurrentMigration` - gets the current version of your database schema.
 - `Migrations` - gets a list of all of your migrations, ordered from oldest to newest.
 - `LatestMigration` - gets the latest migration (the last element in `Migrations`).
 - `MigrateTo(n)` - migrates the database to version `n`. There must be a migration in `Migrations` with version `n`. Will migrate up or down as necessary. Use `MigrateTo(0)` to remove all migrations.
 - `MigrateToLatest()` - shortcut for `MigrateTo(Migrations.Last())`.
 - `Baseline(n)` - updates the VersionInfo table to indicate that the database is on version `n`, but don't run any migrations. This is useful if you've got a pre-existing database which doesn't use migrations: create a migration with version 1 which describes the schema of your database, and use `.Baseline(1)`.

For example, if you want to migrate to the latest version on startup (not particularly recommended):

```csharp
var migrationsAssembly = Assembly.GetEntryAssembly();
using (var connection = new SqliteConnection("Connection String"))
{
    var databaseProvider = new SqliteDatabaseProvider(connection);

    var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
    migrator.Load();

    migrator.MigrateToLatest();
}
```

Or, if you wanted to assert that your database was at the latest migration:

```csharp
var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
migrator.Load();

if (migrator.CurrentMigration.Version != migrator.LatestMigration.Version)
{
    // Throw a 'you must migrate' exception
}
```

You can, of course, migrate to a specific version this way as well, if you want:

```csharp
var migrator = new SimpleMigrator(migrationsAssembly, databaseProvider);
migrator.Load();

migrator.MigrateTo(3);
```

To remove all migrations, use `.MigrateTo(0)`.


ConsoleRunner
-------------

`ConsoleRunner` is a little helper class which aims to be useful if you're writing a command-line application to perform your migrations.
It uses a `SimpleMigrator`, takes your command-line arguments, and uses the latter to decide what methods on the former to execute.

You can use it, extend it (look at the `SubCommands` property), or write your own version: it's not critical in any way.

This is only available when targetting .NET Standard 1.3 (or above) and .NET 4.5.
The Console is not available in .NET Standard 1.2.


Finding Migrations
------------------

`SimpleMigrator` will find migrations to run by searching through an assembly you specify for classes which implement `IMigration<TConnection>`.
This means you can put your migrations inside any assembly you want, and reference that assembly from your code which uses `SimpleMigrator`.

Some common configurations are:

 - Main application contains the migrations, and uses Simple.Migrations to automatically migrate on startup.
 - Main application contains the migrations, and there is a separate executable to run migrations. This executable references the main application.
 - Dedicated DLL contains the migrations, which is used by both the main application and a separate executable (used to run migrations).


You can customize how migrations are found by implementing `IMigrationProvider`.
This returns a collection of `MigrationData` - see `AssemblyMigrationProvider` for an example.
Then pass your migration provider to the appropriate `SimpleMigrator` constructor.


Writing your own Database Provider
---------------------------------

If you want to write your own database provider, first figure out whether you want to support concurrent migrators, and if so, whether you are going to use advisory locks (if you database supports them), or locking on the VersionInfo table.

If you don't want to support concurrent migrators, then subclass `DatabaseProviderBaseWithAdvisoryLock`.
Override `AcquireAdvisoryLock()` and `ReleaseAdvisoryLock()` to do nothing, and override `GetcreateVersionTableSql()`, `GetCurrentVersionSql()`, and `GetSetVersionSql()` to return SQL suitable for your database (see the docs on `DatabaseProviderBaseWithAdvisoryLock` for details).

If you want to support concurrent migrators by using an advisory lock, then subclass `DatabaseProviderBaseWithAdvisoryLock`.
Override `AcquireAdvisoryLock()` to acquire the advisory lock using `this.Connection`, and override `ReleaseAdvisoryLock()` to release that lock.
Override `GetcreateVersionTableSql()`, `GetCurrentVersionSql()`, and `GetSetVersionSql()` to return SQL suitable for your database (see the docs on `DatabaseProviderBaseWithAdvisoryLock` for details).

If you want to support concurrent migrators by using a lock on the SchemaVersion table, then subclass `DatabaseProviderBaseWithVersionTableLock`.
Override `AcquireVersionTableLock()`, and inside create a new transaction using `this.VersionTableConnection`, assign it to `this.VersionTableLockTransaction`, and execute whatever SQL is necessary to convince the database engine that it may not allow other connections to query the VersionInfo table.
Override `ReleaseVersionTableLock()` to destroy `this.VersionTableLockTransaction`.
Override `GetcreateVersionTableSql()`, `GetCurrentVersionSql()`, and `GetSetVersionSql()` to return SQL suitable for your database (see the docs on `DatabaseProviderBaseWithAdvisoryLock` for details).

See the pre-existing database providers, and the documentation on the classes `DatabaseProviderBase`, `DatabaseProviderBaseWithAdvisoryLock`, and `DatabaseProviderBaseWithVersionTableLock` for more details.

Example: Using sqlite-net
-------------------------

[sqlite-net](https://github.com/praeclarum/sqlite-net) is a little SQLite driver and micro-ORM.
It is not an ADO.NET implementation, and so makes a good example for seeing how Simple.Migrations work with drivers which don't implement ADO.NET.

First off, create a new Console Application, and install the sqlite-net NuGet package.
You'll also need to download `sqlite.dll` and add it to your project.

Next, we'll need to create a number of classes to tell Simple.Migrations how to work with sqlite-net: we'll need a database provider and a base class for the migrations.


### `IDatabaseProvider`

First, the `IDatabaseProvider` implementation.
This is the class which lets Simple.Migrations work with the 'SchemaVersion' table in your database.
Since sqlite-net includes a micro-ORM, let's use that to create, read, and modify the SchemaVersion table, rather than executing raw SQL.

Note that it's not normally advisable to write migrations using C# models: migrations by definition work with lots of different versions of a table, whereas a C# class can only encapsulate one version of a table.
However, since our SchemaVersion table is never going to change, it's an acceptable shortcut here.

We're not going to worry about concurrent migrators here.

Our table model:

```csharp
public class SchemaVersion
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public long Version { get; set; }
    public DateTime AppliedOn { get; set; }
    public string Description { get; set; }
}
```

And our `IDatabaseProvider` implementation:

```csharp
public class SQLiteNetDatabaseProvider : IDatabaseProvider<SQLiteConnection>
{
    private readonly SQLiteConnection connection;

    public SQLiteNetDatabaseProvider(SQLiteConnection connection)
    {
        this.connection = connection;
    }

    public SQLiteConnection BeginOperation()
    {
        // This is the connect that will be used for migrations
        return this.connection;
    }

    public void EndOperation()
    {
    }

    public long EnsureCreatedAndGetCurrentVersion()
    {
        this.connection.CreateTable<SchemaVersion>();
        return this.GetCurrentVersion();
    }

    public long GetCurrentVersion()
    {
        // Return 0 if the table has no entries
        var latestOrNull = this.connection.Table<SchemaVersion>().OrderByDescending(x => x.Id).FirstOrDefault();
        return latestOrNull?.Version ?? 0;
    }

    public void UpdateVersion(long oldVersion, long newVersion, string newDescription)
    {
        this.connection.Insert(new SchemaVersion()
        {
            Version = newVersion,
            AppliedOn = DateTime.Now,
            Description = newDescription,
        });
    }
}
```

### Migration base class

Now we need a migration base class, which knows how to execute queries using sqlite-net.

```csharp
public abstract class SQLiteNetMigration : IMigration<SQLiteConnection>
{
    protected SQLiteConnection Connection { get; set; }

    protected IMigrationLogger Logger { get; set; }

    public abstract void Down();

    public abstract void Up();

    public void Execute(string sql)
    {
        this.Logger.LogSql(sql);
        this.Connection.Execute(sql);
    }

    void IMigration<SQLiteConnection>.Execute(SQLiteConnection connection, IMigrationLogger logger, MigrationDirection direction)
    {
        this.Connection = connection;
        this.Logger = logger;

        try
        {
            connection.BeginTransaction();

            if (direction == MigrationDirection.Up)
                this.Up();
            else
                this.Down();

            connection.Commit();
        }
        catch
        {
            connection.Rollback();
            throw;
        }
    }
}
``` 

### Migrations

We can then create migrations like this:

```csharp
[Migration(1)]
public class CreateUsers : SQLiteNetMigration
{
    public override void Up()
    {
        Execute(@"CREATE TABLE Users (
            Id SERIAL NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL
        );");
    }

    public override void Down()
    {
        Execute("DROP TABLE Users");
    }
}
```

### Putting it all together

Finally, let's use all of our components to actually run some migrations.

Note how we have to use `SimpleMigrator<TConnection, TMigrationBase>` here:

```csharp
using (var connection = new SQLiteConnection("SQLiteNetdatabase.sqlite"))
{
    var databaseProvider = new SQLiteNetDatabaseProvider(connection);

    var migrator = new SimpleMigrator<SQLiteConnection, SQLiteNetMigration>(
        Assembly.GetEntryAssembly(), databaseProvider);
    var runner = new ConsoleRunner(migrator);
    runner.Run(args);
}
```