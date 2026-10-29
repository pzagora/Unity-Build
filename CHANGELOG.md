# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this package adheres to Semantic Versioning.

## [1.1.0]

### Added
- `-androidDebugSymbols <None|SymbolTable|Full>`: Android debug symbols zip for store crash reports.
- `-buildReport <path>`: JSON summary of the player build (target, result, size, duration, largest assets), written
  whether or not the build succeeded.

## [1.0.0]

### Added
- `Build.CommandLineBuild.Build`: command-line player builds with `-outputPath` (an `.aab` path builds an Android App
  Bundle), `-buildNumber`, `-bundleVersion`, `-development`, `-managedCodeVariant` (Unity 6.6+), `-defines` and
  `-buildAddressables`; fails the run (exit code 1) on any error.
- `Build.CommandLineBuild.BuildAddressables` and `Build.CommandLineBuild.ApplyDefines` for content-only builds and for
  applying define symbols before a test run.
- Android release signing from the `UNITY_BUILD_KEYSTORE*` / `UNITY_BUILD_KEY_*` environment variables.
- `ICommandLineBuildHook` for project-specific steps before and after the build, and `CommandLineArguments` for reading
  custom arguments.
- Optional Addressables and Reporter support through version defines.
