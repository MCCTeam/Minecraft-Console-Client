# Chapter 3: Reuse a calculation

A function gives a calculation a name. Parameters carry input values into the function. `return` sends a result to the caller.

## Write and test a function

1. Create `functions.bcn`.
2. Copy this complete script into the file.

```beacon
# beacon 1
function subtotal(price, count)
  return price * count
end function
set result to subtotal(3, 4)
assert(result is 12, "subtotal")
assert(subtotal(3, 0) is 0, "empty order")
show "Subtotal: {result}"
```

3. Execute `./Mcc.Cli run /full/path/functions.bcn`.

Expected output is `Subtotal: 12`. A failed assertion makes the run fail. The label identifies the check that failed.

The function contains no chat or game action. You can test it without connection state. Keep calculations separate from actions when possible.

## Move the function to a library

1. Create a directory named `lib` beside your scripts.
2. Create `lib/math.bcn` with the function from [the sample library](../../examples/beacon/lib/math.bcn).
3. Create `order.bcn` with this complete script.

```beacon
# beacon 1
import "lib/math.bcn" as math
set total to math.subtotal(3, 4)
assert(total is 12, "imported subtotal")
show "Subtotal: {total}"
```

4. Execute `./Mcc.Cli run /full/path/order.bcn`.

Expected output remains `Subtotal: 12`. The alias `math` identifies the imported library. The path resolves from `order.bcn`, not from an arbitrary process directory.

An import reuses definitions inside one script. An export lets a separately running script provide a function. These are different arrangements. Chapter 7 explains exports.

Do not place the same event handler in a library and expect imports to register it. Imported event and command blocks do not become registrations.

Next: [Chapter 4: Answer an event](04-events.md).
