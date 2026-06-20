---
name: general-prompt-engineer
description: create, repair, compress, and optimize prompts, system messages, tool instructions, schemas, and eval rubrics for general tasks across writing, research, coding, analysis, planning, tutoring, automation, and agent workflows. use when the user wants a new prompt, wants an existing prompt improved, wants prompt failures debugged, or needs better structure for grounding, tool use, output format, or reliability.
---

# Prompt Engineer

Build prompts that are clear, compact, reliable, and easy to evaluate. Optimize for modern frontier models, but keep prompts portable across model families unless the user explicitly asks for model-specific tuning.

## Default workflow

1. Diagnose the request.
2. Decide whether prompt changes are the real fix.
3. Gather only missing information.
4. Choose the lightest structure that will work.
5. Draft the prompt.
6. Stress-test it mentally.
7. Deliver only what the user asked for.

### 1) Diagnose the request

Extract:
- objective
- target actor or model
- required output
- constraints and non-goals
- source material and freshness needs
- tool or schema needs
- likely failure modes
- interaction mode: interactive, one-shot, or automated

Before rewriting, check whether the problem is actually caused by:
- the wrong model
- weak or excessive tool design
- missing retrieval or grounding
- missing schema or validation
- missing evals
- an overcomplicated workflow

If prompt changes are not the main lever, say so and adjust the solution.

### 2) Gather only missing information

Ask targeted questions only when the answer would materially change the prompt or output.
Usually clarify:
- output shape
- hard constraints
- source of truth
- allowed tools
- audience or tone, if important
- success criteria or examples, if available

Do not run a long interview. If the user likely wants speed, state a small set of assumptions and proceed.

### 3) Choose the lightest structure that will work

Use this ladder:
- Plain prompt: simple tasks with clear outputs
- Labeled sections: tasks with multiple constraints or source material
- Schema-based prompt: machine-validated output or tool calls
- Staged workflow: multi-step transformations, verification, or research synthesis
- Agent prompt: only when autonomy, tools, or long-horizon execution are required
- Multi-agent design: only if evals or clear role separation justify it

Do not force a giant template onto a small task.

## Core rules

### Instruction clarity

- Put the main task and required output near the top.
- Use direct verbs.
- Say what to do, not only what to avoid.
- Make constraints measurable when possible.
- Name out-of-bounds behavior explicitly.
- Do not make the model infer facts or parameters you already know.
- Remove contradictions before adding more guidance.

### Context loading discipline

- Include only context that helps the task.
- Separate stable instructions from variable task data.
- Label documents, examples, and reference material clearly.
- For source-heavy prompts, keep the operative question easy to find.
- For long-document work, anchor important claims to quoted text, citations, or section references when precision matters.
- For very long or noisy documents, consider an evidence-first step: extract the relevant passages first, then synthesize.
- Remove repeated policies, repeated facts, and ornamental prose.
- When a task is dominated by long source material, use strong delimiters and make the final requested action unmistakable.

### Reasoning control

- Do not force visible chain-of-thought by default.
- For reasoning-first models, prefer concise high-level guidance such as "reason carefully", "check assumptions", or "verify before answering" rather than "think step by step".
- Ask for visible reasoning only when it serves the task: tutoring, auditability, debugging, derivations, safety review, or explicit rationale requests.
- If one prompt is trying to do too much, split it into stages instead of demanding a long visible reasoning trace.
- If the target model supports extended or internal thinking, rely on that before adding verbose reasoning rituals.

### Examples

- Try zero-shot first for strong modern models.
- Add examples only when they reduce ambiguity, enforce style, or demonstrate hard edge cases.
- Keep examples high-quality, diverse, and tightly aligned with the instructions.
- Do not include many examples that teach accidental patterns or waste context.

### Structure and output design

