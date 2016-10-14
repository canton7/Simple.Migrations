require 'json'

SIMPLEMIGRATIONS_DIR = 'src/Simple.Migrations'

ASSEMBLY_INFO = File.join(SIMPLEMIGRATIONS_DIR, 'Properties/AssemblyInfo.cs')
SIMPLEMIGRATIONS_JSON = File.join(SIMPLEMIGRATIONS_DIR, 'project.json')

desc "Create NuGet package"
task :package do
  sh 'dotnet', 'pack', '--no-build', '--configuration=Release', '--output=NuGet', SIMPLEMIGRATIONS_DIR
end

desc "Bump version number"
task :version, [:version] do |t, args|
  parts = args[:version].split('.')
  parts << '0' if parts.length == 3
  version = parts.join('.')

  content = IO.read(ASSEMBLY_INFO)
  content[/^\[assembly: AssemblyVersion\(\"(.+?)\"\)\]/, 1] = version
  content[/^\[assembly: AssemblyFileVersion\(\"(.+?)\"\)\]/, 1] = version
  File.open(ASSEMBLY_INFO, 'w'){ |f| f.write(content) }

  content = JSON.parse(File.read(SIMPLEMIGRATIONS_JSON, :encoding => 'bom|utf-8'))
  content['version'] = args[:version]
  File.open(SIMPLEMIGRATIONS_JSON, 'w'){ |f| f.write(JSON.pretty_generate(content)) }
end

desc "Build the project for release"
task :build do
  sh 'dotnet', 'build', '--configuration=Release', SIMPLEMIGRATIONS_DIR
end
