# Chapter 7: Build a compiled package and prepare a release

A source package asks the host to compile one entry file. A compiled package contains a DLL that you build before publication.

Use compiled packages when a plugin needs several C# files or private NuGet dependencies. Runtime source installation does not build arbitrary projects.

## Build the entry DLL

Run this command from the SessionJournal project:

```sh
dotnet build -c Release
```

The sample project produces `SessionJournal.dll` and `SessionJournal.deps.json` under `bin/Release/net10.0`.

## Create a staging directory

1. Create an empty directory outside the source package.
2. Copy `SessionJournal.dll` into it.
3. Copy `SessionJournal.deps.json` into it.
4. Copy the manifest.
5. Copy the `defaults`, `lang`, and `man` directories.
6. Change these manifest values:

```toml
kind = "compiled"
target = "any"
entry = "SessionJournal.dll"
```

Keep all other manifest values. Do not include DMCBK, UMPK, or host framework DLLs.

This example has no private helper DLL. A plugin with private dependencies must include them and declare the required paths. See [dependencies and loading](../dependencies-and-loading.md).

## Test the staged package

For optional advanced verification, use the DMCBK sample checkout from [Chapter 1](01-project.md#optional-advanced-verification). Run its verifier with the staging directory:

```sh
dotnet run --project samples/PluginAuthoring/VerifyPlugin -- /absolute/path/to/staging
```

The program must print the same PASS line as the source test. This checks the actual staged DLL through the real compiled loader.

Do not call the author assembly directly as a substitute for this check. Direct calls bypass package validation and assembly resolution.

## Prepare marketplace assets

1. Archive the staging contents.
2. Check that `plugin.toml` is at the archive root.
3. Calculate the archive's SHA-256.
4. Upload the archive as an immutable release asset.
5. Check that its download URL returns the same bytes.
6. Add the catalogue entry.
7. Publish the catalogue after its assets are available.

A release can contain a source archive and a compiled archive. Each archive has its own matching manifest.

The installer downloads one selected asset per plugin in the resolved graph. It does not download every platform build.

`any` works for this managed example. A native dependency needs an asset for each supported process target. Cross-building a target does not prove native execution.

See [marketplace v2](../../../marketplaces/format.md) for catalogue syntax, exact versions, required dependency resolution, hashes, and transactions.

Next: [Maintenance and troubleshooting](08-maintenance.md).


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
