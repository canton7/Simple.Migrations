SimpleMigrations
================

[![Build status](https://ci.appveyor.com/api/projects/status/iub4g6p0qs7onn2b?svg=true)](https://ci.appveyor.com/project/canton7/simplemigrations)

SimpleMigrations is a simple bare-bones migration framework. 
It doesn't provide SQL generation, or an out-of-the-box command-line tool, or other fancy features.
It does however provide a set of simple, extendable, and composable tools for integrating migrations into your application.

Installation
------------

... will be possible when this is on NuGet. 
If you're super-keen, you can grab packages from AppVeyor, see the badge above.


Quick Start
-----------

I'll introduce SimpleMigrator by walking through a basic example.

Here, we'll create a separate console application, which contains all of our migrations and can be invoked in order to migrate the database between different versions.
If you want your application to automatically migrate to the latest version when it's started, or you want to put your migrations in another library, etc, that's easy too: we'll get on to those at the end.

First, create a new Console Application.

The first task is to create (at least one) migration.
Migrations are classes which derive from `Migration`, and are decorated with the `[Migration(versionNumber)]` attribute.
How you number your migrations is up to you: some people like to number them sequentially, while others like to use the current date and time (e.g. `20150105164402`).

In `Migrations/1_CreateUsers.cs`:

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
        var versionProvider = new SQLiteVersionProvider();

        var migrator = new SimpleMigrator(migrationsAssembly, db, versionProvider);

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
It's not necessary to use this by anymeans (you can call methods on `SimpleMigrator` directly), but it saves time when you just want a simple command-line application to run your migrations.

And we're done!
Build your project, and run it with the command-line argument `help` to see what's available.


Migrations
----------

Let's discuss migrations in detail.

A migration is a class which implement `IMigration<TDatabase>`, and is decorated with the `[Migration]` attribute.
`IMigration<TDatabase>` is database-agnostic: it doesn't care what sort of database you're talking to.
All migrations have an `Up` and `Down` method, a `DB` property to access the database connection, and a `Logger` property to log the SQL which is being run and any other messages.

`Migration` is an abstract class which implements `IMigration<IDbConnection>`, providing a base class for all migrations which use an ADO.NET database connection.
It adds an `Execute` method, which logs the SQL it's given (using the `Logger`) and runs it (using the `DB`).

Migrations are executed inside a transaction by default.
This means that either the migration is applied in full, or not at all.
If you need to disable this (some SQL statements in some databases cannot be run inside a transaction, for example), specify `UseTransaction=false` in the `[Migration]` attribute.
It is strongly recommended that migrations which do not execute inside of a transaction do only a single thing: use multiple migrations if necessary.

If you want to perform more complicated operations on the database (e.g selecting data, or inserting variable data) you'll have to use the `DB` property directly.
[`Dapper`](https://github.com/StackExchange/dapper-dot-net) may make your life easier here, or you can create your own migration base class which uses a specific type of database connection, and add your own helpers methods to it.
See TODO below for more details on how to do this.


Version Providers
-----------------

SimpleMigrations aims to be as database-agnostic as possible.
However, there is one place where SimpleMigrations needs to talk to the database directly: the schema version table.
SimpleMigrations uses a table inside of your database to track which version the database is at, i.e. which migration was most recently applied.
In order to manipulate this, it needs to communicate with your database.
It does this using version providers.

A version provider is a class which implements `IVersionProvider<TDatabase>`.
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

 - SQLite (`VersionProviders.SQLiteVersionProvider`)
 - PostgreSQL (`VersionProviders.PostgresSQLVersionProvider`)


Connection Providers
--------------------

SimpleMigrations needs to be able to create, commit, and rollback transactions.
In order to encapsulate this functionality, the interface `IConnectionProvider<TDatabase>` exists.
This is implemented by `ConnectionProvider` for ADO.NET.

Most of the time you won't need to think about this.
However, if you're using a database connection which is not based on ADO.NET, you'll need to write your own connection provider, TODO see below.


`SimpleMigrator`
----------------

`SimpleMigrator` is where it all kicks off.
This class provides access to the list of all migrations, the current migration, and methods to migrate to a particular version (or straight to the latest version).
It is also responsible for finding migrations, and for instantiating and configuring each migration.

`SimpleMigrator` comes in two flavours: `SimpleMigrator<TDatabase, TMigrationBase>` which allows you to use any sort of database connection (not just ADO.NET) and any migration base class, and `SimpleMigrator`, which assumes you're using ADO.NET and `Migration`.


`ConsoleRunner`
---------------

`ConsoleRunner` is a little helper class which aims to be useful if you're writing a command-line application to perform your migirations.
It uses a `SimpleMigrator`, takes your command-line arguments, and uses the latter to decide what methods on the former to execute.

You can use it, extend it (look at the `SubCommands` property), or write your own version: it's not critical in any way.

