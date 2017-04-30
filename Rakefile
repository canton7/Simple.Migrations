require 'json'

SIMPLEMIGRATIONS_DIR = 'src/Simple.Migrations'
UNIT_TESTS_DIR = 'src/Simple.Migrations.UnitTests'

ASSEMBLY_INFO = File.join(SIMPLEMIGRATIONS_DIR, 'Properties/AssemblyInfo.cs')
SIMPLEMIGRATIONS_JSON = File.join(SIMPLEMIGRATIONS_DIR, 'project.json')

SIMPLEMIGRATIONS_CSPROJ = File.join(SIMPLEMIGRATIONS_DIR, 'Simple.Migrations.csproj')
NUGET_DIR = File.join(File.dirname(__FILE__), 'NuGet')

desc "Create NuGet package"
task :package do
  sh 'dotnet', 'pack', '--no-build', '--configuration=Release', "--output=\"#{NUGET_DIR}\"", '--include-symbols', SIMPLEMIGRATIONS_DIR
end

desc "Bump version number"
task :version, [:version] do |t, args|
  version = args[:version]

  content = IO.read(SIMPLEMIGRATIONS_CSPROJ)
  content[/<VersionPrefix>(.+?)<\/VersionPrefix>/, 1] = version
  File.open(SIMPLEMIGRATIONS_CSPROJ, 'w'){ |f| f.write(content) }
end

desc "Build the project for release"
task :build do
  sh 'dotnet', 'build', '--configuration=Release', SIMPLEMIGRATIONS_DIR
end

desc "Run tests"
task :test do
  Dir.chdir(UNIT_TESTS_DIR) do
    sh 'dotnet', 'test'
  end
end