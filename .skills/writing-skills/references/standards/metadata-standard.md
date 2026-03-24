# SKILL.md Metadata Standard

Official frontmatter fields.

## Required Fields

```yaml
---
name: skill-name
description: >-
  Use when [trigger condition].
---
```

| Field | Rules |
|-------|-------|
| `name` | 1-64 chars, lowercase, hyphens only, must match directory name |
| `description` | 1-1024 chars, should describe when to use |

## Optional Fields

```yaml
---
name: skill-name
description: Purpose and triggers.
metadata:
  category: "reference"
  version: "1.0.0"
---
```

## Name Validation

```regex
^[a-z0-9]+(-[a-z0-9]+)*$
```

**Valid**: `my-skill`, `git-release`, `tdd`
**Invalid**: `My-Skill`, `my_skill`, `-my-skill`
