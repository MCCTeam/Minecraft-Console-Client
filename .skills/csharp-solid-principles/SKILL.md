---
name: csharp-solid-principles
description: Apply SOLID principles pragmatically to C# and .NET code. Use this skill whenever the user asks to explain, review, refactor, design, or critique C#/.NET code for maintainability, extensibility, cohesion, coupling, testability, dependency injection, interface design, inheritance/substitutability, service boundaries, or code smells—even if they do not explicitly mention SOLID. Use it for ASP.NET Core services, libraries, domain/application code, legacy refactors, architecture reviews, and teaching. Prefer evidence-based, behavior-preserving improvements over cargo-cult abstractions.
metadata:
  compatibility: Designed for modern C#/.NET code, including ASP.NET Core and Microsoft.Extensions.DependencyInjection.
---

# C# SOLID Principles

Use SOLID as a set of design heuristics for managing change, contracts, and dependencies—not as five rules that every class must visibly demonstrate.

## Core stance

- Optimize for maintainability around **real change pressure**, not maximum abstraction.
- Preserve behavior first when refactoring legacy code. Add characterization tests before risky structural changes when feasible.
- Diagnose the actual design problem before naming a principle. A class can be large without violating SRP, and a `switch` can be perfectly appropriate without violating OCP.
- Do not force all five principles into every review. Mention only principles that materially explain the design pressure.
- Prefer the smallest refactoring that improves cohesion, contract clarity, or dependency direction.
- Distinguish **Dependency Inversion Principle (DIP)** from **Dependency Injection (DI)** and **Inversion of Control (IoC)**. DI can support DIP, but they are not synonyms.
- Avoid speculative interfaces. Introduce abstractions around meaningful roles, volatile policies, external infrastructure, or test seams—not around every concrete class.
- Treat public APIs and interfaces as behavioral contracts. Signature compatibility alone is not enough for LSP.

## First decide what the user needs

Choose the response mode that matches the request:

1. **Teach** — explain one or more principles with realistic C# bad/good examples and tradeoffs.
2. **Review** — identify concrete smells in supplied code, rank by impact, and map only relevant findings to SOLID.
3. **Refactor** — preserve behavior, show a minimal sequence of changes, and provide revised C#.
4. **Design** — propose boundaries, abstractions, ownership, DI registrations, and tests for a new feature.
5. **Troubleshoot architecture** — inspect dependency direction, service lifetimes, interface contracts, and extension points.

Read `references/examples.md` when teaching, reviewing, or refactoring a specific SOLID principle. Read `references/review-playbook.md` for diagnostic questions and review structure. Read `references/sources.md` when grounding claims, resolving ambiguity, or citing external guidance.

## Principle guide

### S — Single Responsibility Principle

Interpret SRP as **cohesion around one responsibility / one primary reason to change**, not “one method per class.” Ask which actor, policy, or source of change owns the behavior.

Look for:
- business policy mixed with persistence, transport, formatting, telemetry, or email;
- unrelated change reasons landing in the same class;
- a service whose constructor keeps growing because it coordinates unrelated concerns;
- tests that must mock many unrelated collaborators to exercise one behavior.

Prefer:
- cohesive domain/application services;
- orchestration separated from specialized policy/infrastructure components;
- extraction only where responsibilities truly change independently.

Do not automatically split a class merely because it is long.

### O — Open/Closed Principle

Design stable behavior so expected variations can be added by extension or composition instead of repeatedly editing a central conditional.

Look for:
- the same `switch`/`if` on a type, channel, country, payment method, or policy repeated across the codebase;
- every new variant requiring edits in several stable classes;
- extension points that already have multiple implementations and keep growing.

Prefer:
- strategy/policy objects, delegates, handlers, decorators, or polymorphism when a variation axis is proven;
- registration/composition at the application edge;
- exhaustive switches for closed domains when that is clearer and safer.

Do not create a plugin architecture for hypothetical changes.

### L — Liskov Substitution Principle

Any implementation used through a base class or interface must honor the expectations promised by that abstraction. Check behavior, not only type compatibility.

Evaluate:
- **preconditions** — subtype/implementation must not demand more than the contract;
- **postconditions** — it must not promise less;
- **invariants** — required state rules remain true;
- **failure semantics** — no surprising `NotSupportedException`, silent no-op, or unrelated exception where the contract implies support;
- **side effects and ordering** — observable behavior remains compatible;
- **nullability, cancellation, idempotency, and async semantics** when these are part of the API contract.

Prefer composition or narrower role interfaces when not every implementation can honor the same contract.

### I — Interface Segregation Principle

Shape interfaces around the needs of clients and coherent roles. Consumers should not depend on members they do not use, and implementers should not be forced to fake unsupported behavior.

Look for:
- large “service” or repository interfaces used differently by unrelated clients;
- implementations throwing `NotSupportedException` for interface members;
- mocks with many irrelevant setup members;
- read-only consumers depending on write/admin operations.

Prefer small role interfaces such as `IOrderReader`, `IOrderWriter`, or capability-focused abstractions. One class may implement multiple role interfaces.

Do not fragment interfaces into one-member types without a client-driven reason.

### D — Dependency Inversion Principle

