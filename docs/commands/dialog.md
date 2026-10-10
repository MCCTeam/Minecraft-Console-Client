# /dialog

Read a server dialog, fill its inputs, and choose a button.

Type the examples in MCC's input prompt. They use the default `/` command prefix.

## Before you start

Connect to a server that supports custom dialogs. Minecraft introduced this protocol feature in 1.21.6. The server must send a dialog before these commands can act on it.

`/dialog` does not create a dialog. The examples below describe possible server dialogs. Your server can use different input keys, choices, button labels, and actions.

Start each interaction with:

```text
/dialog show
```

Read the body, input keys, available choices, and numbered buttons. Use the values from that output in your commands.

## Syntax

```text
/dialog
/dialog show
/dialog open
/dialog set <input> <value>
/dialog input <input> <value>
/dialog click <index>
/dialog click-label <label>
/dialog cancel
/dialog dismiss
```

Replace the angle-bracket placeholders with actual values. Do not type the brackets.

| Form | Meaning |
| --- | --- |
| `/dialog` or `/dialog show` | Print the current dialog and its values. |
| `/dialog open` | Open the current dialog in MCC's TUI. |
| `/dialog set <input> <value>` | Prepare a value for one input. |
| `/dialog input <input> <value>` | An alternative spelling of `set`. |
| `/dialog click <index>` | Press a button by its displayed number, starting at 1. |
| `/dialog click-label <label>` | Press a button with that label. |
| `/dialog cancel` | Cancel if the dialog permits closing or declares an exit action. |
| `/dialog dismiss` | Close through the same cancellation API, without the command's close-permission check. |

## Example: acknowledge a maintenance notice

Suppose the server shows this notice:

- Title: `Server maintenance`.
- Message: `The server will restart in ten minutes.`
- Button 1: `Understood`.

Read the notice, then acknowledge it:

```text
/dialog show
/dialog click-label Understood
```

You can use the button number instead:

```text
/dialog click 1
```

Choose one click form. Pressing a button clears MCC's current dialog state. A later command needs the next dialog, if the server sends one.

## Example: accept the server rules

Suppose the server sends a rules dialog with:

| Element | Server-provided value |
| --- | --- |
| Boolean input key | `rules_read` |
| Button 1 | `Accept rules` |
| Button 2 | `Leave` |

Read the rules before answering. Set the checkbox value, check it, then choose the acceptance button:

```text
/dialog show
/dialog set rules_read true
/dialog show
/dialog click-label Accept rules
```

`set` stores the value in MCC. It does not press a button or send the answer immediately. The second `show` displays your prepared value.

The server's button action determines whether that value is submitted and what happens next. Check MCC's action result and the server's response.

If you do not accept the rules, choose the displayed `Leave` button instead:

```text
/dialog click-label Leave
```

Use this as an alternative to acceptance. The server decides what `Leave` does, which can include disconnecting you.

## Example: join a parkour event

Suppose an event dialog has these inputs:

| Input key | Type | Allowed values in this example |
| --- | --- | --- |
| `display_name` | Text | Your event name |
| `course` | Single option | `meadow` or `canyon` |
| `reminders` | Boolean | `true` or `false` |
| `rounds` | Number range | 1 through 5 |

The submit button is labeled `Join event`.

Prepare your answers:

```text
/dialog show
/dialog set display_name River Runner
/dialog set course meadow
/dialog set reminders false
/dialog set rounds 3
/dialog show
```

This selects the meadow course, disables reminders, and requests three rounds. The event name contains a space.

After checking the displayed values, submit through the button:

```text
/dialog click-label Join event
```

Use the option ID `meadow`, even if its visible label is `Meadow course`. The option ID is the value that `set` accepts.

To change a prepared answer before pressing the button:

```text
/dialog set course canyon
/dialog set rounds 2
/dialog show
```

Run these changes before `Join event`, while the same dialog is still current. Each `set` replaces your previous value for that input.

## Example: send feedback about a building

Suppose a feedback dialog contains the text input `message`, the number input `rating`, and a button labeled `Send feedback`. Its rating range is 1 through 5.

```text
/dialog show
/dialog set message The library is easy to find. Please add a sign for the entrance.
/dialog set rating 4
/dialog show
/dialog click-label Send feedback
```

