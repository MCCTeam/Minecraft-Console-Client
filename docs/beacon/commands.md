# Beacon command reference

MCC has two Beacon command surfaces. Terminal commands run once and exit. `/scripts` commands operate inside a running MCC client.

These examples use the default internal command prefix, `/`. Run terminal commands from the application directory. Use full script paths when possible.

## Terminal commands

```text
Mcc.Cli lint <file...> [--format text|json] [--target-lib N] [--strict] [--fix]
Mcc.Cli run <file...> [--seed N] [--tick S] [--format text|json]
Mcc.Cli format <file...> [--check]
```

All three commands also accept `--stdin` and `--stdin-name <name>`. `--stdin` reads piped source instead of files.

| Option | Applies to | Meaning |
| --- | --- | --- |
| `--format text` | lint, run | Print a readable report. This is the default. |
| `--format json` | lint, run | Print machine-readable diagnostics and results. |
| `--target-lib N` | lint | Check against an explicit library version. The current library version is `2`. The source header remains `# beacon 1`. |
| `--strict` | lint | Treat unresolved providers and unverifiable cross-script/plugin calls as errors. |
| `--fix` | lint | Apply supported corrections to files. Review the resulting diff. |
| `--seed N` | run | Choose an integer random seed. The default is `1234`. |
| `--tick S` | run | Advance virtual time once after loading. Default: `0` seconds. |
| `--check` | format | Report changes without writing files. |
| `--stdin` | all | Read source from standard input instead of files. |
| `--stdin-name name.bcn` | all | Set the source name used in reports. |

A one-shot command runs before normal configuration loading. It does not create a login session or generate a configuration directory.

A `run` command can load event handlers, but it does not deliver Minecraft events. Reads use the offline host's empty observations.

```sh
./Mcc.Cli lint /full/path/total.bcn --strict
./Mcc.Cli run /full/path/total.bcn --seed 42 --format json
./Mcc.Cli run /full/path/timers.bcn --tick 60
./Mcc.Cli format /full/path/total.bcn --check
```

To pass piped source on a POSIX shell:

```sh
printf '%s\n' '# beacon 1' 'show 2 + 3' | ./Mcc.Cli run --stdin --stdin-name calculation.bcn
```

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | The operation succeeded. Warnings do not fail normal lint. Strict lint can escalate provider warnings to errors. |
| `1` | A script failed, lint found errors, or `format --check` found a change. |
| `2` | Arguments were invalid or a file could not be read or written. |

Normal `format` rewrites files. `format --stdin` returns formatted source on standard output. It does not rewrite a file.

## Commands inside MCC

Type these commands at the MCC input prompt. Do not enter them as operating-system commands.

- `/scripts`, `/scripts list`: List running IDs and mute state. Example: `/scripts list`.
- `/scripts run <id|file> [--trace]`: Load source and register its handlers. Example: `/scripts run welcome --trace`.
- `/scripts stop <id|all>`: Stop one script or all scripts. Example: `/scripts stop all`.
- `/scripts reload [id]`: Reload one file, or all running files. Example: `/scripts reload welcome`.
- `/scripts lint <file...> [options]`: Check source with the shared lint engine. Example: `/scripts lint welcome --format json`.
- `/scripts new <template> <id>`: Create a complete template without overwriting a file. Example: `/scripts new welcome mybot`.
- `/scripts mute [on|off]`: Show or change script chat suppression. Example: `/scripts mute on`.
- `/scripts repl [--seed N] [line]`: Evaluate a line against the live session. Example: `/scripts repl show 1 + 2`.
- `/scripts watch [on|off]`: Show or change automatic reload after file changes. Example: `/scripts watch on`.
- `/scripts config <id> [key value]`: Show settings or change a declared setting. Example: `/scripts config welcome prefix Hello`.
- `/scripts format <file> [--check]`: Format a script or report a diff. Example: `/scripts format welcome --check`.
- `/scripts ui`: Open the script manager in TUI mode. Example: `/scripts ui`.
- `/help scripts`: Show command usage. Example: `/help scripts`.

`run`, `lint`, and `format` can resolve a bare script ID in the configured scripts directory. An existing explicit file path takes priority.

For text with spaces, quote the value. For a declared yes/no setting, use `yes` or `no`.

```text
/scripts config welcome-helper prefix "Welcome back"
/scripts reload welcome-helper
```

Lint options match the terminal lint engine. In-client JSON output returns the report as a successful command message, even when the report contains errors. Inspect its `summary` and diagnostics.

### Templates

| Name | Starting behavior |
| --- | --- |
| `empty` | Print a local announcement when loading. |
| `welcome` | Greet players on tab-list join and remember seen players. |
| `guard` | Warn when health or food drops below its threshold. |
| `shop` | Answer `!price` messages using a saved price map. The map starts empty. |

Templates create `<id>.bcn`. Use letters, numbers, hyphens, and underscores for IDs.

### Stop, reload, mute, and watch

Stopping removes handlers, timers, commands, and exports. It does not remove saved state or settings.

Reload creates new globals and registrations. The script can restore saved state during loading. Inspect diagnostics after every reload.

Mute suppresses script chat while the logic continues. It does not stop inventory actions, movement, file writes, or network calls.

Watching is opt-in. Start with manual reload while learning. An automatic reload can reset globals and run top-level side effects again.

### REPL

A REPL is an interactive evaluator. Each `/scripts repl` line shares its REPL locals with later lines.

```text
/scripts repl set total to 3 * 4
/scripts repl show total
```

The second command prints `12`. The REPL uses the live session when available. Game operations can therefore affect that session.

Use the one-shot `run` command for isolated checks. Use the REPL for short interactive observations.