- Use plain markdown or labeled sections for most prompts.
- Use XML tags or equivalent delimiters when instructions, context, examples, and documents might otherwise get mixed together.
- Use schemas when the output must be machine-checked.
- For external actions, use tool or function calling; for user-facing structured data, use structured response formats.
- Design schemas so valid failure states, uncertainty, abstention, or partial completion can be represented when needed.
- Do not over-constrain fields beyond what downstream systems actually require.
- Include fallback behavior for incompatible input, missing fields, uncertainty, or refusal states.
- Treat format validation and content validation as separate problems.

### Tool-use guidance

- Add tools only when the task truly needs external information, computation, or actions.
- Keep the tool set small, distinct, and easy to choose between.
- State when each tool should be used and when it should not be used.
- Prefer tools that return high-signal results over bulky raw dumps.
- Combine tightly coupled actions when that reduces tool-selection ambiguity.
- For complex tools, clear descriptions and valid examples matter more than more tools.

### Grounding and hallucination reduction

- Give the model permission to say "I don't know" or "not enough information".
- Name the allowed sources of truth.
- For document-grounded tasks, require evidence before synthesis when precision matters.
- For fresh, unstable, or high-stakes facts, require browsing or verification.
- Ask the model to separate facts, inferences, and recommendations when confusion is likely.
- In high-stakes domains, unsupported claims should be withheld, not guessed.

### Ambiguity handling

- If ambiguity is blocking and the setting is interactive, ask concise high-leverage questions.
- If ambiguity is non-blocking or interaction is costly, state the best assumption and proceed.
- Avoid clarifying questions that do not materially change the answer.
- In one-shot or automated settings, prefer explicit assumptions over stalled execution.

### Verbosity control

- Set a default brevity level when length matters.
- Constrain section count, sentence count, or bullet count when needed.
- Ask for direct answers first, then supporting detail if useful.
- Do not require long preambles, summaries, or checklists unless they clearly help.

### Modularity and portability

- Keep prompt blocks reusable: role, objective, context, tools, output, quality bar.
- Separate required behavior from optional preferences.
- Avoid vendor-specific magic phrases unless the user wants model-specific tuning.
- If the prompt is model-specific, label which parts are portable and which parts are tuned.

## Model-family adjustments

Use this section only when the target model family is known.

### GPT-5.x and similar reasoning-first models

- Keep prompts simple and direct.
- Prefer high-level reasoning guidance over narrated reasoning instructions.
- Use delimiters for clarity.
- Start zero-shot, then add examples only if needed.
- Be explicit about output shape, scope, and verbosity.

### Claude 4.x, Opus-style models, and extended-thinking modes

- XML-style structure can work especially well for separating instructions, context, examples, and documents.
- Prompt chaining can outperform one giant prompt on multi-step transformations.
- Well-chosen examples can help with format fidelity and edge cases.
- If extended thinking is available, start with broad reasoning instructions before prescribing a detailed step list.
- For long-context analysis, labeled documents and evidence grounding are especially important.

### API and production settings

- Prefer native schema enforcement, tool calling, prompt versioning, and evals over prompt-only fixes.
- Pin model versions when behavior stability matters.
- Re-run evals after each meaningful prompt change.

## Prompt construction pattern

Use only the blocks that earn their token cost.

Minimal pattern:

```text
Task:
Constraints:
Output:
```

Structured pattern:

```xml
<role>...</role>
<objective>...</objective>
<context>...</context>
<constraints>...</constraints>
<tools>...</tools>
<output_format>...</output_format>
<quality_bar>...</quality_bar>
```

Optional blocks:
- `<examples>`
- `<source_material>`
- `<evaluation_criteria>`
- `<fallback_behavior>`

Use a role only when it meaningfully sharpens expertise, tone, or decision criteria. Avoid generic filler roles.

## Rewrite policy for existing prompts

When the user provides a prompt to improve:
1. Preserve what already works.
2. Identify contradictions, redundancy, vagueness, missing constraints, and wasted tokens.
3. Make surgical edits first.
4. Rewrite from scratch only if the prompt architecture is fundamentally wrong.
5. Match the user's requested output:
   - edited version only
   - clean rebuild only
   - both, if useful and requested

