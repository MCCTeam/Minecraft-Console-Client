# Theory-Aligned Pathing Regression

## Context
MCC already has two useful but separate assets for pathing and parkour validation:

- [tools/sim_jump_reach.py](/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/tools/sim_jump_reach.py) models a subset of vanilla jump reachability and can answer whether specific jump shapes are theoretically reachable.
- The live harness scripts under [tools/](/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/tools) validate real MCC behavior on a local server, but they currently act as curated scenario suites rather than a stable projection of one theoretical source of truth.

The immediate goal is to make the simulator the authority for first-wave jump capability claims, then align a smaller live regression layer to that authority. This first wave must stay intentionally narrow: it should cover only movement families already modeled by `sim_jump_reach.py`, not every higher-level execution behavior MCC currently exercises live.

## Requirements
- Treat `tools/sim_jump_reach.py` as the authority for first-wave jump capability expectations.
- Restrict first-wave coverage to movement families already modeled by the simulator:
  - linear flat jumps
  - linear ascend jumps
  - linear descend jumps
  - neo jumps
  - ceiling-constrained or headhitter jumps
- Produce both machine-readable and human-readable theory outputs from the same source data.
- Define live regression coverage through canonical buckets, not by replaying every theoretical case.
- Ensure every theory-aligned live case can be traced back to one or more theory case IDs.
- Keep existing specialized live suites available, but do not treat them as part of the first-wave theory authority.
- Preserve the current MCC local workflow based on `tools/mcc-env.sh`, `mcc-debug`, tmux-backed local sessions, and shared local servers.

## Design

### Recommended approach
Three approaches were considered:

1. Hand-maintain theory expectations and live cases separately.
2. Make the simulator authoritative, then select canonical live buckets from its output.
3. Fully auto-generate all live cases from simulator output.

Approach 2 is the recommended first-wave design. It keeps one theory authority, creates a stable contract for live coverage, and avoids over-scoping the first iteration with full live generation.

### Capability layers
The regression system should be split into three layers with explicit responsibilities:

- Theory matrix
  - Generated from `tools/sim_jump_reach.py`.
  - Defines what MCC is expected to support for the first-wave movement families.
- Canonical live coverage
  - Derived from the theory matrix by bucket rules.
  - Validates representative easy, boundary, and reject scenarios on a real server.
- Specialized live suites
  - Existing higher-level pathing suites such as mixed-route, braking, or landing-recovery scenarios.
  - Remain valuable, but are explicitly outside the first-wave theory contract until their behaviors also have a stable theoretical source.

This separation prevents higher-level execution scenarios from contaminating the meaning of the first-wave authority layer.

### Theory matrix schema
The theory matrix should be stored as a fine-grained case table. Each row represents one distinct theoretical movement judgment. The table should include at least:

- `case_id`
- `family`
- `subfamily`
- `movement_mode`
- `momentum_ticks`
- `gap_blocks`
- `delta_y`
- `ceiling_height`
- `wall_width`
- `expected_reachable`
- `landing_x`
- `apex_y`
- `margin`
- `notes`

Recommended family and subfamily values for the first wave:

- `linear`
  - `flat`
  - `ascend`
  - `descend`
- `neo`
- `ceiling`
  - `headhitter`

The important contract is that `expected_reachable` comes from the simulator, not from handwritten shell-script expectations.

### Canonical bucket model
Live coverage should not replay every theoretical case. Instead, the theory matrix should be grouped into canonical buckets that classify the live representative scenarios. Each canonical bucket should have stable dimensions:

- `family`
- `subfamily`
- `movement_mode`
- `difficulty_band`

The first-wave difficulty bands are:

- `easy`
  - clearly reachable with generous margin
- `boundary`
  - close to the theoretical edge and most likely to regress
- `reject`
  - theoretically unreachable and expected to be rejected live

Each canonical live case must reference:

- `case_id`
- `bucket_id`
- `expected_result`
- `world_recipe_id`
- `start`
- `goal`

This ensures the live harness is executing a curated projection of the theory matrix rather than inventing expectations independently.

### First-wave movement scope
The first-wave theory authority covers only what `sim_jump_reach.py` already models directly:

