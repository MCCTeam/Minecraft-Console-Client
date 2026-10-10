# Beacon language

Beacon uses labeled blocks and one assignment form: `set name to value`. Each script owns its globals. Function parameters and locals belong to that function call.

## Values and expressions

| Kind | Example | Notes |
| --- | --- | --- |
| Text | `"Steve"` | Interpolation uses `{expression}`. Literal braces use `{{` and `}}`. |
| Number | `12.5` | Arithmetic and numeric comparisons use numbers. |
| Yes/no | `yes`, `no` | Conditions require this kind. |
| List | `[3, 5, 7]` | `for each` visits its values. |
| Map | `{bread: 3}` | Read a field with `.bread` or `["bread"]`. |
| None | `none` | Represents an absent value. |

Equality uses `is` and `is not`. The aliases `==` and `!=` also parse. `=` does not assign.

`and` combines conditions. `or` also supplies defaults: `saved("count") or 0`. Only `no` and `none` select that fallback. Zero, empty text and empty lists do not.

1. Compare values explicitly in conditions.
2. Use `is set` before reading an optional nested value.
3. Use bracket access for map keys that contain spaces or punctuation.

## Branches and loops

```beacon
# beacon 1
set total to 0
repeat 3 times
  set total to total + 2
end repeat
if total is 6 then
  show "Expected total"
else
  show "Unexpected total"
end if
while total < 8
  set total to total + 1
end while
assert(total is 8, "loop result")
```

`else if` adds another branch. `stop` exits a loop. `skip` continues with its next iteration. A long loop can exhaust the execution budget.

## Functions

```beacon
# beacon 1
function subtotal(price, count)
  return price * count
end function
set result to subtotal(3, 4)
assert(result is 12, "subtotal")
show result
```

Functions can return any Beacon value. Recursion has a depth limit. Pure helper functions are easier to test than functions that also send chat.

## Tasks and time

`start worker()` starts background work. `tasks()` lists live task records, including their IDs. `await id` waits for completion. `cancel task id` cancels one.

`wait 2 seconds` yields the current task. It does not block the connection loop. The minimum wait is 100 milliseconds.

```beacon
# beacon 1
function later()
  wait 1 second
  show "Task completed"
end function
start later()
set active to tasks()
if len(active) > 0 then
  await active[0].id
end if
```

Use [timer blocks](events-and-game.md#timers) for recurring work. Session changes cancel pending sleeps and one-shot timers. A script reload creates fresh execution state.

## Handle errors

```beacon
# beacon 1
try
  assert(no, "example failure")
catch err
  show "Caught: {err.message}"
finally
  show "Attempt finished"
end try
```

Caught errors expose `message`, `code` and `line`. Wrong argument kinds, missing providers and failed session actions can raise errors.

1. Catch failures near the action that can fail.
2. Include a useful local diagnostic.
3. Put cleanup in `finally` when it must run after either outcome.

Do not repeatedly retry a failed game action without a delay. The action can remain unavailable for the entire session.

## Text and data helpers

| Group | Functions |
| --- | --- |
| Text | `len`, `lower`, `upper`, `trim`, `split`, `join`, `slice`, `replace`, `index_of` |
| Collections | `sort`, `reverse`, `unique`, `keys`, `values`, `has_key` |
| Conversion | `text`, `number`, `json_parse`, `json_stringify` |
| Matching | `match`, `match_all`, `escape_regex` |
| Numbers | `min`, `max`, `clamp`, `round`, `abs`, `log` |
| Randomness | `random`, `pick`, `chance` |
| Tests | `assert(condition, label)` |

`sort` returns a new list of numbers or text. `unique` preserves first-seen order. `index_of` returns a zero-based position or `none`.

Next: [Events and game APIs](events-and-game.md).

## Read an expression step by step

In `price * count + fee`, multiplication runs before addition. Parentheses make the intended grouping visible: `(price * count) + fee`. Prefer explicit parentheses when several comparisons or defaults appear together.

List positions start at zero. A position outside the list does not provide a usable item. Check the list length before selecting a position that can be absent.

A map field can be missing. Check a missing field before treating it as a number or another map. `or` is a default operator as well as a boolean operator. It does not treat every empty value as false.

```beacon
# beacon 1
set record to {name: "Alice"}
set count to record.count or 0
if count is 0 then
  show "No orders yet"
end if
set names to [" Alice ", "BOB"]
for each name in names
  show lower(trim(name))
end for
```

Expected output is `No orders yet`, `alice`, and `bob` on separate lines.

## Convert external text

User input starts as text. A numeric calculation needs a number. Test or catch conversion errors instead of assuming that every input is numeric.

```beacon
# beacon 1
set count to number("4")
assert(count is 4, "number conversion")
set encoded to json_stringify({item: "bread", count: count})
set decoded to json_parse(encoded)
assert(decoded.count is 4, "JSON round trip")
show decoded.item
```

Expected output is `bread`. JSON describes data. It does not execute a Beacon script. A JSON response can still have missing or unexpected fields.

## Understand scope

Each script has its own globals. Function parameters and local assignments belong to that call. An event alias is available inside its handler. Do not read an event alias from unrelated top-level code.

Use names that describe their values, such as `total` or `player_name`. Avoid names that hide built-in tables such as `server`, `inv`, or `world`.

## Keep background work bounded

Use `start` when work must continue separately. Keep the task ID when you need to cancel or await it. Use `every` when the main purpose is recurring scheduling.

A `wait` yields execution. It does not guarantee that the world remains unchanged. Read the relevant container, position, or entity again after a wait.

For complete progression from values to tested functions, read [Chapters 2 and 3](guide/02-values.md).

## Operators

| Operation | Syntax | Example |
| --- | --- | --- |
| Arithmetic | `+`, `-`, `*`, `/`, `%` | `price * count` |
| Equal or unequal | `is`, `is not` | `item is "bread"` |
| Numeric order | `<`, `<=`, `>`, `>=` | `count >= 3` |
| Boolean logic | `and`, `or`, `not` | `ready is yes and count > 0` |
| Optional value | `is set`, `is not set` | `record.name is set` |
| Empty value | `is empty` | `found is empty` |
| Text search | `contains`, `starts with`, `ends with` | `message starts with "!price "` |
| Pattern match | `matches` | `message matches /[0-9]+/` |

Addition accepts two numbers or two text values. It does not automatically combine a number with text. Use interpolation or `text(value)`.

Numeric division by zero fails. `or` defaults only on `no` and `none`. Conditions must produce yes/no values.

## Block placement and endings

Place `import` and `extern` declarations before event, function, timer, and command blocks. Keep those blocks at the top level.

Each block needs its matching ending: `end if`, `end while`, `end repeat`, `end for`, `end function`, `end on`, `end every`, `end in`, `end command`, or `end try`.

Inside a loop, `stop` exits the loop and `skip` moves to the next iteration. `return` ends a function call.

Inside an event handler, `stop event` ends the handler immediately. It can also suppress supported local chat presentation hooks. Later statements in that handler do not run.

## Header metadata

`# beacon 1` selects the source syntax. The standard library has its own version. The current library is version `2`.

| Header | Meaning |
| --- | --- |
| `# needs: chat.send world.read` | Required capability names, separated by spaces. |
| `# wants: shop.calculate` | Optional capability. Missing providers produce warnings. |
| `# setting prefix = "Hello" ; Greeting text` | Editable scalar default with a description. |
| `# desc: Show an order total.` | Description used by script command help. |
| `# example: /order-total` | Example used by script command help. |

Ordinary comments start with `#`. Keep explanations close to the statement they describe.

For the full function list, read [the standard library](standard-library.md).
