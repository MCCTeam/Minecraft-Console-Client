# /scripts

Manage Beacon scripts.

## Syntax

```text
/scripts [list|run <id|file> [--trace]|stop <id|all>|reload [id]|lint <file> [--format text|json]|new <template> <id>|mute [on|off]|repl [--seed <n>] <line>|watch [on|off]|config <id> [key value]|format <file> [--check]|ui]
```

## Forms

- `/scripts list`: running scripts and the mute state
- `/scripts run <id|file> [--trace]`: load and run a script from the scripts folder
- `/scripts stop <id|all>`: stop one script, or every script at once
- `/scripts reload [id]`: re-run one script, or every running script, from disk
- `/scripts lint <file> [--format text|json]`: check a file with the shared lint engine
- `/scripts new <template> <id>`: scaffold a script from a working template
- `/scripts mute [on|off]`: gag script chat while logic runs, or show the held count
- `/scripts repl [--seed <n>] [line]`: evaluate one line against the live session
- `/scripts watch [on|off]`: opt-in hot-reload when a running script file changes
- `/scripts config <id> [key value]`: show a script settings file, or set one key
- `/scripts format <file> [--check]`: normalize layout and print the diff
- `/scripts ui`: open the visual script workbench

## Flags

- `--format text|json`: lint output: the human view, or the agent-scriptable JSON document
- `--trace`: print the structured per-line trace after run
- `--seed <n>`: replay one repl line with a fixed RNG seed
- `--check`: format dry run: print the diff without writing

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/scripts list
/scripts run welcome
/scripts run welcome --trace
/scripts lint welcome.bcn --format json
/scripts new welcome mybot
/scripts mute on
/scripts repl show 1 + 2
/scripts stop all
/scripts format welcome
/scripts ui
```

## Behavior and requirements

Script IDs normally name `.bcn` files in the `scripts` folder beside your configuration folder. Use a quoted path when it contains spaces.

`run` loads a script and its event handlers. `stop` cancels its work and removes scoped registrations. `reload` reads source from disk again. The bare reload form reloads running scripts.

Templates include `welcome`, `guard`, `shop`, and `empty`. `new` creates source in the script folder. `config` displays or updates a script's declared settings.

`mute on` suppresses script chat while logic continues. It is not a sandbox and does not prevent every possible side effect. `watch on` reloads running scripts after source changes.

`repl` evaluates a line against the live script runtime. `--seed` fixes random input for that evaluation. `--trace` shows the structured run trace. `lint` and `format` share the offline tooling engines.

Read [Beacon](../beacon/index.md) and [the chaptered tutorial](../beacon/guide/index.md) before relying on game events or persistent state.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
