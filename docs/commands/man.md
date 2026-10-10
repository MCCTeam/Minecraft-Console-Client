# /man

Read the MCC manual: guides, not syntax.

## Syntax

```text
/man [topic|ui]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/man` | List every manual topic |
| `/man <topic>` | Read one topic |
| `/man -k <word>` | Search every page for a word |
| `/man ui` | open the searchable command and manual browser |

## Flags

| Flag | Meaning |
| --- | --- |
| `-k` | Search page text instead of opening a topic |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/man getting-started
/man movement
/man -k pathfinding
/man ui
```

## Behavior and requirements

`/man` lists available manual topics. `/man -k <word>` searches topic contents. Plugin manuals can appear in the same catalogue.

Use `/help` for exact syntax. Use `/man` for concepts and workflows. `/man ui` opens the Manual tab in the TUI browser.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
