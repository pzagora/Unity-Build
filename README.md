# Unity Build

Command-line player and Addressables builds for CI (Jenkins, GitHub Actions, …): one build entry point for every
project instead of a copy of a build script in each. Editor-only. It uses Addressables and the
[Reporter](https://github.com/pzagora/Unity-Reporter) package when the project has them, and neither is required.

## Installation

Add the zagora.dev registry and the package to `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    { "name": "zagora.dev", "url": "https://upm.zagora.dev", "scopes": [ "com.zagoradev" ] }
  ],
  "dependencies": {
    "com.zagoradev.unity.build": "1.1.1"
  }
}
```

## Building

Every entry point works on the build target Unity was started with (`-buildTarget`) and throws on failure, which makes
batch-mode Unity exit with code 1:

```bash
Unity -batchmode -nographics -quit -projectPath . -buildTarget Android -logFile - \
  -executeMethod Build.CommandLineBuild.Build -outputPath Builds/Game.apk -buildNumber 42
```

| Method | Does |
|---|---|
| `Build.CommandLineBuild.Build` | Builds the player (enabled scenes from Build Settings) |
| `Build.CommandLineBuild.BuildAddressables` | Builds only the Addressables content |
| `Build.CommandLineBuild.ApplyDefines` | Adds `-defines` to the scripting define symbols and saves them, for a following run such as `-runTests` |

| Argument | Used by | Meaning |
|---|---|---|
| `-outputPath <path>` | Build (required) | Output file or folder. On Android, an `.aab` path builds an App Bundle, anything else an APK. iOS writes an Xcode project folder. |
| `-buildNumber <n>` | Build | Positive whole number: Android version code, iOS and macOS build number |
| `-bundleVersion <x.y.z>` | Build | Player version (`PlayerSettings.bundleVersion`) |
| `-development` | Build | Development player build |
| `-managedCodeVariant <Debug\|Checked\|Instrumented\|Release>` | Build, BuildAddressables | C# diagnostics (Unity 6.6+, `PlayerSettings.SetManagedCodeVariant`); ignored with a warning on older versions |
| `-defines <A;B>` | all | Extra scripting define symbols for the active target, added to the project's own (`;`, `,` or spaces between them) |
| `-buildAddressables` | Build | Build the Addressables content first. Without it, a project with Addressables must already have content built for the target. |
| `-androidDebugSymbols <None\|SymbolTable\|Full>` | Build | Android debug symbols Unity writes next to the build (`*.symbols.zip`, for store crash reports) |
| `-buildReport <path>` | Build | Writes a JSON summary of the build (target, result, size, duration, the 25 largest assets by packed size), also when the build fails |

Passing `-buildNumber` without a value, with a non-number, or with a value below 1 fails the build before Unity changes
the platform build numbers.

The settings are applied to the project's Player Settings in this order: defines, bundle version, build number, managed
code variant, Android signing and debug symbols. A CI checkout should be reset between builds (`git checkout -f`), so they don't stick.

### Android signing

Release keystores come from environment variables, so passwords never appear on the command line or in the project:

| Variable | Meaning |
|---|---|
| `UNITY_BUILD_KEYSTORE` | Path to the keystore. Without it the project's own signing settings apply. |
| `UNITY_BUILD_KEYSTORE_PASSWORD` | Keystore password (required with a keystore) |
| `UNITY_BUILD_KEY_ALIAS` | Key alias (required with a keystore) |
| `UNITY_BUILD_KEY_PASSWORD` | Key password; defaults to the keystore password |

## Project-specific steps

Implement `Build.ICommandLineBuildHook` anywhere in the project's Editor code; it's found automatically:

```csharp
using Build;
using UnityEditor.Build.Reporting;

public sealed class StampVersionHook : ICommandLineBuildHook
{
    public int Order => 0;

    public void BeforeBuild(CommandLineBuildContext context)
    {
        var channel = CommandLineArguments.Get("-channel") ?? "internal";
        // e.g. write the channel into a config asset, change context.Options.scenes, …
    }

    public void AfterBuild(CommandLineBuildContext context, BuildReport report)
    {
        // e.g. upload symbols when report.summary.result == BuildResult.Succeeded
    }
}
```

Hooks run in ascending `Order` after the package's own settings are applied, and `AfterBuild` runs whether the build
succeeded or not. `context.Options` is what gets built and can be changed. `CommandLineArguments` reads any argument.

## Optional packages

- **Addressables** (`com.unity.addressables`) enables `BuildAddressables` and `-buildAddressables` (version define
  `PACKAGE_ADDRESSABLES`).
- **Reporter** (`com.zagoradev.unity.reporter`) routes the build log through `Report` (version define `PACKAGE_REPORTER`).
  When Reporter is embedded under `Assets` instead of installed as a package, add `PACKAGE_REPORTER` to the project's
  scripting define symbols.

## Tests

Package-owned EditMode tests under `Tests/Editor` cover command-line parsing and build-number application, scripting
defines, Android signing validation, build hook discovery/order/context and the Addressables-content prerequisite. Add
`com.zagoradev.unity.build` to the project's `testables` array, then run them with Unity Test Framework:

```bash
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml
```
