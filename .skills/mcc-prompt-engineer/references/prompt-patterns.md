# Prompt Engineering Patterns for MCC Tasks
# Reference file — load when selecting structural patterns for the generated prompt

---

## Core Principles (Anthropic / 2025–2026 Best Practices)

### 1. Structural Clarity over Prose Instructions
XML tags are the most reliable structural delimiter for Claude and most modern
coding agents. Use `<role>`, `<context>`, `<reasoning_protocol>`,
`<design_goals>`, `<scope_constraint>`, and `<output_format>` consistently.
Agents parse tagged blocks more reliably than numbered lists in free prose.

### 2. Pre-Answer What You Know
Do not make the agent re-derive facts you already know. If codebase exploration
has identified the exact failing file and line, put it in `<context>`. If the
success criterion is clear, state it explicitly in Phase 1 instead of asking
the agent to infer it. Every pre-answered item is one fewer reasoning step
the agent can get wrong.

### 3. Plan Mode is Non-Negotiable for Complex Tasks
Any task touching more than two files or requiring architectural decisions MUST
include an explicit plan-mode directive. Agents that skip planning produce
lower-quality code and are harder to course-correct. The directive must appear
before Phase 0 so it gates the entire session.

### 4. Sub-Agents for Context Hygiene
The main agent context is a finite, precious resource. Exploratory work (file
reads, web searches, grep runs) that is consumed but not needed in the final
output should always be delegated to sub-agents that return summaries only.
Keyword: "Return a concise written summary. Do NOT dump raw output into the
main context."

### 5. Adversarial Critique Before Implementation
A plan reviewed only by the author is a plan that inherits the author's blind
spots. Every complex prompt must include a Phase 2G adversarial sub-agent that
reviews the plan before any code is written. This is the single highest-ROI
addition to any agentic prompt.

### 6. Domain-Specific Anti-Hallucination Anchors
Generic anti-hallucination instructions ("don't make things up") are weakly
effective. Effective anchors name the exact high-risk domains:
- OAuth endpoint URLs (fabrication-prone)
- MSAL / Microsoft auth API signatures (version-sensitive)
- Minecraft protocol packet IDs and field layouts (specialised, sparse training data)
- MCC internal class/method names (not in general training data)

### 7. Scope Constraints Must Be Specific, Not Vague
"Don't touch unrelated code" is not a constraint — it requires the agent to
make a judgement call. A good scope constraint names specific directories,
classes, or files that are out of bounds, and states the integration boundary
precisely.

### 8. Output Format as a Delivery Contract
The `<output_format>` block is a contract, not a suggestion. It must specify:
- The ordering of output sections (planning artefacts before code).
- File naming conventions.
- Code block format (fenced, with filename on the opening fence line).
- Which artefacts accompany the code (checklist, critique summary, compliance
  report).

---

## Pattern Library

### Pattern A — Bug Fix with Root Cause Isolation

Best for: authentication failures, network errors, unexpected exceptions.

Key additions to the reasoning protocol:
- Phase 1.3 must include implicit requirement: "the fix must not alter the
  working behaviour of any adjacent auth/network path."
- Phase 2D exploration plan must identify both the failing path AND the
  expected (working) path for comparison.
- Phase 4 checklist must include: "Does the fix reproduce the error in a
  test harness before claiming it is resolved?"

### Pattern B — Refactor + New Module Introduction

Best for: extracting monolithic logic into a dedicated, testable module.

Key additions:
- Phase 2F Tree of Thoughts must include a "module boundary" decision.
- Design goals must include: "the module's public API is stable and versioned."
- Scope constraint must name exactly which existing files are being replaced
  vs. which are being delegated to (the integration seam).
- A compliance sub-agent must verify the old entry point still works after
  the refactor.

### Pattern C — Protocol / Network Implementation

Best for: Minecraft packet handling, connection management, session state.

Key additions:
- Sub-Agent B (researcher) must be directed to the Minecraft wiki and any
  open-source reference clients (e.g., wiki.vg, Prismarine).
- Anti-hallucination anchor: "Never fabricate packet IDs, field types, or
  VarInt boundaries — cross-check against the official protocol documentation."
- Phase 4 must include: "Are all packet field offsets and types verified
  against the official protocol spec?"

### Pattern D — C# Language Modernisation

Best for: C# 14 features, record types, primary constructors, pattern matching.

Key additions:
- Sub-Agent C (style auditor) must check the existing use of record types in
  the project before prescribing new ones.
- Design goals must specify which C# 14 features are required vs. optional.
- Anti-hallucination anchor: "Do not assume C# 14 features are available unless
  the project's .csproj has been confirmed to target .NET 10 or a compatible
  SDK."
- Phase 4 must include: "Does the code compile cleanly against the target
  .NET version? Are there any C# 14 features used that require a language
  version pragma?"

### Pattern E — Bot Scripting / Extension

Best for: new bot actions, scripting API extensions, event hooks.

Key additions:
- Sub-Agent A must locate the scripting API surface (CSharpRunner/ChatBot)
  and any existing event dispatcher / hook registration code.
- Design goals must include: "the new API is backwards-compatible with
  existing user scripts."
- Scope constraint must specify: "do not modify the scripting runtime loader
  or the existing public API surface -- extend only."

### Pattern F -- Context Engineering / JIT Context Loading

Best for: tasks where the agent needs broad codebase awareness without context
overload, or tasks that span multiple subsystems.

Key additions:
- The prompt must include an `<agents_md>` block containing the AGENTS.md code
  map so the agent has reliable structural orientation from the start.
- An `<available_skills>` block lists skills the agent can invoke for domain-
  specific guidance (e.g., `mcc-chatbot-authoring`, `mcc-version-adaptation`).
- Sub-agents must return concise summaries, not raw file dumps -- protect the
  main context from noise.
- Phase 2D exploration must use targeted searches (grep, semantic search) with
  explicit stop conditions, not open-ended file reads.
- Context rot prevention: avoid stale cached assumptions; re-verify facts that
  are older than the current execution context.
- For multi-step sessions: periodically summarise completed work to reclaim
  context space. Emit incremental progress rather than accumulating full
  history.

---

## Prompt Length Calibration

| Task complexity | Recommended prompt size |
|---|---|
| Single-file bug fix | ~40–80 lines — short role, context, 3-phase reasoning, clear output |
| Module refactor | ~120–200 lines — full ULTRATHINK, 4 sub-agents, ToT decisions |
| New protocol feature | ~150–250 lines — full ULTRATHINK, external research mandate, wiki anchors |
| Architecture overhaul | ~200–300 lines — full ULTRATHINK, 5+ sub-agents, compliance verifier |

Longer is not better. Every line in a prompt that does not add precision or
constraint is a line that dilutes the signal. Trim ruthlessly after drafting.

---

## Checklist: Signs of a Weak Prompt

- The role block is generic ("expert software engineer") rather than domain-specific.
- `<context>` omits the exact error message or failing state.
- Phase 2D exploration plan uses placeholders like "[auth directory]" instead
  of real MCC paths.
- Sub-agents have open-ended missions ("research everything about X").
- No adversarial critique phase.
- Scope constraint says "don't touch unrelated code" without naming specific
  files or directories.
- `<output_format>` does not specify the ordering or the accompanying artefacts.
- Plan mode directive is absent or appears after Phase 0.