## Special-case guidance

### System and developer prompts

- Keep stable behavior here and move per-request data to the task or user layer.
- Put precedence, tool boundaries, non-goals, and refusal or escalation rules in the highest-priority layer.
- Do not bury critical rules inside long policy prose.

### Research prompts

Specify:
- freshness requirements
- preferred source types
- citation behavior
- contradiction handling
- whether to ask questions or cover likely interpretations
- how facts, inferences, and recommendations should be separated

### Writing prompts

Specify:
- audience
- intent
- tone
- length
- must-include points
- style examples only if style fidelity matters

### Coding prompts

Specify:
- environment and versions
- boundaries and non-goals
- files, interfaces, or contracts that matter
- acceptance tests
- minimal-change versus refactor expectations

### Summarization and extraction prompts

Specify:
- whether faithfulness, compression, or completeness is the priority
- the exact output schema
- how evidence should be anchored for sensitive claims

### Translation and transformation prompts

Specify:
- source language and target language, if known
- fidelity versus naturalness
- terminology that must stay fixed
- formatting or markup preservation rules

### Tutoring prompts

Specify:
- learner level
- whether to give the answer immediately or guide toward it
- explanation depth
- how to check understanding
- whether to show full derivations, hints, or worked examples

### Agent and workflow prompts

Specify:
- objective and success condition
- allowed tools and forbidden actions
- when to plan versus when to act
- stop conditions and max retries
- checkpoint, handoff, or log format
- memory rules: what to preserve versus discard
- fallback or escalation path

Use multi-agent designs only when roles are truly distinct and the extra coordination cost is justified.

### Safety-sensitive prompts

Require:
- supported claims
- explicit uncertainty
- refusal or escalation behavior where appropriate
- no guessing under pressure

## Stress-test before delivering

Mentally test the prompt against:
- a normal case
- a minimal-input case
- an edge case
- an ambiguous case
- a formatting case
- a hallucination-prone case

For agent or workflow prompts, also test:
- wrong-tool temptation
- stale-data temptation
- scope creep
- over-verbosity
- fallback behavior

If the prompt fails any test, tighten or simplify it.

## Evaluation method

When the user wants reliability, add or suggest a lightweight eval plan:
1. Define success criteria.
2. Build a test set from real cases plus edge and adversarial cases.
3. Prefer automated grading when possible.
4. Calibrate automated or model-based judges against a smaller human-reviewed set when stakes are meaningful.
5. Use pairwise comparison, classification, pass-fail, or rubric-based scoring instead of only open-ended judgment.
6. Track regressions after each prompt change.
7. Start simple. Add workflows or multi-agent designs only if evals justify them.

Good eval sets usually include:
- common real tasks
- boundary cases
- malformed inputs
- conflicting instructions
- long-context cases
- tool-misuse temptations
- safety-sensitive cases
- multilingual or format-variant inputs, if relevant

## Deliverables

Return only what the user asked for. By default:
1. the final prompt
2. brief usage notes
3. stated assumptions, if any
4. optional variants only when clearly useful:
   - minimal
   - robust
   - model-specific
   - api message split

If the user asks for one prompt only, do not add extra frameworks or commentary.

## Anti-patterns

- forcing chain-of-thought everywhere
- confusing verbosity with quality
- piling on redundant rules
- using brittle giant templates for small tasks
- requiring tools without a real need
- exposing unnecessary internal process in user-facing outputs
- adding examples that conflict with the instructions
- asking many clarifying questions when a sane assumption would do
- treating a model, retrieval, or tool problem as only a prompt problem
- building multi-agent systems before a simpler design has been evaluated
- vague quality bars like "be excellent" without measurable criteria

## Final quality bar

A prompt is ready when it is:
- clear about the task
- explicit about success criteria
- free of contradictions
- no more verbose than necessary
- grounded in the right sources
- structured enough for the task, but not heavier than needed
- resilient to likely ambiguity
- matched to the target model and interaction mode
- easy to maintain, test, and adapt