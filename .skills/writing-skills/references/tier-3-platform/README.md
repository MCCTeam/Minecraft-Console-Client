---
description: When to use Tier 3 (Platform) skill architecture for large platforms.
metadata:
  tags: [tier-3, platform, enterprise]
---

# Tier 3: Platform Skills

Enterprise-grade skills for entire platforms (AWS, Cloudflare, Convex, etc).

## When to Use

- **Entire platform**: 10+ products/services
- **1000+ lines total**: Would overwhelm context if monolithic
- **Complex decision logic**: Users start with "I need X" not "I want product Y"

## The 5-File Pattern

Each product directory has exactly 5 files:

| File | Purpose | When to Load |
|------|---------|--------------|
| `README.md` | Overview, when to use | Always first |
| `api.md` | Runtime APIs, methods | Implementing features |
| `configuration.md` | Config, environment | Setting up |
| `patterns.md` | Common workflows | Best practices |
| `gotchas.md` | Pitfalls, limits | Debugging |

## Decision Trees

```markdown
Need to store data?
  Simple key-value -> kv/
  Relational queries -> d1/
  Large files/blobs -> r2/
  Per-user state -> durable-objects/
```

## Progressive Disclosure in Action

- **Startup**: Only name + description (~100 tokens)
- **Activation**: SKILL.md with trees (<5000 tokens)
- **Navigation**: One product's 5 files (as needed)

## Checklist

- [ ] SKILL.md contains ONLY decision trees + index
- [ ] Each product has exactly 5 files
- [ ] Decision trees cover all "I need X" scenarios
- [ ] Cross-references stay one level deep
- [ ] Every product has `gotchas.md`
