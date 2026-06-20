---
description: Standards and naming rules for creating agent skills.
metadata:
  tags: [standards, naming, yaml, structure]
---

# Skill Development Guide

## Directory Structure

```
skills/
  {skill-name}/           # kebab-case, matches `name` field
    SKILL.md              # Required: main skill definition
    references/           # Optional: supporting documentation
      README.md           # Sub-topic entry point
      *.md                # Additional files
```

## Naming Rules

| Element | Rule | Example |
|---------|------|---------|
| Directory | kebab-case, 1-64 chars | `react-best-practices` |
| `SKILL.md` | ALL CAPS, exact filename | `SKILL.md` (not `skill.md`) |
| `name` field | Must match directory name | `name: react-best-practices` |

## SKILL.md Structure

```markdown
---
name: {skill-name}
description: >-
  Use when [trigger condition].
metadata:
  category: technique
  triggers: keyword1, keyword2, error-text
---

# Skill Title

Brief description of what this skill does.

## When to Use
- Symptom or situation A
- Symptom or situation B

## How It Works
Step-by-step instructions or reference content.

## Examples
Concrete usage examples.

## Common Mistakes
What to avoid and why.
```

## Description Best Practices

```yaml
# BAD: Workflow summary
description: Analyzes code, finds bugs, suggests fixes

# GOOD: Trigger conditions only
description: Use when debugging errors or reviewing code quality.
```

**Rules:**
- Start with "Use when..."
- Keep under 500 characters
- Use third person

## Context Efficiency

| Guideline | Reason |
|-----------|--------|
| Keep SKILL.md < 500 lines | Reduces context consumption |
| Put details in supporting files | Agent reads only what's needed |
| Use tables for reference data | More compact than prose |

## Verification Checklist

- [ ] `name` matches directory name?
- [ ] `SKILL.md` is ALL CAPS?
- [ ] Description starts with "Use when..."?
- [ ] Under 500 lines?
- [ ] Tested with real scenarios?
