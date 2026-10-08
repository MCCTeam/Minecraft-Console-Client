# Create a release package

The MCC installers need a complete archive. A single executable does not contain the managed assemblies required by source plugins.

This page describes local packaging. It does not grant permission to publish a GitHub release.

You need the pinned [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [Python](https://www.python.org/downloads/) 3.10 or later, and [Node.js with npm](https://nodejs.org/en/download).

## Build one target

1. Open the repository terminal.
2. Initialize ConsoleInteractive.
3. Load the build helpers.
4. Publish your target into a new output folder.

```bash
git submodule update --init ConsoleInteractive
source tools/mcc-env.sh
mcc-publish --rid linux-x64 -- -o /tmp/mcc-publish/linux-x64
```

Use a fresh output folder for each target. Do not place runtime configuration, tokens, scripts or installed plugin data in it.

## Create an archive

```bash
python3 tools/package-release.py /tmp/mcc-publish/linux-x64 --tag v2.0.0-preview.1 --rid linux-x64 --output artifacts/releases
```

The tool verifies the executable, managed assembly, dependency manifest and runtime configuration. It keeps every published file in the archive.

Release assets use these names:

```text
Mcc-v2.0.0-preview.1-linux-x64.tar.gz
Mcc-v2.0.0-preview.1-osx-arm64.tar.gz
Mcc-v2.0.0-preview.1-win-x64.zip
SHA256SUMS
```

Archive files sit at the archive root. They do not use an additional parent folder. Unix archives contain `Mcc.Cli`; Windows archives contain `Mcc.Cli.exe`.

The checksum file covers each archive in the output folder. The tool refuses to replace an existing archive. A published payload must remain immutable.

## Target identifiers

| Platform | RIDs |
| --- | --- |
| Windows | `win-x86`, `win-x64`, `win-arm64` |
| Linux with glibc | `linux-x64`, `linux-arm64`, `linux-arm` |
| Linux with musl | `linux-musl-x64`, `linux-musl-arm64`, `linux-musl-arm` |
| macOS | `osx-x64`, `osx-arm64` |

Publish only targets that you can validate. Cross-building does not prove that a binary runs on its target system.

## Check before publication

1. Run `mcc-test`.
2. Run `python3 tools/test_installers.py`.
3. Run `python3 tools/check_docs.py`.
4. Install site dependencies with `npm --prefix docs ci`.
5. Build the documentation with `npm --prefix docs run docs:build`.
6. Extract each archive in an empty directory.
7. Run its help command on the target platform.
8. Check classic and TUI startup on supported terminals.
9. Verify that translated resource assemblies remain in their culture directories.

After the owner approves publication, upload the archives and their shared `SHA256SUMS` file to the matching GitHub release tag.

The default installers select the latest stable GitHub release. Users need an explicit version for a preview tag. The new installer refuses old MCC 1.x releases.