The message uses the rest of the command line. Do not surround it with quotation marks. Quotes would become part of the value.

If the server instead allows decimal ratings, such as 0 through 5, you can use:

```text
/dialog set rating 4.5
```

Use a decimal point, not a comma. Check the displayed range before using a decimal value.

## Example: confirm a teleport

Suppose the server asks whether you want to teleport to an event arena. It shows:

- Button 1: `Teleport`.
- Button 2: `Stay here`.

Read the destination and any cost in the dialog body. To confirm:

```text
/dialog show
/dialog click 1
```

To decline instead:

```text
/dialog click 2
```

These are alternative responses. Do not send both. The commands press the displayed buttons. The server controls the teleport and any cost.

If two buttons have the same label, use their numbers. `click-label` rejects an ambiguous label.

## Input rules

| Input type | How to enter the value |
| --- | --- |
| Text | Type the text after the input key. Spaces are allowed. |
| Boolean | Use `true` or `false`. Do not use `yes`, `no`, `1`, or `0`. |
| Single option | Use an option ID from `show`, with its exact spelling and case. |
| Number range | Use a number inside the displayed range. Use `.` for decimals. |

Input keys are case-sensitive. Use `rules_read` only if the dialog declares that exact key.

If an input key itself contains spaces, quote the key only:

```text
/dialog set "contact name" Alex Morgan
```

This works only for a dialog that declares the key `contact name`. The value is `Alex Morgan` without quotes.

You can also use the `input` alias:

```text
/dialog input display_name River Runner
```

It has the same behavior as `set`.

Button label matching ignores case but requires the full label. Labels with spaces need no quotes:

```text
/dialog click-label Accept rules
```

## Open the dialog in the TUI

When a server dialog is current, use:

```text
/dialog open
```

The TUI displays its body, inputs, and buttons. Values prepared with `set` seed the input fields when you open the view.

Use the TUI fields and buttons to complete that interaction. Avoid changing the same answers through text commands while the overlay is open.

In the classic console, use `show`, `set`, and the click commands instead. `open` requires the TUI renderer.

Pressing `Escape` in the TUI closes the overlay only. It does not submit an answer or cancel the server dialog. You can inspect it again with `/dialog show`.

## Cancel or dismiss a dialog

For example, if you decide not to finish the event signup:

```text
/dialog cancel
```

`cancel` checks whether the dialog permits closing or declares an exit action. It can refuse cancellation. When available, a server-provided `Cancel` or `Back` button is another choice whose behavior the server defines.

The `dismiss` form skips that command-level permission check:

```text
/dialog dismiss
```

Both commands call the same cancellation API. If the server declares a custom exit action, either command can send it. Neither guarantees a local-only close or an answer-free operation.

Both clear the command's prepared inputs. To close only the TUI overlay, use `Escape` there.

## What a button can do

Read the action description shown beside each button. Its label alone does not define its behavior.

- A custom server action can send the dialog inputs to the server.
- A supported command action sends its command to the server.
- A close-only button clears the dialog without sending an action.
- URL, suggested-command, and clipboard actions report their value. They do not automatically open a browser, fill the prompt, or copy text.
- Some action types are unsupported. MCC reports that the action was not performed.

A button press clears the local dialog even when its action is unsupported. A success message for a displayed URL is not a server submission.

## Common problems

| Problem | What to check |
| --- | --- |
| No dialog is current. | Wait for the server to send one. These commands do not create dialogs. |
| An input key is unknown. | Run `show`. Copy the input key, not its descriptive label. |
| A boolean value is invalid. | Use `true` or `false`. |
| An option value is invalid. | Copy an allowed option ID with its exact case. |
| A number is outside the range. | Use the range printed by `show`. |
| A button label is unknown or ambiguous. | Use the full displayed label, or the displayed button number. |
| `open` reports that the TUI is unavailable. | Use the text commands or restart MCC in TUI mode. |
| The dialog is an unresolved registry reference. | MCC cannot render its body or press its buttons in this build. A displayed reference does not mean the dialog is empty. |

Treat each new dialog as a new interaction. Read its current fields and buttons before answering it.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
