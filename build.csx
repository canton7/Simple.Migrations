#!/usr/bin/env dotnet-script

#r "nuget: SimpleTasks, 0.9.4"

using SimpleTasks;
using static SimpleTasks.SimpleTask;

#nullable enable

string simpleMigrationsDir = "src/Simple.Migrations";

string testsDir = "src/Simple.Migrations.UnitTests";

string nugetDir = "NuGet";

CreateTask("build").Run((string versionOpt, string configurationOpt) =>
{
    var flags = CommonFlags(versionOpt, configurationOpt);
    Command.Run("dotnet", $"build {flags} \"{simpleMigrationsDir}\"");
});

CreateTask("package").DependsOn("build").Run((string version, string configurationOpt) =>
{
    var flags = CommonFlags(version, configurationOpt) + $" --no-build --output=\"{nugetDir}\"";
    Command.Run("dotnet", $"pack {flags} \"{simpleMigrationsDir}\"");
});

string CommonFlags(string? version, string? configuration) =>
    $"--configuration={configuration ?? "Release"} -p:VersionPrefix=\"{version ?? "0.0.0"}\"";

CreateTask("test").Run(() =>
{
    Command.Run("dotnet", $"test \"{testsDir}\"");
});

return InvokeTask(Args);
