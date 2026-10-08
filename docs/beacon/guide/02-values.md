# Chapter 2: Work with values

You will calculate an order total. A variable is a name for a value. `set total to 0` creates or updates that value.

## Calculate a total

1. Create `values.bcn`.
2. Copy this complete script into the file.

```beacon
# beacon 1
set customer to "Alice"
set prices to [3, 5, 7]
set total to 0
for each price in prices
  set total to total + price
end for
if total >= 10 then
  show "{customer}: large order"
else
  show "{customer}: small order"
end if
assert(total is 15, "order total")
show "Total: {total}"
```

3. Run `./Mcc.Cli run /full/path/values.bcn` with MCC from Chapter 1.

Expected output:

```text
Alice: large order
Total: 15
```

`prices` is a list. `for each` visits each value in order. The body adds that value to `total`. `end for` closes the loop.

Text interpolation replaces `{total}` with the value of `total`. Double braces, `{{` and `}}`, produce literal braces.

A condition chooses a branch. `>=` means greater than or equal to. Conditions need a yes/no value. A number alone is not a condition.

## Use a map

A map stores values under keys. Keys let you describe related information without relying on list positions.

```beacon
# beacon 1
set order to {item: "bread", count: 4, unit_price: 3}
set total to order.count * order.unit_price
show "{order.item}: {total} coins"
assert(order["item"] is "bread", "map key")
```

Expected output is `bread: 12 coins`. Dot access works for simple keys. Bracket access also supports keys with spaces or punctuation.

## Avoid common mistakes

| Intended action | Correct form | Reason |
| --- | --- | --- |
| Assign a value | `set count to 4` | Assignment uses `set` and `to` |
| Compare values | `count is 4` | `is` tests equality |
| Check absence | `value is set` | `none` represents a missing value |
| Supply a default | `saved("count") or 0` | Only `no` and `none` select the default |
| Read the first list value | `prices[0]` | List positions start at zero |

Zero and empty text do not select the right side of `or`. This matters when zero is a valid saved count.

Next: [Chapter 3: Reuse a calculation](03-functions.md).
