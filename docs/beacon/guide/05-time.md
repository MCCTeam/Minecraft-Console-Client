# Chapter 5: Add scheduled work

A timer runs work after time passes. A cooldown limits how often a handler receives an event. Use timers for reminders and cooldowns for repeated requests.

## Create two timers

1. Open a terminal in the MCC application directory.
2. Create `schedule.bcn` with this complete script.

```beacon
# beacon 1
in 5 seconds do
  show "One reminder"
end in
every 10 seconds
  show "Status check"
end every
```

3. Execute `./Mcc.Cli run /full/path/schedule.bcn --tick 30`.

The runner advances its clock once by 30 seconds. It reports one `Status check` and one `One reminder`. The offline runner checks recurring timers before one-shot timers. This output order does not represent chronological playback.

A recurring timer does not run three times to replay every missed interval. One clock jump can trigger it at most once. To test separate intervals, use `VirtualClock` with `BeaconEngine` and advance it between ticks.

## Wait inside a task

`wait` pauses the current task. It lets the scheduler run other work. A long calculation without a wait can exhaust its execution budget.

```beacon
# beacon 1
function delayed_report()
  wait 1 second
  show "Report complete"
end function
start delayed_report()
set active to tasks()
if len(active) > 0 then
  await active[0].id
end if
```

`start` is a statement. It does not return a task ID for assignment. `tasks()` gives live task records. `await` waits for the selected ID. `cancel task id` requests cancellation.

Run this task example with `/scripts run <id>` in normal MCC. The offline runner advances virtual time only after loading finishes.

## Limit repeated requests

```beacon
# beacon 1
# needs: chat.send
on chat as e cooldown 10 seconds named "hello" when e.message is "!hello"
  whisper e.player "Hello, {e.player}!"
end on
```

The named cooldown limits this handler across senders. It is not a separate timer for each player. The chat bucket applies another limit across all scripts.

The current runtime checks the cooldown before the `when` filter. An unrelated chat event can consume the window. Use this form only when that behavior fits your handler. An exact chat command without cooldown is clearer for the first helper.

Reconnect cancels pending waits and one-shot timers. Do not assume an old scheduled action completes in a new session. Recurring work must also handle unavailable game state.

Next: [Chapter 6: Remember settings and state](06-state.md).
