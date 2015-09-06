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

Let's take a look at 
