---
description: When to use Tier 1 (Simple) skill architecture.
metadata:
  tags: [tier-1, simple, single-file]
---

# Tier 1: Simple Skills

Single-file skills for focused, specific purposes.

## When to Use

- **Single concept**: One technique, one pattern, one reference
- **Under 200 lines**: Can fit comfortably in one file
- **No complex decision logic**: User knows exactly what they need
- **Frequently loaded**: Needs minimal token footprint

## Structure

```
my-skill/
  SKILL.md          # Everything in one file
```

## Checklist

- [ ] Fits in <200 lines
- [ ] Single focused purpose
- [ ] No need for `references/` directory
- [ ] Description uses "Use when..." pattern
