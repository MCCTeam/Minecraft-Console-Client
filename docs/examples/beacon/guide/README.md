# Chaptered Beacon examples

These files accompany [Make a Beacon script](../../../beacon/guide/index.md). Use MCC’s one-shot runner in Chapter 1 for game-free examples. Use a controlled live MCC session in Chapter 4 for event examples.

| File | Expected behavior |
| --- | --- |
| `hello.bcn` | Print `Hello from Beacon` |
| `values.bcn` | Print the order size and `Total: 15` |
| `functions.bcn` | Check two calculations and print `Subtotal: 12` |
| `greeter.bcn` | Whisper a greeting for an exact `!hello` event |
| `schedule.bcn` | Print one-shot and recurring timer results |
| `settings.bcn` | Print the default `Hello` setting |
| `saved-counter.bcn` | Increment the persisted count with a configuration folder |
| `price.bcn` | Register `/guide-price <item>` |
| `shop.bcn`, `shop-caller.bcn` | Export a function and call the running provider |
| `welcome-helper.bcn` | Greet a sender, persist the greeting count, and register `/guide-stats` |

MCC supplies the connection and configuration paths. Copy live examples into the scripts directory beside your configuration directory. Load them with /scripts run <id>.
