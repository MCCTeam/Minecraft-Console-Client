# Chapter 8: Test and diagnose

Test a script in layers. Start with calculations. Then test events. Finally check server behavior.

## Check syntax and formatting

1. Save the actual `.bcn` file.
2. Run lint.

```sh
./Mcc.Cli lint /full/path/script.bcn
```

3. Check formatting without changing the file.

```sh
./Mcc.Cli format /full/path/script.bcn --check
```

4. Apply formatting when you want MCC to rewrite the layout.

```sh
./Mcc.Cli format /full/path/script.bcn
```

Formatting changes spacing and block layout. It does not prove that the script performs the intended action.

## Check calculations and timers

1. Use a fixed random seed.
2. Advance virtual time when your file registers timers.

```sh
./Mcc.Cli run /full/path/schedule.bcn --seed 42 --tick 30
```

`--tick 30` advances the test clock once after loading. It does not wait for 30 real seconds.

A recurring timer runs at most once for this advance. Offline execution does not replay every missed interval.

A top-level `await` can wait for a task before the virtual clock advances. Use normal MCC for examples that await real sleeps.

## Read a diagnostic

| Information | Meaning | What to inspect |
| --- | --- | --- |
| File and line | Source location | The statement and its surrounding block |
| Code | Stable diagnostic category | The related syntax, capability, or runtime problem |
| Message | What failed | The input values and current session |
| Hint | Suggested correction | Whether it matches your intended action |

Check errors before warnings. Fix the first error before investigating later errors.

Do not remove a capability declaration just to hide a warning. Check whether the script uses the operation.

## Separate three failures

A syntax failure means Beacon cannot understand the file. For example, `end if` cannot close a `for each` block.

A capability failure means the script does not declare an operation or its provider is unavailable. A declaration alone cannot enable world tracking.

A game failure means an action cannot complete in the current session. MCC can be disconnected, a container can close, or the server can refuse.

## Check a live handler

1. Use a server that you control.
2. Enable the tracking that the script requires.
3. Connect MCC before invoking game actions.
4. Load the script with `/scripts run <id> --trace`.
5. Send the trigger from a second player when the handler expects a sender.
6. Check MCC's diagnostic and the server result.
7. Send an unrelated trigger to check the filter.
8. Stop the script after the check.

`--trace` shows the load's structured trace. It does not promise a complete trace of every later event.

A successful load does not execute every handler. Test each event and command that matters. Test one unavailable prerequisite too.

For repeatable simulated-event tests, use [DMCBK's test host](https://github.com/MCCTeam/DMCBK/blob/master/docs/beacon/guide/08-testing.md).

Next: [Chapter 9: Run the complete helper](09-complete-helper.md).
