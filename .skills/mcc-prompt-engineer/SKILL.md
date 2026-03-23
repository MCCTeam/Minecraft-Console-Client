---
name: mcc-prompt-engineer
description: >
  Manually triggered skill for the Minecraft Console Client (MCC) project
  (https://github.com/MCCTeam/Minecraft-Console-Client). Invoke this skill
  when the user wants to create, design, or generate a high-quality prompt for
  addressing any MCC-related development request -- bug fixes, new features,
  refactors, protocol work, authentication, bot scripting, or architecture
  decisions. The skill interviews the user, explores the MCC codebase via
  sub-agents, identifies relevant project skills, and synthesises everything
  into a state-of-the-art, self-contained prompt that includes an embedded
  reasoning framework, plan-mode directives, skill references, and targeted
  sub-agent instructions. Do NOT trigger automatically; wait for the user to
  explicitly invoke it (e.g. "generate a prompt for...", "build me a prompt",
  "/mcc-prompt-engineer", or "use the MCC prompt skill").
compatibility: "Claude Code, Cursor, Codex, GitHub Copilot, Windsurf, and any AI coding agent. Optional tools: AskUserQuestion, Task, WebSearch, plan."
---

# MCC Prompt Engineer

Generates state-of-the-art prompts for Minecraft Console Client development
tasks. Combines live codebase knowledge (via sub-agents and AGENTS.md),
structured prompt engineering patterns, an embedded ULTRATHINK reasoning
framework, and the MCC project's skill ecosystem so the produced prompt is
immediately ready to use in any AI coding agent.

---

## Reference files -- load on demand

| File | Load when |
|---|---|
| `references/reasoning-framework.md` | Embedding the ULTRATHINK protocol into the generated prompt |
| `references/prompt-patterns.md` | Selecting the right structural patterns for the prompt |

Additionally, read `AGENTS.md` at the repository root early in the process.
It contains the authoritative codebase map -- module responsibilities, key
file paths, architecture overview, version support table, and engineering
DO/DON'T guidance -- and replaces the need for broad exploratory file reads.

---

## Step 0 -- Environment Detection

Determine which tools are available before doing anything else. This gates how
you ask questions and spawn sub-agents.

```
Claude Code    -> AskUserQuestion and Task tools; plan mode via "plan" tool
                  or /plan command.
Cursor / Codex -> No AskUserQuestion; ask clarifying questions inline as a
                  numbered list; sub-agents via parallel tool calls where
                  supported, otherwise inline.
GitHub Copilot -> Similar to Cursor; use runSubagent where available.
Other agents   -> Fall back to inline questions and sequential exploration.
```

Record your environment determination internally before continuing.

---

## Step 1 -- Parse the Request

Extract everything the user has stated. Do not invent requirements or make
assumptions yet. Capture:

- **Domain area:** authentication, bot scripting, protocol handling, network,
  performance, refactor, new feature, bug fix, version adaptation, or other.
- **Stated goal:** what the user wants to achieve.
- **Known constraints:** language version (C# 14 / .NET 10), compatibility
  requirements, scope limits (additive-only, etc.).
- **References provided:** URLs, file paths, issue numbers, error messages.
- **Ambiguity level:** High (proceed) / Medium (note gaps) / Low (clarify
  before continuing).

---

## Step 2 -- Clarification Interview

**Goal:** Resolve all blocking ambiguities before spending time on codebase
exploration. Unblocking questions first saves sub-agent round-trips.

### If in Claude Code
Use the `AskUserQuestion` tool. Ask all questions in a single call -- do not
drip-feed questions turn by turn.

### In any other environment
Print a numbered list of questions. Wait for answers before proceeding.

### Question selection guide

Ask only what is genuinely blocking:

| Ambiguity | Blocking? | Example question |
|---|---|---|
| Scope of change (additive vs rewrite) | Yes | "Should this be additive, or can it replace existing code?" |
| Target .NET / C# version | Yes if non-obvious | "Which .NET version -- 8, 10, or latest?" |
| Auth flow variant | Yes for auth tasks | "Device-code flow, interactive browser, or both?" |
| Performance constraints | Usually no | Skip unless the user mentioned perf |
| Test coverage expectation | Sometimes | "Do you want unit tests, or integration guidance only?" |

**Always ask:**
1. "Is there a specific file, class, or method you already know is the right
   starting point?"
2. "Are there any hard constraints -- things the solution must NOT do or touch?"

Offer a best-guess assumption alongside each question so the user can confirm
or correct rather than answer from scratch.

---

## Step 3 -- Codebase Exploration

Start by reading `AGENTS.md` at the repository root. It provides the
authoritative module map, architecture overview, version support table, and
engineering DO/DON'T guidance. Use it to:

- Identify which modules and files are relevant to the user's domain
- Understand the project's conventions and constraints
- Pre-populate sub-agent exploration plans with concrete file paths

Then dispatch the following sub-agents **simultaneously**. Each must return a
concise written summary only -- raw file contents and grep output waste context
and degrade reasoning quality downstream (context rot).

### SUB-AGENT A -- Domain Explorer (read-only)

**Mission:** Locate and map every file, class, and method directly relevant
to the user's domain area. Scope your search using the module map from
AGENTS.md rather than exploring the entire repository.

**Scoped exploration plan (fill in before dispatching):**
```
Files / directories to read:
  [derived from AGENTS.md module map for this domain -- fill in concrete paths]

Searches to run:
  grep for: [key identifiers from the user's request]

Output:
  - File paths and relevant class/method names
  - The exact lines most relevant to the user's goal
  - Existing abstractions or interfaces that should be extended
  - Patterns and conventions in use

Stop condition: the full call-chain for the relevant feature is mapped.
```

### SUB-AGENT B -- Dependency & Integration Scout (read-only)

**Mission:** Identify everything that calls into or depends on the domain area
found by Sub-Agent A, so the generated prompt can correctly scope the
integration seam.

**Output:**
- All call sites that need updating or wiring
- Public interfaces or contracts that must be preserved
- Any existing test files covering this area
- NuGet packages or external dependencies in use

**Stop condition:** the integration boundary is fully mapped.

### SUB-AGENT C -- Web & Docs Researcher

**Mission:** Search the web and official documentation for the user's domain.
Always search the web -- do not limit research to the codebase.

**Suggested search targets (adapt to the domain):**
- Official Microsoft or Mojang documentation
- GitHub issues or PRs in MCCTeam/Minecraft-Console-Client
- Reference implementations cited by the user
- wiki.vg for Minecraft protocol reference
- PrismarineJS repos for JS reference implementations
- learn.microsoft.com for .NET or auth APIs

**Output:** A concise reference document: best-practice approach, known
pitfalls, and links to authoritative sources. Flag conflicting information.

Await all sub-agent summaries before proceeding to Step 4.

---

## Step 4 -- Skill Discovery

Scan the `.claude/skills/` directory in the project root. Read the YAML
frontmatter (name + description) from each skill's `SKILL.md`. The current
MCC skills and their domains:

| Skill | When it's relevant |
|---|---|
| `csharp-best-practices` | Any task that writes or modifies C# code |
| `humanizer` | Any task that produces user-facing documentation |
| `mcc-chatbot-authoring` | Creating or modifying bots (built-in or script) |
| `mcc-dev-workflow` | Building MCC, starting test servers, debugging |
| `mcc-integration-testing` | Validating changes against a real Minecraft server |
| `mcc-version-adaptation` | Adding support for a new Minecraft version |

Identify which skills are relevant to the user's request. Record them for
inclusion in the generated prompt's `<available_skills>` block.

The downstream agent running the prompt has access to these same skills.
Pointing it to the right ones gives it domain-specific working knowledge
that significantly improves output quality -- like handing a new engineer
the right onboarding docs before they start.

---

## Step 5 -- Synthesis

Combine the sub-agent summaries, user answers, AGENTS.md context, and skill
catalogue into a single internal knowledge base:

```
## Synthesis Note

Goal (one sentence): ...
Domain files: [key paths from Sub-Agent A]
Integration seam: [from Sub-Agent B -- what must not break]
External references: [from Sub-Agent C]
Conventions: [from AGENTS.md engineering guidance]
Relevant skills: [from Step 4]
Blocking unknowns remaining: [if any, ask the user now]
```

If blocking unknowns remain, ask them now before generating the prompt.

---

## Step 6 -- Generate the Prompt

Read `references/reasoning-framework.md` and `references/prompt-patterns.md`
now if you have not already.

Build the final prompt using the **Prompt Assembly Checklist** below. Every
item must be addressed -- a missing item is a prompt defect.

### Prompt Assembly Checklist

- [ ] `<role>` block: domain expert covering all relevant technologies.
- [ ] `<context>` block: synthesised from user goal + sub-agent findings.
      Include the exact error message or failure mode if provided.
      Pre-answer known facts so the downstream agent does not re-derive them.
- [ ] `<agents_md>` directive: instruct the agent to read AGENTS.md for the
      module map, architecture, and engineering guidance.
- [ ] `<available_skills>` block: list the relevant skills from Step 4 with
      file paths and when to load each one.
- [ ] `<reasoning_protocol>` block: adapted ULTRATHINK framework.
      Phase 0 orientation pre-answered where certain.
      Phase 1 requirements pre-seeded from the synthesis note.
      Phase 2 decomposition pre-seeded with sub-tasks.
      Phase 2D exploration plan pre-populated with real file paths.
      Phase 4 self-validation items domain-specific and verifiable.
- [ ] Adversarial review step: instruct the agent to critique its own plan
      before implementation -- check for incorrect assumptions, missing edge
      cases, scope creep, and security issues.
- [ ] Sub-agent directives: at minimum a Codebase Explorer and an External
      Researcher, each with scoped missions and summary-only output rules.
- [ ] Plan mode directive: must appear before Phase 0. Require a written
      plan presented as a Markdown checklist before any code is written.
- [ ] `<design_goals>` block: 3-6 measurable, verifiable goals.
- [ ] `<scope_constraint>` block: name specific directories, classes, or
      files that must NOT be touched.
- [ ] `<output_format>` block: ordered delivery -- planning artefacts first,
      then implementation files.
- [ ] Web search mandate in at least one sub-agent directive.
- [ ] Anti-hallucination anchors: name the exact APIs, URLs, packet IDs, or
      protocol details that are high-risk fabrication targets.
- [ ] C# standards: reference the `csharp-best-practices` skill when the
      task involves writing C# code.

### Prompt structure template

Use this XML skeleton. Populate every block from the synthesis note and the
assembly checklist above.

```xml
<role>
[Domain expert covering: C# 14 / .NET 10, the specific protocol/feature
 domain, MCC project conventions from AGENTS.md]
</role>

<context>
[User goal restated. Known error or failure mode. Why the current state
 is insufficient. What "done" looks like. Key facts pre-answered.]
</context>

<agents_md>
Read AGENTS.md at the repository root before starting implementation.
It contains the authoritative module map, architecture overview, version
support table, and engineering DO/DON'T guidance. Use it to orient yourself
and scope your exploration. When AGENTS.md and other docs disagree, prefer
current code, then AGENTS.md.
</agents_md>

<available_skills>
The following project skills are at .claude/skills/ and should be loaded
(by reading their SKILL.md) when their domain applies to this task:

[List only relevant skills, one per line:]
- csharp-best-practices (.claude/skills/csharp-best-practices/SKILL.md):
  Read before writing or reviewing any C# code.
- [other relevant skills...]

Load skills just-in-time as you reach relevant work, not all upfront.
</available_skills>

<reasoning_protocol>
## Plan Before Code (non-negotiable)

Before writing any implementation code, produce and present a complete
written plan as a Markdown checklist. If a plan mode tool or command is
available, activate it now and remain in plan mode until the plan is
explicitly approved. Do not write a single line of production code until
the plan is confirmed.

[Adapted ULTRATHINK framework from references/reasoning-framework.md.
 Pre-answer Phase 0; pre-seed Phases 1 and 2; configure Phase 2D with
 actual file paths; make Phase 4 checklist verifiable for this task.

 Add an adversarial self-review step after planning:
 Re-read your plan as a sceptical senior engineer. Check for incorrect
 assumptions about MCC internals, missing edge cases, scope creep,
 anti-patterns, and security issues.]
</reasoning_protocol>

<design_goals>
[3-6 measurable, verifiable goals. Each checkable with a yes/no answer.]
</design_goals>

<scope_constraint>
[What must NOT be modified. Name specific directories, classes, or files.
 What must remain backwards-compatible. What to avoid even if it seems
 helpful.]
</scope_constraint>

<output_format>
[Ordered: planning artefacts first (checklist, design decisions, critique
 summary), then implementation files, then compliance report.]
</output_format>
```

### Sub-agent output discipline

Every sub-agent directive in the generated prompt must include:

> "Return a concise written summary only. Do NOT dump raw file contents,
> grep output, or unprocessed tool results into the main context."

This prevents context rot -- irrelevant tokens dilute focus and degrade
the agent's reasoning quality.

---

## Step 7 -- Prompt Quality Gate

Before delivering, verify every item:

```
- [ ] Every block (<role>, <context>, <agents_md>, <available_skills>,
      <reasoning_protocol>, <design_goals>, <scope_constraint>,
      <output_format>) is present and non-empty.
- [ ] The prompt directs the agent to read AGENTS.md for orientation.
- [ ] <available_skills> lists the correct skills for this task's domain.
- [ ] Phase 2D has actual file paths, not generic placeholders.
- [ ] Plan mode directive appears before Phase 0.
- [ ] All sub-agents have scoped missions and summary-only output rules.
- [ ] At least one sub-agent has an explicit web search mandate.
- [ ] Phase 4 items are objectively verifiable for THIS task.
- [ ] Anti-hallucination anchors target this domain's fabrication risks.
- [ ] Scope constraint is specific enough to prevent accidental drift.
- [ ] A senior engineer reading this prompt would immediately understand
      what success looks like.
```

Fix any unchecked items before delivering.

---

## Step 8 -- Deliver

Present the generated prompt in a fenced code block (` ```xml `) so the user
can copy it cleanly.

Follow with a brief plain-English summary (3-5 sentences) explaining:
- What the prompt will instruct the agent to do
- Which MCC files and skills the agent will be directed to
- The most likely blocking decision points
- Any remaining assumptions the user should validate

---

## Anti-patterns -- never do these

- Do not ask more than 3-4 clarifying questions at once.
- Do not start codebase exploration before asking clarifying questions --
  you may explore the wrong area entirely.
- Do not generate a prompt that skips the planning phase.
- Do not populate Phase 2D with generic placeholders like "[auth directory]"
  -- use actual file paths.
- Do not produce a prompt with vague scope constraints. "Don't touch
  unrelated code" requires the agent to guess. Name the specific files
  and directories that are out of bounds.
- Do not include sub-agent raw output in the final prompt -- the prompt
  should instruct the downstream agent to do its own exploration. Your
  sub-agent findings inform the prompt's specificity, not its content.
- Do not list skills in `<available_skills>` that are irrelevant to the task.
