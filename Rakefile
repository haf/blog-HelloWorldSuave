require 'bundler/setup'

require 'albacore'
require 'albacore/tasks/versionizer'
require 'albacore/ext/teamcity'

Albacore::Tasks::Versionizer.new :versioning

desc 'Perform fast build (warn: doesn\'t d/l deps)'
build :quick_build do |b|
  b.logging = 'detailed'
  b.sln     = 'HelloWorldSuave.sln'
end

desc 'restore all nugets as per the packages.config files'
nugets_restore :restore do |p|
  p.out = 'packages'
  p.exe = 'tools/NuGet.exe'
end

desc 'Perform full build'
build :build => [:versioning, :restore] do |b|
  b.sln = 'HelloWorldSuave.sln'
  # alt: b.file = 'src/MyProj.sln'
end

directory 'build/pkg'

desc 'package nugets - finds all projects and package them'
nugets_pack :create_nugets => ['build/pkg', :versioning, :build] do |p|
  p.files   = FileList['src/**/*.{csproj,fsproj,nuspec}'].
    exclude(/Tests/)
  p.out     = 'build/pkg'
  p.exe     = 'tools/NuGet.exe'
  p.with_metadata do |m|
    m.description = 'A cool nuget'
    m.authors = 'Henrik'
    m.version = ENV['NUGET_VERSION']
  end
  p.with_package do |p|
    p.add_file 'file/relative/to/proj', 'lib/net40'
  end
end

task :default => :create_nugets
