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
end

directory 'build/pkg'

task :default => :build
