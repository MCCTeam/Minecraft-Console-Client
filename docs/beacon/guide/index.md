# Make a Beacon script

This guide starts with a local calculation. It finishes with a helper that answers chat, remembers a count, and registers a command. Each chapter explains the new syntax before you use it.

You do not need a Minecraft server for the first three chapters. Chapter 4 uses a real MCC connection on a server that you control.

A script is a text file. A host is the application that runs that file. DMCBK supplies the interpreter. MCC supplies the connection, paths, and terminal interface.

| Chapter | What you will make | What you will learn |
| --- | --- | --- |
| [1. Create and run a file](01-first-file.md) | A local greeting | MCC setup, `.bcn`, headers, output, diagnostics |
| [2. Work with values](02-values.md) | An order calculator | Variables, lists, maps, conditions, loops |
| [3. Reuse a calculation](03-functions.md) | A tested helper function | Parameters, results, imports, assertions |
| [4. Answer an event](04-events.md) | A chat helper | Event records, filters, capabilities, live checks |
| [5. Add scheduled work](05-time.md) | A status reminder | Timers, virtual time, waits, tasks, cooldowns |
| [6. Remember settings and state](06-state.md) | A persistent counter | Defaults, saved values, shared state, lifetimes |
| [7. Add commands and integrations](07-integrations.md) | A command and provider caller | Arguments, exports, optional plugins, failures |
| [8. Test and diagnose](08-testing.md) | Repeatable script checks | Lint, format, failure reports, game prerequisites |
| [9. Run the complete helper](09-complete-helper.md) | A complete chat helper | Installation, connection, reload, cleanup |

Read the chapters in order for your first script. After that, use the [language reference](../language.md), [game reference](../events-and-game.md), and [recipes](../recipes.md).

The complete files are in [example files](../../examples/beacon/guide/README.md). Each chapter identifies its file and expected result. A successful offline test proves the script's calculations. It does not prove that a server accepts an action.
