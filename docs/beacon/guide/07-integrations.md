# Chapter 7: Add commands and integrations

A script command is a command in the client's dispatcher. It is not a new command on the Minecraft server.

## Add an argument

1. Create `scripts/price.bcn`.
2. Copy this complete script into the file.


```beacon
# beacon 1
# desc: Calculate the current bread order.
# example: /guide-price bread
command "/guide-price <item>"
  set item to lower(arg("item"))
  if item is "bread" then
    show "Bread costs 3 coins"
  else
    show "Unknown item: {item}"
  end if
end command
```

`<item>` accepts one string value. Use quotation marks when that value contains spaces. `arg("item")` reads that word. The leading slash belongs in the declaration. The host's dispatcher decides how user input reaches it.

Load the file with `/scripts run price`. Type `/guide-price bread` in MCC. The command registration disappears when you stop the script.

## Export a function

```beacon
# beacon 1
export function subtotal(price, count)
  return price * count
end function
```

Save this provider as `scripts/shop.bcn`. Load it with `/scripts run shop`.

Save the following caller as `scripts/shop-caller.bcn`:

```beacon
# beacon 1
set total to call "shop.subtotal"(3, 4)
assert(total is 12, "provider result")
show "Provider total: {total}"
```

Run the caller with `/scripts run shop-caller`. Check for `Provider total: 12`.

Both scripts must run in the same MCC client. The provider needs to be loaded first. An import copies definitions into the caller's program. An export remains owned by the running provider.

## Call an optional plugin

```beacon
# beacon 1
# wants: shop.calculate
extern subtotal from "shop-tools"
try
  show subtotal(3, 4)
catch err
  show "Pricing unavailable: {err.message}"
end try
```

This example expects a plugin named `shop-tools` to register `subtotal` with capability `shop.calculate`. The declaration does not install that plugin.

`# wants:` allows loading when the capability is absent. The actual call can still fail. `catch` provides a useful result for that case.

Use `# needs:` when the script cannot work without the provider. See [plugin Beacon integration](../../plugins/development/beacon-integration.md) for the provider implementation.

## Handle a failed action

```beacon
# beacon 1
try
  assert(no, "demonstration")
catch err
  show "Failure code: {err.code}"
finally
  show "Attempt finished"
end try
```

The failure record includes `code`, `message`, and `line`. `finally` runs after success or failure. Do not catch an error only to repeat the same failing call immediately.

Next: [Chapter 8: Test and diagnose](08-testing.md).
