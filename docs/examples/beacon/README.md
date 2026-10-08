# Beacon examples

These files match the [Beacon guides](../../beacon/index.md).

| Script | Purpose |
| --- | --- |
| `total.bcn` | Calculate a total and assert the result |
| `order.bcn` | Import a helper from `lib/math.bcn` |
| `timers.bcn` | Schedule one-shot and recurring output |

Run these files with MCC's offline tool. Replace paths with your copied files' full paths.

```sh
./Mcc.Cli lint /full/path/total.bcn
./Mcc.Cli run /full/path/total.bcn
./Mcc.Cli run /full/path/order.bcn
./Mcc.Cli run /full/path/timers.bcn --tick 60
```

Keep `order.bcn` beside its `lib` directory so its import resolves.

For live recipes, copy each file into the `scripts` directory beside your MCC configuration directory. Load it with `/scripts run <id>`.

`total.bcn`, `order.bcn`, and `timers.bcn` print locally. Game and event recipes need the prerequisites described below.

## Practical recipes

See [recipes from MCC](../../beacon/recipes.md) for the complete scripts, triggers and prerequisites.

- [Chat quiz](quiz.bcn)
- [TPS warning with a cooldown](tps-guard.bcn)
- [Inventory report](inventory-report.bcn)
- [Find nearby chests](nearby-chests.bcn)
- [Entity census](census.bcn)
- [A command with an argument](price-command.bcn)
- [A shared counter](shared-counter.bcn)
- [Answer a team-selection dialog](team-dialog.bcn)
- [Use an optional plugin](optional-pricing.bcn)

The [chaptered guide samples](guide/README.md) add beginner exercises for values, events, timers, commands, and persistent state.
