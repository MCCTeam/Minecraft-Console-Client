# Testing Guide - TDD for Skills

Complete methodology for testing skills using RED-GREEN-REFACTOR cycle.

## Testing All Skill Types

### Discipline-Enforcing Skills (rules/requirements)

**Test with**:
- Academic questions: Do they understand the rules?
- Pressure scenarios: Do they comply under stress?
- Multiple pressures combined: time + sunk cost + exhaustion

**Success criteria**: Agent follows rule under maximum pressure

### Technique Skills (how-to guides)

**Test with**:
- Application scenarios: Can they apply the technique correctly?
- Variation scenarios: Do they handle edge cases?
- Missing information tests: Do instructions have gaps?

**Success criteria**: Agent successfully applies technique to new scenario

### Pattern Skills (mental models)

**Test with**:
- Recognition scenarios: Do they recognize when pattern applies?
- Counter-examples: Do they know when NOT to apply?

**Success criteria**: Agent correctly identifies when/how to apply pattern

### Reference Skills (documentation/APIs)

**Test with**:
- Retrieval scenarios: Can they find the right information?
- Gap testing: Are common use cases covered?

**Success criteria**: Agent finds and correctly applies reference information

## Pressure Types for Testing

| Pressure | Example |
|----------|---------|
| Time | "You have 5 minutes to complete this task" |
| Sunk cost | "You already spent 2 hours on this" |
| Authority | "Senior developer said to skip tests" |
| Exhaustion | "This is the 10th task today" |

## Complete Test Checklist

**Baseline (RED)**:
- [ ] Designed 3+ pressure scenarios
- [ ] Ran scenarios WITHOUT skill
- [ ] Documented verbatim agent responses

**Implementation (GREEN)**:
- [ ] Skill addresses SPECIFIC baseline failures
- [ ] Re-ran scenarios WITH skill
- [ ] Agent complied in all scenarios

**Bulletproofing (REFACTOR)**:
- [ ] Tested with combined pressures
- [ ] Found and documented new rationalizations
- [ ] Added explicit counters
- [ ] Re-tested until no more loopholes
