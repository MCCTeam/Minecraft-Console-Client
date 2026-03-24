# CSO Guide - Claude Search Optimization

Advanced techniques for making skills discoverable by agents.

## The Discovery Problem

You have 100+ skills. Agent receives a task. How does it find the RIGHT skill?

**Answer**: The `description` field.

## Critical Rule: Description = Triggers, NOT Workflow

### The Trap

When description summarizes workflow, agents take a shortcut.

**Real example that failed**:

```yaml
# Agent did ONE review instead of TWO
description: Code review between tasks

# Skill body had flowchart showing TWO reviews
```

**Why it failed**: Agent read description, thought "code review between tasks means one review", never read the flowchart.

**Fix**:

```yaml
# Agent now reads full skill and follows flowchart
description: Use when executing implementation plans with independent tasks
```

### The Pattern

```yaml
# BAD: Workflow summary
description: Analyzes git diff, generates commit message in conventional format

# GOOD: Trigger conditions only
description: Use when generating commit messages or reviewing staged changes
```

## Token Efficiency

**Target word counts**:
- Frequently-loaded skills: <200 words total
- Other skills: <500 words

## Keyword Strategy

### Error Messages
Include EXACT error text users will see.

### Symptoms
Use words users naturally say: "flaky", "hangs", "slow", "timeout", "race condition"

### Tools & Commands
Actual names, not descriptions: "pytest", not "Python testing"

### Synonyms
Cover multiple ways to describe same thing: timeout/hang/freeze

## Description Template

```yaml
description: "Use when [SPECIFIC TRIGGER]."
metadata:
  triggers: [error1], [symptom2], [tool3]
```

## Third Person Rule

```yaml
# BAD: First person
description: "I can help you with async tests"

# GOOD: Third person
description: "Handles async tests with race conditions"
```

## Verification Checklist

- [ ] Description starts with "Use when..."?
- [ ] Description is <500 characters?
- [ ] Description lists ONLY triggers, not workflow?
- [ ] Includes 3+ keywords (errors/symptoms/tools)?
- [ ] Third person throughout?
- [ ] Name uses gerund or verb-first format?
