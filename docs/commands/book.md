# /book

Read, write, edit, and sign the book held in your main hand.

Type these commands in MCC's input prompt. The examples use the default `/` command prefix.

## Before you start

Connect to a server. Enable `[Gameplay] Inventory` in `client.toml`. See [configuration](../client/configuration.md#gameplay).

For reading, hold a Book and Quill or a signed Written Book. For writing, editing, and signing, hold an unsigned Book and Quill. A plain Book item does not work.

To select the first hotbar position:

```text
/changeslot 1
/book
```

Put your book in that position first. [`/changeslot`](changeslot.md) selects the position but does not move an item into it. `/book` describes the held book and its page count.

Keep the same book selected throughout an editing example. Run one command at a time. Wait for each server inventory update before the next change. Use `read` to check the updated pages.

## Syntax

```text
/book
/book read [page]
/book write <text>
/book write text <text>
/book write file <path>
/book edit
/book edit page <n> <text>
/book edit append [text]
/book edit insert <n> [text]
/book edit delete <n>
/book sign <title>
```

Replace the values in angle brackets. Square brackets mark optional arguments. Do not type the brackets.

| Form | Meaning |
| --- | --- |
| `/book` | Describe the book in your main hand. |
| `/book read [page]` | Read all pages or one numbered page. |
| `/book write <text>` | Replace the entire book's contents. |
| `/book write text <text>` | Replace the contents using explicit text mode. |
| `/book write file <path>` | Replace the contents from a local UTF-8 text file. |
| `/book edit` | Open the writable book in MCC's TUI editor. |
| `/book edit page <n> <text>` | Replace one existing page. |
| `/book edit append [text]` | Add a page at the end. Omit text for a blank page. |
| `/book edit insert <n> [text]` | Insert a page at a numbered position. Omit text for a blank page. |
| `/book edit delete <n>` | Remove one existing page. |
| `/book sign <title>` | Sign the current contents with the given title. |

Page numbers start at `1`. Text and titles can contain spaces without quotation marks. Surrounding quotes become part of the text or title.

## Read a book

To read all pages of the held book:

```text
/book read
```

In the classic console, MCC prints the pages. In TUI mode, this command opens the read-only book view.

To read individual pages as text in either mode:

```text
/book read 1
/book read 2
```

The second command requires a book with at least two pages. Each result identifies the page number and total page count.

The same commands read a signed book. Signed books also show their title and author.

## Write and sign a one-page book

Hold an unsigned Book and Quill. These commands create a short diary:

```text
/book write Today I built a small house near the river.
/book read 1
```

`write` replaces every existing page with this one page. It does not sign the book. Read the page before you continue.

To correct the page:

```text
/book edit page 1 Today I built a stone house near the river.
/book read 1
```

When the page is correct, sign it:

```text
/book sign My First Diary
/book read 1
```

The signed book has the title `My First Diary` and keeps the page you edited.

::: warning Signing is permanent
Check all pages and the title before you sign. Signing turns the Book and Quill into a Written Book. You cannot edit or rename that signed book with `/book`.
:::

## Write multiple pages

In a `write` command, type `\f` between pages. Type `\n` for a new line within a page. Enter the following as one command line:

```text
/book write Welcome to River Town.\nPlease enjoy your visit.\fTown rules:\nRespect other players.\fUseful places:\nThe market is near the bridge.
```

This creates three pages:

| Page | Contents |
| --- | --- |
| 1 | Welcome to River Town. A new line follows the first sentence. |
| 2 | Town rules, followed by a new line and the rule. |
| 3 | Useful places, followed by a new line and the market location. |

Check each page:

```text
/book read 1
/book read 2
/book read 3
```

The characters `\f` and `\n` are typed escapes. MCC converts them to page breaks and line breaks. Pressing Enter submits the command.

Use `\f` with `write` to create separate pages. The `edit page`, `append`, and `insert` commands each modify one page. Use `\n` for line breaks in those commands.

## Edit several pages and sign the book

This example continues the three-page River Town book above. Keep it unsigned until you complete all changes.

1. Replace page 1:

   ```text
   /book edit page 1 Welcome to River Town.\nAsk Alex if you need help.
   /book read 1
   ```

2. Replace page 2:

   ```text
   /book edit page 2 Town rules:\nRespect other players.\nAsk before changing their builds.
   /book read 2
   ```

3. Replace page 3:

   ```text
   /book edit page 3 Useful places:\nMarket: near the bridge.\nLibrary: beside the town square.
   /book read 3
   ```

4. Check that all three pages contain the intended text.
5. Sign the book:

   ```text
   /book sign River Town Guide
   ```

6. Read the signed book:

   ```text
   /book read
   ```

Each `edit page` command preserves the other pages. `sign` uses the book's current pages. It does not add or replace page text.

## Add pages to an existing book

To add text after the last page of an unsigned book:

```text
/book edit append Travel notes:\nFollow the road east to reach the station.
/book
```

`/book` shows the new page count. Read that page by its number.

You can also add a blank page:

```text
/book edit append
```

If the book then has four pages, fill the blank fourth page with:

```text
/book edit page 4 More notes will go here.
/book read 4
```

Replace `4` with your actual page number. `edit page` requires an existing page. Use `append` or `insert` to create a new page first.

## Insert and delete pages

Hold a separate unsigned book for this example, or accept that `write` will replace your current contents.

Create a two-page draft:

```text
/book write Introduction: our village.\fDraft ending: add travel directions here.
```

Insert a new page before page 2:

```text
/book edit insert 2 Travel directions: follow the river north.
/book read 2
/book read 3
```

The inserted text is now page 2. The old draft ending moves from page 2 to page 3.

Remove the draft ending:

```text
/book edit delete 3
/book
```

The book now has two pages: the introduction and the travel directions.

To insert a blank page before the first page:

```text
/book edit insert 1
```

Later pages move forward by one position. You can insert at an existing page position or immediately after the last page.

Deleting a page moves later pages back by one position. If you delete the only page, MCC keeps one blank page.

## Write a book from a text file

Create a UTF-8 text file on the computer that runs MCC. For example, save this text as `town-guide.txt`:

```text
Welcome to River Town.\nAsk Alex if you need help.\fTown rules:\nRespect other players.\fUseful places:\nThe market is near the bridge.
```

The literal `\f` separators create three pages. You can use actual line breaks in the file or typed `\n` escapes within a page.

Hold an unsigned Book and Quill. Use the absolute path to your file. For Linux, macOS, or MCC running inside WSL:

```text
/book write file /home/user/mcc-books/town-guide.txt
```

For MCC running natively on Windows:

```text
/book write file C:/Users/Alex/Documents/MCC Books/town-guide.txt
```

Replace the example path with your file's path. The path uses the rest of the command line, including spaces. Do not surround it with quotation marks.

Relative paths use MCC's working directory. An absolute path avoids confusion when a launcher uses a different directory.

After the server updates the book, read all three pages. Sign it only after you check the contents:

```text
/book read 1
/book read 2
/book read 3
/book sign River Town Guide
```

## Write text that starts with a command keyword

Use explicit text mode when the first word of your page is `file` or `text`:

```text
/book write text file storage is beside the library.
/book write text text for the notice board goes here.
```

These are independent examples. Each command replaces the whole book with one page. The first page starts with `file`. The second starts with `text`.

## Use the TUI editor

In TUI mode, hold an unsigned Book and Quill:

```text
/book edit
```

Edit the page text in the overlay. `PageUp` and `PageDown` change pages. At the last page, `PageDown` adds a blank page if the page limit permits it.

Use `Ctrl+S` to save without signing. To sign, enter a title in the title field and use `Ctrl+G`. An empty title saves without signing.

`Escape` closes the overlay. If you have unsaved changes, the first press requests confirmation. A second press discards the changes.

The classic console does not support `/book edit` without additional arguments. Use `edit page`, `append`, `insert`, and `delete` there.

## Common problems

| Problem | What to check |
| --- | --- |
| MCC says you are not holding a book. | Select the hotbar position containing a Book and Quill or Written Book. A plain Book does not work. |
| MCC says the book cannot be edited. | Use an unsigned Book and Quill. Signed books are read-only. |
| A page does not exist. | Use `/book` to check the page count. Use `append` or `insert` before editing a new page. |
| The file is not found. | Check the path on MCC's computer. Remove surrounding quotes. Use an absolute path. |
| A page or title is too long. | Shorten it according to the limit in MCC's error message. Limits depend on the Minecraft protocol. |
| An update cannot be sent. | Check the server version. The current book-edit path supports Minecraft 1.17 and later. Earlier versions can have readable books without supporting this edit path. |
| A change appears to lose the previous edit. | Wait for the server inventory update between changes. Read the latest page before editing again. |

Writing, editing, and signing send requests to the server. Check the updated book before you continue to the next operation.

[All commands](index.md) · [Inventory](inventory.md) · [Troubleshooting](../troubleshooting/index.md)
