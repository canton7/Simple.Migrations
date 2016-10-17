![Project Icon](icon.png) SimpleMigrations
==========================================

[![NuGet](https://img.shields.io/nuget/v/Simple.Migrations.svg)](https://www.nuget.org/packages/Simple.Migrations/)
[![Build status](https://ci.appveyor.com/api/projects/status/iub4g6p0qs7onn2b?svg=true)](https://ci.appveyor.com/project/canton7/simplemigrations)

SimpleMigrations is a simple bare-bones migration framework for .NET Core (.NET Standard 1.2 and .NET 4.5). 
It doesn't provide SQL generation, or an out-of-the-box command-line tool, or other fancy features.
It does however provide a set of simple, extendable, and composable tools for integrating migrations into your application.

**WARNING: Until SimpleMigrations reaches version 1.0, I reserve the right to make minor backwards-incompatible changes to the API.**

### Table of Contents

1. [Installation](#installation)
2. [Quick Start](#quick-start)
3. [Migrations](#migrations)
4. [Version Providers](#version-providers)
5. [Connection Providers](#connection-providers)
6. [SimpleMigrator](#simplemigrator)
7. [ConsoleRunner](#consolerunner)
8. [Finding Migrations](#finding-migrations)
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
        var db = new SQLiteConnection("DataSource=database.sqlite");
        var versionProvider = new SqliteVersionProvider();

        var migrator = new SimpleMigrator(migrationsAssembly, db, versionProvider);

        // Note: ConsoleRunner is only available in .NET Standard 1.3 and .NET 4.5 (not .NET Standard 1.2)
        var consoleRunner = new ConsoleRunner(migrator);
        consoleRunner.Run(args);
    }
}
```

What's going on here? 
Well, we create a new `SimpleMigrator`, passing in the assembly to search for migrations, the database connection to use to talk to the database, and the version provider to use.

What's a version provider?
SimpleMigrations uses a table inside your database to track which version the database is at.
Since difference databases (SQLite, PostgreSQL, etc) will need different commands to create and modify that table, SimpleMigrations uses a version provider to provide that ability. 
There are a few built-in version providers, or you can easily write your own, see below.

Finally, we create and run a `ConsoleRunner`. 
This is a little helper class which gives you a nice command-line interface for running migrations.
It's not necessary to use this by any means (you can call methods on `SimpleMigrator` directly), but it saves time when you just want a simple command-line application to run your migrations.

If you don't want a command-line interface (or you're using .NET Standard 1.2, which doesn't provide a Console), then you can [call methods on `SimpleMigrator` directly](#simplemigrator).

And we're done!
Build your project, and run it with the command-line argument `help` to see what's available.


Migrations
----------

Let's discuss migrations in detail.

A migration is a class which implement `IMigration<TConnection>`, and is decorated with the `[Migration]` attribute.
`IMigration<TConnection>` is database-agnostic: it doesn't care what sort of database you're talking to.
All migrations have an `Up` and `Down` method, a `DB` property to access the database connection, and a `Logger` property to log the SQL which is being run and any other messages.

`Migration` is an abstract class which implements `IMigration<ITransactionAwareDbConnection>` (basically an `IDbConnection`), providing a base class for all migrations which use an ADO.NET database connection.
It adds an `Execute` method, which logs the SQL it's given (using the `Logger`) and runs it (using the `DB`).

Migrations are executed inside a transaction by default.
This means that either the migration is applied in full, or not at all.
If you need to disable this (some SQL statements in some databases cannot be run inside a transaction, for example), specify `UseTransaction=false` in the `[Migration]` attribute.
It is strongly recommended that migrations which do not execute inside of a transaction do only a single thing: use multiple migrations if necessary.

If you want to perform more complicated operations on the database (e.g selecting data, or inserting variable data) you'll have to use the `DB` property directly.
[`Dapper`](https://github.com/StackExchange/dapper-dot-net) may make your life easier here (it works by providing extension methods on `IDbConnection`), or you can create your own migration base class which uses a specific type of database connection, and add your own helpers methods to it.


Version Providers
-----------------

SimpleMigrations aims to be as database-agnostic as possible.
However, there is one place where SimpleMigrations needs to talk to the database directly: the schema version table.
SimpleMigrations uses a table inside of your database to track which version the database is at, i.e. which migration was most recently applied.
In order to manipulate this, it needs to communicate with your database.
It does this using version providers.

A version provider is a class which implements `IVersionProvider<TConnection>`.
This interface describes methods which:

 - Ensure that the version table is created
 - Fetch the current schema version from the database
 - Update the current schema version (up and down)

A helper class, `VersionProviderBase`, assumes an ADO.NET connection and implements `IVersionProvider<IDbConnection>`.
This class provides methods for executing SQL to perform the operations required by `IVersionProvider`, and has abstract methods which child classes can use to provide that SQL.

A few implementations of `VersionProviderBase` have been provided, which provide access to the schema version table for a small number of database engines.
If you're not using one of these database engines, you'll have to write your own version provider.
Subissions to support more database engines are readily accepted!

The currently supported database engines are:

 - MSSQL (`VersionProviders.MssqlVersionProvider`)
 - SQLite (`VersionProviders.SqliteVersionProvider`)
 - PostgreSQL (`VersionProviders.PostgresSQLVersionProvider`)
 - MySQL (`VersionProviders.MysqlVersionProvider`)


Connection Providers
--------------------

SimpleMigrations needs to be able to create, commit, and rollback transactions.
In order to encapsulate this functionality, the interface `IConnectionProvider<TConnection>` exists.
This is implemented by `ConnectionProvider` for ADO.NET.

Most of the time you won't need to think about this.
However, if you're using a database connection which is not based on ADO.NET, you'll need to write your own connection provider, see the [sqlite-net example below](#example-using-sqlite-net).


SimpleMigrator
--------------

`SimpleMigrator` is where it all kicks off.
This class provides access to the list of all migrations, the current migration, and methods to migrate to a particular version (or straight to the latest version).
It is also responsible for finding migrations, and for instantiating and configuring each migration.

`SimpleMigrator` comes in two flavours: `SimpleMigrator<TConnection, TMigrationBase>` which allows you to use any sort of database connection (not just ADO.NET) and any migration base class, and `SimpleMigrator`, which assumes you're using ADO.NET and `Migration`.

You'll want to use this class directly in any scenario where you're not using the `ConsoleRunner` to provide a command-line interface for you [see below](#consolerunner).

For example, if you want to migrate to the latest version on startup (not particularly recommended):

```csharp
var migrationsAssembly = Assembly.GetEntryAssembly();
var connection = new ConnectionProvider(/* fetch the connection you normally use in your application */);
var versionProvider = new SQLiteVersionProvider();

var migrator = new SimpleMigrator(migrationsAssembly, connection, versionProvider);
migrator.Load();

migrator.MigrateToLatest();
```

Or, if you wanted to assert that your database was at the latest migration:

```csharp
var migrator = new SimpleMigrator(migrationsAssembly, connection, versionProvider);
migrator.Load();

if (migrator.CurrentMigration.Version != migrator.LatestMigration.Version)
{
    // Throw a 'you must migrate' exception
}
```

You can, of course, migrate to a specific version this way as well, if you want:

```csharp
var migrator = new SimpleMigrator(migrationsAssembly, connection, versionProvider);
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

 - Main application contains the migrations, and uses SimpleMigrations to automatically migrate on startup.
 - Main application contains the migrations, and there is a separate executable to run migrations. This executable references the main application.
 - Dedicated DLL contains the migrations, which is used by both the main application and a separate executable (used to run migrations).


You can customize how migrations are found by implementing `IMigrationProvider`.
This returns a collection of `MigrationData` - see `AssemblyMigrationProvider` for an example.


Example: Using sqlite-net
-------------------------

[sqlite-net](https://github.com/praeclarum/sqlite-net) is a little SQLite driver and micro-ORM.
It is not an ADO.NET implementation, and so makes a good example for seeing how SimpleMigrations work with drivers which don't implement ADO.NET.

First off, create a new Console Application, and install the sqlite-net NuGet package.
You'll also need to download `sqlite.dll` and add it to your project.

Next, we'll need to create a number of classes to tell SimpleMigrations how to work with sqlite-net: we'll need a version provider, connection provider, and a base class for the migrations.

### `IConnectionProvider`

Let's start with the `IConnectionProvider` implementation.
This is a class which knows how to work with transactions.
As you can see, there's not very much to it:

```csharp
public class SQLiteNetConnectionProvider : IConnectionProvider<SQLiteConnection>
{
    public SQLiteConnection Connection { get; }

    public SQLiteNetConnectionProvider(SQLiteConnection connection)
    {
        this.Connection = connection;
    }

    public void BeginTransaction()
    {
        this.Connection.BeginTransaction();
    }

    public void CommitTransaction()
    {
        this.Connection.Commit();
    }

    public void RollbackTransaction()
    {
        this.Connection.Rollback();
    }
}
```

### `IVersionProvider`

Next, the `IVersionProvider` implementation.
This is the class which lets SimpleMigrations work with the 'SchemaVersion' table in your database.
Since sqlite-net includes a micro-ORM, let's use that to create, read, and modify the SchemaVersion table, rather than executing raw SQL.

Note that it's not normally advisable to write migrations using C# models: migrations by definition work with lots of different versions of a table, whereas a C# class can only encapsulate one version of a table.
However, since our SchemaVersion table is never going to change, it's an acceptable shortcut here.

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

And our `IVersionProvider` implementation:

```csharp
public class SQLiteNetVersionProvider : IVersionProvider<SQLiteConnection>
{
    private SQLiteConnection connection;

    public void Setconnection(SQLiteConnection connection)
    {
        this.connection = connection;
    }

    public void EnsureCreated()
    {
        this.connection.CreateTable<SchemaVersion>();
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

Now we don't strictly need to create a migration base class: we could have each migration implement `IMigration<SQLiteConnection>`.
However, if we do create one we can automatically log all executed SQL, which would be nice...

```csharp
public abstract class SQLiteNetMigration : IMigration<SQLiteConnection>
{
    public SQLiteConnection DB { get; set; }

    public IMigrationLogger Logger { get; set; }

    public abstract void Down();

    public abstract void Up();

    public void Execute(string sql)
    {
        this.Logger.LogSql(sql);
        this.DB.Execute(sql);
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
using (var connection = new SQLiteConnection("SQLiteNetDatabase.sqlite"))
{
    var migrationsAssembly = Assembly.GetExecutingAssembly();
    var connectionProvider = new SQLiteNetConnectionProvider(connection);
    var versionProvider = new SQLiteNetVersionProvider();

    var migrator = new SimpleMigrator<SQLiteConnection, SQLiteNetMigration>(
        migrationsAssembly, connectionProvider, versionProvider
    );
    migrator.Load();

    migrator.MigrateToLatest();
}
```