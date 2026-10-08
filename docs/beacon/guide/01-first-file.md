# Chapter 1: Create and run a file

You will create `hello.bcn` and run it without a server. This separates script errors from connection errors.

## Prepare MCC

1. Install MCC using the [installation guide](../../getting-started/installation.md).
2. Create a directory for your own script files.
3. Open a terminal in the MCC application directory.

You need a text editor. A basic editor is enough. Save plain text with UTF-8 encoding. Check that the filename ends in `.bcn`, rather than `.bcn.txt`.

These chapters use `./Mcc.Cli` on Linux and macOS. Use `.\Mcc.Cli.exe` in Windows PowerShell. For a framework-dependent build, use `dotnet /full/path/Mcc.Cli.dll`.

## Create the script

1. Create `hello.bcn` in your script directory.
2. Copy this complete script into the file.

```beacon
# beacon 1
show "Hello from Beacon"
```

3. Replace `/full/path/hello.bcn` with your file's full path.
4. Check the script.

```sh
./Mcc.Cli lint /full/path/hello.bcn
```

5. Run the script.

```sh
./Mcc.Cli run /full/path/hello.bcn
```

The output contains:

```text
Hello from Beacon
```

In Windows PowerShell, use these commands with your actual path:

```powershell
.\Mcc.Cli.exe lint "C:\MccData\scripts\hello.bcn"
.\Mcc.Cli.exe run "C:\MccData\scripts\hello.bcn"
```

The installer may provide an `mcc` launcher. That launcher accepts the same arguments.

MCC also prints a file status. `# beacon 1` selects the supported language version. Every complete script needs this header.

Ordinary comments also start with `#`. `show` writes local output. It does not send a Minecraft message.

## Find the first error

The filename must match the terminal argument. If MCC cannot read it, check the path and spelling.

If lint reports a line and column, inspect that location first. A missing closing quote can also affect the next line.

1. Correct the first error.
2. Run lint again.
3. Run the script after lint succeeds.

## Try a change

1. Replace `Hello from Beacon` with your own greeting.
2. Save the file.
3. Run the file again.
4. Check that the output contains your new text.

This edit-and-check loop will remain useful as your scripts become larger.

Next: [Chapter 2: Work with values](02-values.md).
