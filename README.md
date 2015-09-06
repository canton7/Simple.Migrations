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

`Migration` is an abstract class which implement `IMigration<IDbConnection>`, providing a base class for all migrations which use an ADO.NET database connection.
It adds an `Execute` method, which logs the SQL it's given (using the `Logger`) and runs it (using the `DB`).

Migrations are executed inside a transaction by default.