- linear flat jumps
- linear ascend jumps
- linear descend jumps
- neo jumps
- ceiling-constrained or headhitter jumps

The first wave explicitly does not promote these existing live-only behaviors into theory authority:

- repeated parkour chains
- parkour landing recovery into turns
- braking and speed-carry transitions
- mixed long-route execution
- segment-to-segment transition behavior

Those scenarios remain useful, but they belong to specialized live suites until a simulator-backed authority exists for them.

### Output artifacts
The simulator-backed generation step should produce three synchronized outputs from the same in-memory data:

- JSON
  - primary machine-readable artifact for automation
- CSV
  - convenient for inspection, filtering, and quick diffs
- Markdown
  - human-readable capability summary and bucket overview

The design requires these outputs to be generated in one pass so they cannot silently drift apart.

### Live suite reorganization
The first-wave live layer should be organized into theory-aligned and specialized suites.

Theory-aligned suites:

- Refactor [tools/test-parkour.sh](/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/tools/test-parkour.sh) into the main theory-aligned linear-jump suite.
- Add a dedicated live suite for neo and ceiling-constrained cases.

Specialized live suites retained outside the theory contract:

- [tools/test-pathing-jump-combos.sh](/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/tools/test-pathing-jump-combos.sh)
- [tools/test-pathing-template-regressions.sh](/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/tools/test-pathing-template-regressions.sh)
- [tools/test-pathing-long-routes.sh](/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/tools/test-pathing-long-routes.sh)
- [tools/test-transition-braking.sh](/home/ryan/Minecraft/Minecraft-Console-Client-milutinke/tools/test-transition-braking.sh)

This lets MCC keep broader pathing smoke coverage without pretending every advanced live script is already grounded in the simulator.

### Execution and comparison flow
The first-wave regression pipeline should be one directional:

1. Generate the full theory matrix from `sim_jump_reach.py`.
2. Derive canonical buckets and canonical live cases from that matrix.
3. Run the theory-aligned live suites against the canonical live case set.
4. Join live results back to theory case IDs and produce a comparison report.

Live suites must not encode the truth model themselves. They are executors and verifiers only.

### Result model
The comparison layer should use these result classes:

- `expected_pass / live_pass`
- `expected_pass / live_fail`
- `expected_reject / live_reject`
- `expected_reject / live_unexpected_pass`
- `invalid_live_case`

`invalid_live_case` is reserved for harness or environment faults such as malformed geometry, invalid goals, startup failure, or RCON and session issues. It should not be treated as a capability result.

### File layout
The first-wave implementation should keep the layout conservative:

- Keep `tools/sim_jump_reach.py` as the theory entry point.
- Add theory export outputs under `tools/` or a closely related generated-output location.
- Add a canonical live-case manifest under `tools/` or a nearby data location suitable for shell-script consumption.
- Reuse existing `tools/mcc-env.sh` helpers, `mcc-debug`, tmux-backed MCC sessions, and shared local server management.

No change is required to the core MCC runtime architecture for the first-wave design itself.

### Delivery order
The implementation should proceed in this order:

1. Stabilize theory export generation from `sim_jump_reach.py`.
2. Define canonical bucket and world-recipe selection rules.
3. Convert `tools/test-parkour.sh` to consume canonical theory-aligned cases.
4. Add the theory-aligned neo and ceiling live suite.
5. Leave specialized live suites in place with documentation clarifying that they are outside the first-wave theory authority.

This order keeps truth-generation ahead of live execution and avoids locking shell suites to premature handwritten expectations.

## Validation
- Generate the theory matrix and confirm JSON, CSV, and Markdown outputs are produced from the same dataset.
- For each first-wave bucket, require at least one canonical `easy`, `boundary`, and `reject` live case where applicable to that movement family.
- Record theory case ID, bucket ID, world recipe ID, expected result, live result, and MCC log path for every theory-aligned live case.
- Run theory-aligned live suites using the existing local server workflow through `source tools/mcc-env.sh` and `mcc-debug`.
- Keep specialized live suites runnable as separate checks, but do not block first-wave theory alignment on converting them.

## Open questions
- None for the first-wave scope. Higher-level mixed execution behaviors are intentionally deferred until a simulator-backed authority exists for them.