High-level policy should not be coupled directly to low-level infrastructure details. Put abstractions at the boundary that reflects the higher-level need, then let infrastructure implement them.

Look for:
- domain/application code constructing database, HTTP, file-system, SMTP, clock, or queue details directly;
- static/global service access;
- `IServiceProvider` used as a service locator inside business logic;
- Core/Application projects referencing Infrastructure projects;
- infrastructure-shaped interfaces leaking into policy code.

Prefer:
- constructor-injected explicit dependencies;
- abstractions named after the capability the policy needs;
- infrastructure depending on application/core contracts;
- DI registration in the composition root.

For ASP.NET Core/.NET DI, also check lifetimes. A singleton must not capture scoped services, and container-owned disposables should normally be disposed by the container.

## C#/.NET-specific review rules

- Favor constructor injection for required collaborators. Optional dependencies should be genuinely optional in behavior, not hidden requirements.
- Avoid injecting `IServiceProvider` merely to resolve arbitrary services later; that hides dependencies and behaves like service location.
- Do not introduce an interface solely to mock a class when a simpler seam (pure function, delegate, `TimeProvider`, wrapper around infrastructure, or real lightweight implementation) is clearer.
- Use records/value objects for data semantics when they improve invariants; SOLID does not require everything to be a mutable class hierarchy.
- Prefer composition over inheritance when variation is behavioral rather than taxonomic.
- Preserve cancellation by accepting and passing `CancellationToken` through async boundaries when the operation contract is cancellable.
- Preserve nullability contracts. A subtype that returns `null` where callers of the abstraction expect non-null violates substitutability even if the compiler can be bypassed.
- Be careful with DI lifetimes: scoped dependencies inside singletons are a correctness problem, not merely a style issue.
- Keep composition-root wiring separate from business policy. Concrete types are normal at the application edge.

## Review workflow

When code is provided:

1. State the code's apparent responsibility and dependency boundaries in 1–3 sentences.
2. Identify the **change axes**: what is likely to vary independently (business policy, transport, persistence, formatting, provider, jurisdiction, workflow step, etc.).
3. Identify behavioral contracts visible through interfaces/base classes.
4. Find concrete evidence of coupling, mixed responsibility, unsupported capabilities, or brittle modification points.
5. Rank findings by practical impact: correctness risk, change amplification, test friction, then style.
6. For each meaningful finding, provide:
   - **Evidence** — the exact code pattern.
   - **Principle** — only if it clarifies the issue.
   - **Why it matters** — likely maintenance/test/correctness cost.
   - **Smallest useful refactoring** — avoid redesigning the entire system.
7. Show revised C# when the user asked for code or when it materially clarifies the recommendation.
8. Add or suggest tests that lock down behavior before/after the refactor.
9. Call out tradeoffs and cases where the original is reasonable.

## Refactoring workflow

For non-trivial legacy code, favor incremental steps:

1. Capture current behavior with focused tests.
2. Extract pure calculations or policies before infrastructure.
3. Introduce an abstraction only at a demonstrated seam.
4. Move infrastructure behind that seam.
5. Replace repeated conditionals with polymorphism/handlers only if the variation repeats or is expected to grow.
6. Split interfaces based on actual consumers.
7. Re-check behavioral substitutability.
8. Wire implementations in the composition root and validate lifetimes.
9. Keep public API changes explicit; offer a compatibility-preserving option when possible.

## Default answer shape for code reviews

Use this structure unless the user requests another format:

### Assessment
A concise summary of the design and the highest-value issue.

### Findings
For each significant finding:
- **Finding / principle:**
- **Evidence:**
- **Impact:**
- **Refactoring:**

### Revised code
Show the smallest coherent example, not a giant rewrite.

### Tests to protect behavior
List the behaviors that should be locked down.

### Tradeoffs
Explain where added abstraction is or is not justified.

## Teaching SOLID

When the user asks to learn SOLID:

- Define each principle in plain C# terms.
- Show a realistic **Bad** example and explain the concrete maintenance problem.
- Show a realistic **Good** version and explain what changed.
- Include one “do not over-apply this” caveat per principle.
- Emphasize the relationships:
  - SRP and ISP both improve cohesion, at class and API boundaries respectively.
  - OCP often emerges from good abstractions and composition.
  - LSP determines whether polymorphic extension is actually safe.
  - DIP controls dependency direction; DI is one mechanism for supplying those dependencies.

Use `references/examples.md` as the canonical example bank.

## When to push back

Push back gently when the proposed refactor would:

- create interfaces with only one speculative implementation and no meaningful seam;
- replace a clear closed-domain switch with a complex hierarchy;
- split a cohesive class just to reduce line count;
- add DI to pure data/value objects;
- introduce inheritance where composition is simpler;
- preserve a misleading abstraction whose implementations cannot honor the same behavior;
- make code harder to understand without reducing meaningful change risk.

Explain which future change would justify the abstraction later.

## Quality bar

A strong SOLID recommendation should make at least one of these measurably better:

- fewer places changed for a likely feature;
- smaller blast radius for infrastructure changes;
- clearer behavioral contract;
- fewer irrelevant dependencies for a consumer;
- safer substitution among implementations;
- simpler focused tests;
- clearer ownership of business rules.

If none improve, prefer the simpler design.
