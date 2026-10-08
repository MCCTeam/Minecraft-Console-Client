# Beacon recipes from MCC

These examples adapt the MIT-licensed [DMCBK Beacon recipes](https://github.com/MCCTeam/DMCBK/blob/master/docs/beacon/recipes.md) for MCC.

The scripts demonstrate chat handlers, game reads, shared state, commands, dialogs and plugin calls. Each recipe has a matching file under [example files](../examples/beacon/README.md).

1. Copy the chosen file into MCC’s scripts directory.
2. Enable the required tracking features in MCC.
3. Load the script with `/scripts run <id>` in MCC.
4. Follow the recipe’s trigger instructions.

A capability declaration grants script access. It does not connect the client or enable world, inventory or entity tracking.

The examples below use live events unless stated otherwise. Offline tests check syntax and simulated behavior. They cannot prove server acceptance.

| Recipe | File |
| --- | --- |
| [Chat quiz](#quiz) | [quiz.bcn](../examples/beacon/quiz.bcn) |
| [TPS warning with a cooldown](#tps-guard) | [tps-guard.bcn](../examples/beacon/tps-guard.bcn) |
| [Inventory report](#inventory-report) | [inventory-report.bcn](../examples/beacon/inventory-report.bcn) |
| [Find nearby chests](#nearby-chests) | [nearby-chests.bcn](../examples/beacon/nearby-chests.bcn) |
| [Entity census](#census) | [census.bcn](../examples/beacon/census.bcn) |
| [A command with an argument](#price-command) | [price-command.bcn](../examples/beacon/price-command.bcn) |
| [A shared counter](#shared-counter) | [shared-counter.bcn](../examples/beacon/shared-counter.bcn) |
| [Answer a team-selection dialog](#team-dialog) | [team-dialog.bcn](../examples/beacon/team-dialog.bcn) |
| [Use an optional plugin](#optional-pricing) | [optional-pricing.bcn](../examples/beacon/optional-pricing.bcn) |

<a id="quiz"></a>

## Chat quiz

Adapted from MCC’s quiz-night example. A map holds the current round and each player’s score.

Run the script. Send `!quiz` in public chat. Whisper `creeper` to the bot. The first matching answer wins.

```beacon
# beacon 1
# needs: chat.send

set quiz to {running: no, q: "", a: "", wins: {}}

on chat when message is "!quiz"
  if quiz.running is yes
    whisper player "A round is already running: {quiz.q}"
    stop event
  end if
  set quiz.running to yes
  set quiz.q to "What mob explodes when it gets close?"
  set quiz.a to "creeper"
  say "Quiz! {quiz.q} First correct whisper wins."
end on

on whisper when quiz.running is yes and lower(trim(message)) is quiz.a
  set quiz.running to no
  set wins to quiz.wins
  set wins[player] to (wins[player] or 0) + 1
  set quiz.wins to wins
  say "{player} got it! Wins total: {wins[player]}. Say !quiz for another round."
end on
```

Requires a live chat session. Scores stay in memory until the script reloads. Exact matching prevents unrelated messages from winning.

<a id="tps-guard"></a>

## TPS warning with a cooldown

Adapted from MCC’s TPS guard. The cooldown prevents repeated warnings during sustained lag.

Run the script while connected. The handler warns when a delivered TPS event reports fewer than 15 ticks per second.

```beacon
# beacon 1
# needs: chat.send

on tps cooldown 300 seconds named "tps-warn" when tps < 15
  say "Heads up: server TPS is {tps} ({server.mspt} ms/tick). Easy on the farms."
end on
```

Requires TPS observations from the host. The cooldown controls warnings independently of the shared chat allowance.

<a id="inventory-report"></a>

## Inventory report

Adapted from MCC’s inventory reporter. This version waits for a chat request and catches unavailable inventory reads.

Enable inventory tracking. Run the script. Send `!inventory` in public chat. Read the report in local output.

```beacon
# beacon 1
# needs: inventory.read
on chat when message is "!inventory"
  try
    show "Carrying {inv.count("torch")} torches."
    for each s in inv.list()
      show "#{s.slot} {s.count}x {s.name} ({s.type})"
    end for
    show "Wearing: {inv.armor()}"
  catch err
    show "Inventory report failed: {err.message}"
  end try
end on
```

This script prints locally and does not move items. Item text matchers check type IDs or display names. The `minecraft:` prefix is optional.

<a id="nearby-chests"></a>

## Find nearby chests

Adapted from MCC’s prospecting example. It reads tracked terrain and reports up to five matches.

Enable terrain tracking. Run the script. Send `!chests` in public chat.

```beacon
# beacon 1
# needs: chat.send world.read world.search
on chat when message is "!chests"
  try
    set found to world.find_blocks("chest", 32, 5)
    if found is empty
      say "No tracked chests within 32 blocks."
    else
      for each c in found
        say "Chest at {c.x} {c.y} {c.z}."
      end for
    end if
  catch err
    show "Chest search failed: {err.message}"
  end try
end on
```

Search covers tracked chunks. An empty result does not prove that unexplored chunks contain no chests. This script does not open or change blocks.

<a id="census"></a>

## Entity census

Adapted from MCC’s pasture census. A reusable function supplies the same report on demand or every five minutes.

Enable entity tracking. Run the script. Send `!census` in public chat.

```beacon
# beacon 1
# needs: chat.send entity.read

function census()
  set cows to entities.count("cow", 48)
  set sheep to entities.count("sheep", 48)
  set pigs to entities.count("pig", 48)

  return "Pasture census: {cows} cows, {sheep} sheep, {pigs} pigs."
end function

every 300 seconds
  say census()
end every

on chat when message is "!census"
  say census()
end on
```

Counts describe tracked entities within 48 blocks. They do not describe every animal on the server.

<a id="price-command"></a>

## A command with an argument

Adapted from MCC’s price command. The command reads a saved price and uses a default when no value exists.

Run the script. Invoke the internal command `/price bread` in the host.

```beacon
# beacon 1
# needs: chat.send

# desc: Look up today's price.
# example: /price bread
command "/price <item>"
  set target to arg("item") or "bread"
  set p to saved("price_" + target) or 10
  say "{target} costs {p} coins today."
end command
```

This registers an internal client command. It does not add a command to the Minecraft server. Saved prices require a configured storage path.

<a id="shared-counter"></a>

## A shared counter

Adapted from MCC’s cooperating-script example. A lock protects a shared read-and-write operation.

Run the script twice in the same client. The second run prints the next count.

```beacon
# beacon 1
lock shared
  set n to shared["quiz.plays"] or 0
  set shared["quiz.plays"] to n + 1
end lock
show "Shared count: {shared["quiz.plays"]}"
```

Shared state belongs to one client and stays in RAM. It does not survive process restart. Use namespaced keys to avoid collisions.

<a id="team-dialog"></a>

## Answer a team-selection dialog

Adapted from MCC’s dialog examples. This version filters on the stable field key and reports errors locally.

Set `team` to the value that your server accepts. Inspect the dialog’s input keys and button order before enabling the script.

```beacon
# beacon 1
# needs: dialog.read dialog.write
# setting team = "blue"
on dialog as d when d.input_keys contains "team_choice"
  try
    if dialog.answer({team_choice: settings.team}, 2)
      show "Team answer submitted."
    else
      show "Team answer refused."
    end if
  catch err
    show "Team dialog failed: {err.message}"
  end try
end on
```

Button numbers start at one. This server-specific example uses button two. Submission does not prove that the server accepted the team choice.

<a id="optional-pricing"></a>

## Use an optional plugin

Adapted from MCC’s optional shop integration. This version uses the `shop-tools` function from the DMCBK plugin guide.

Load `shop-tools` if available. Run the script. Send `!subtotal` in public chat.

```beacon
# beacon 1
# needs: chat.send
# wants: shop.calculate
extern subtotal from "shop-tools"
on chat when message is "!subtotal"
  try
    say "Four items at three coins each cost {subtotal(3, 4)} coins."
  catch err
    show "Pricing unavailable: {err.message}"
  end try
end on
```

The optional capability permits loading without the provider. Calls still fail catchably when the provider is absent. Follow the plugin guide’s Beacon initialization sequence.

## Check a recipe with a simulated host

`ScriptTestHost` supplies captured chat and adjustable game reads. This test drives the quiz through its start and winning-answer events:

```csharp
using DMCBK.Core.Beacon;
using DMCBK.Testing;

string source = await File.ReadAllTextAsync("quiz.bcn");
var host = new ScriptTestHost();
var engine = new BeaconEngine(host);
try
{
    var run = await engine.RunScriptAsync("quiz", source);
    if (!run.Success)
        throw new InvalidOperationException("Quiz failed to load.");
    await engine.FireEventAsync("chat", BeaconEventFields.Chat("Alice", "!quiz"));
    await engine.FireEventAsync("whisper", BeaconEventFields.Whisper("Bob", "creeper"));
    if (!host.Says.Any(line => line.Contains("Bob got it!", StringComparison.Ordinal)))
        throw new InvalidOperationException("The winning answer did not produce a result.");
}
finally
{
    engine.RemoveScript("quiz");
}
```

Add `DMCBK.Beacon` and `DMCBK.Testing` to the test project. Run it from the directory that contains `quiz.bcn`.

For a live MCC session, use [the MCC script workflow](getting-started.md#run-in-mcc). For the optional pricing provider, see [Beacon plugin integration](../plugins/development/beacon-integration.md).

## Adapt a recipe without losing its checks

1. Copy the matching sample file before editing it.
2. Change one setting or condition.
3. Lint the complete file.
4. Run its simulated trigger.
5. Check the exact output or requested action.
6. Check the behavior on your controlled server when the recipe uses game state.

A recipe's header lists script requirements. The client still needs corresponding tracking and session support. A capability does not add chunks or entities that the client never received.

For a chat recipe, test the exact trigger and one unrelated message. For a cooldown recipe, test two immediate triggers. For a container or dialog recipe, test absence and closure as well as the expected open state.

These recipes are independent examples. Loading every sample can register overlapping handlers or commands. Start with the one recipe that matches your use case.

For a gradual introduction, use the [nine-chapter script guide](guide/index.md) before adapting game recipes.
