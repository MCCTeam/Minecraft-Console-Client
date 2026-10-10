# /lang

Show or change the MCC interface language.

## Syntax

```text
/lang [<tag>|auto]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/lang` | show the UI language, the server locale, and plugin coverage |
| `/lang <tag>` | set the UI language, for example de or pt-BR |
| `/lang auto` | follow the operating system |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/lang
/lang de
/lang auto
```

## Behavior and requirements

The command shows or changes the MCC interface language. It reports the server-visible Minecraft locale separately. A valid language tag can still fall back to English for missing translations.

A change saves `Localization.Language` in `client.toml` and applies supported live consumers. `/plugins settings all regen` rewrites settings comments while preserving values. It does not reset settings.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
