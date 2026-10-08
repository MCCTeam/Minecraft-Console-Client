# Write documentation

Write for a reader who can use Minecraft but has never used a terminal or written code. Define a new term before you use it repeatedly.

## Page structure

Give each page one clear purpose. Explain what the reader will create or change. State the prerequisites before the first command.

Use numbered steps for a procedure. Use one action per sentence. Keep instruction sentences short and use familiar words.

After a code block, explain the expected result. Include a way to check that result. Explain common mistakes close to the step that can cause them.

Examples must match the current CLI, DMCBK version and plugin source. A plausible example is not enough.

## Source ownership

The Beacon and plugin authoring sections adapt DMCBK's documentation. The official plugin pages describe source, manifests, settings and manuals from DMCBK-Plugins.

Keep the source attribution in those indexes. When an API changes, update the source documentation and its MCC adaptation together.

## Check your changes

Install the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [Python](https://www.python.org/downloads/) 3.10 or later, and [Node.js with npm](https://nodejs.org/en/download) before these checks.

```bash
npm --prefix docs ci
python3 tools/check_docs.py
npm --prefix docs run docs:build
```

Validate Beacon files with `Mcc.Cli lint file.bcn`. Run offline examples with `Mcc.Cli run file.bcn`. On PowerShell, use `.\Mcc.Cli.exe`.

Compile plugin examples against the documented NuGet versions. Check exported contracts, settings defaults and the command registration lifecycle.

Place downloadable examples under `docs/examples/`. The site preparation tool copies non-Markdown files into the public examples directory.

## Writing and translation

Use Humanizer to remove filler, invented importance and repetitive prose. Use ASD-STE100 principles for procedures and clear technical explanations.

These principles do not claim certified aerospace compliance. The project uses them to reduce ambiguity for beginners and readers who use English as a second language.

Keep each paragraph on one physical line. Do not translate code, identifiers or filenames. Preserve placeholders in translatable messages.
