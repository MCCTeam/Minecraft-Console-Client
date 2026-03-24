# Anti-Rationalization Guide

Techniques for bulletproofing skills against agent rationalization.

## The Problem

Discipline-enforcing skills face a unique challenge: smart agents under pressure will find loopholes.

## Technique 1: Close Every Loophole Explicitly

Don't just state the rule - forbid specific workarounds.

### Bad Example
```markdown
Write code before test? Delete it.
```

### Good Example
```markdown
Write code before test? Delete it. Start over.

**No exceptions**:
- Don't keep it as "reference"
- Don't "adapt" it while writing tests
- Don't look at it
- Delete means delete
```

## Technique 2: Address "Spirit vs Letter" Arguments

Add foundational principle early:

```markdown
**Violating the letter of the rules is violating the spirit of the rules.**
```

## Technique 3: Build Rationalization Table

| Excuse | Reality |
|--------|---------|
| "Too simple to test" | Simple code breaks. Test takes 30 seconds. |
| "I'll test after" | Tests passing immediately prove nothing. |
| "Spirit not ritual" | The letter IS the spirit. |

## Technique 4: Create Red Flags List

```markdown
## Red Flags - STOP and Start Over
- Code before test
- "I already manually tested it"
- "This is different because..."

**All of these mean**: Delete code. Start over.
```

## Technique 5: Use Strong Language

```markdown
# Weak (invites rationalization)
You should write tests first.

# Strong (no wiggle room)
ALWAYS write test first.
NEVER write code before test.
```

## Technique 6: Provide Escape Hatch for Legitimate Cases

```markdown
## When NOT to Use
- Spike solutions (throwaway exploratory code)
- One-time scripts deleting in 1 hour

**Everything else**: Follow the rule. No exceptions.
```

## Complete Bulletproofing Checklist

- [ ] Forbidden each specific workaround explicitly?
- [ ] Added "spirit vs letter" principle?
- [ ] Built rationalization table from baseline tests?
- [ ] Created red flags list?
- [ ] Used strong language (ALWAYS/NEVER)?
- [ ] Provided explicit escape hatch?
- [ ] Description includes pre-violation symptoms?
