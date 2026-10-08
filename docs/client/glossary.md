# Glossary

These terms appear in the client, script, plugin, and marketplace guides.

| Term | Meaning in these guides |
| --- | --- |
| Terminal | The operating system window where you enter shell commands and start MCC |
| CLI | Command-line interface, including startup options and a text prompt |
| TUI | Terminal user interface, a full-screen interface inside a terminal |
| Shell | The program that interprets operating system commands, such as Bash or PowerShell |
| Working directory | The folder used to resolve a process's relative paths |
| Absolute path | A path that identifies a location independently of the working directory |
| Configuration folder | The directory that holds MCC's TOML settings and supporting runtime files |
| Profile | A saved account or server entry, selected through its local name |
| Session | One active connection and its Minecraft state |
| Protocol | The Minecraft message format used between client and server |
| Snapshot | The client's current recorded view of game data |
| Feature | A game subsystem, such as terrain, inventory, or entities |
| Module | A composed client service, such as Commands, Beacon, or Plugins |
| Command prefix | The character that identifies local MCC commands, `/` by default |
| TOML | A text format for settings, using keys, values, tables, and arrays |
| Beacon | MCC's scripting language, with source files ending in `.bcn` |
| Event handler | Script or plugin code that runs when a named event occurs |
| REPL | An evaluator that accepts short statements and retains its own values between them |
| Source plugin | A plugin whose selected package supplies a C# entry file for runtime compilation |
| Compiled plugin | A plugin whose selected package supplies a managed DLL entry |
| Native dependency | Compiled machine code tied to an operating system and processor architecture |
| API | Application programming interface, the contracts that code uses to call another component |
| SDK | Software development kit, the contracts and tools used to author an extension |
| NuGet | The package system used to restore .NET libraries |
| Manifest | A package's `plugin.toml`, which declares identity, entry, compatibility, and dependencies |
| Marketplace index | The list of plugin identities and paths to their release catalogues |
| Release catalogue | A plugin's available versions and their downloadable assets |
| Asset | One source or compiled archive for a release and target |
| Dependency | Another plugin or library required by a consumer |
| Version range | A rule that permits a set of versions, such as `>=2.0.0 <3.0.0` |
| SemVer | Semantic Versioning, the major.minor.patch version form with optional prerelease labels |
| Pin | A saved restriction that prevents a managed plugin from changing version |
| Lock | The file that records exact installed versions, assets, hashes, and source bindings |
| Transaction | One planned change to the installed plugin graph |
| Rollback | Restoration of a retained package selection, without reversing arbitrary plugin side effects |
| RID | .NET runtime identifier, such as `linux-x64` or `osx-arm64` |
| Process architecture | The architecture of the running MCC process, which can differ from the machine's architecture |
| glibc / musl | Different Linux system C libraries, relevant when selecting native binary assets |
| SHA-256 | The digest algorithm used to check that downloaded bytes match release metadata |
| Token | A secret value that can authorize account or service access |
| Culture tag | A language/region identifier, such as `de` or `pt-BR` |

A valid command can still fail a feature or session check. A valid checksum can still identify unsafe code. These checks answer different questions.

[Documentation home](../index.md) · [Command reference](../commands/index.md)
