---
name: csharp-async-best-practices
description: Use when reviewing, writing, refactoring, or designing c# async code that uses task, task-generic, valuetask, cancellationtoken, task.whenall, task.whenany, task.run, configureawait, async void, or fire-and-forget patterns. Trigger on `.result`, `.wait()`, deadlocks, cancellation propagation, asp.net core background work, ui responsiveness, exception flow, and performance-sensitive async api design.
metadata:
  category: technique
  triggers:
    - c#
    - async
    - task
    - valuetask
    - cancellationtoken
    - configureawait
    - .result
    - .wait()
    - async void
    - fire-and-forget
    - task.run
    - whenall
    - whenany
    - asp.net core
    - deadlock
---

# C# Async Best Practices

## Overview

Apply evidence-backed async guidance with this priority order:

1. correctness and cancellation semantics
2. context-specific API design
3. concurrency behavior and failure handling
4. performance tuning only when the hot path is real

Treat blanket advice as suspect. Separate official behavior from expert interpretation and from your own recommendation.

## Workflow

1. Classify the code before judging it.
   - **I/O-bound async**: network, file, database, timers, async waits
   - **CPU-bound work**: expensive computation
   - **Context**: library, UI app, ASP.NET Core app, background service, test code
   - **Pressure**: hot path or ordinary path
2. Prefer the least surprising correct design.
3. Only optimize allocations or scheduling after the correctness story is sound.
4. Load the matching reference file before making strong claims.
   - **General rules and code review defaults**: `references/core-guidance.md`
   - **Context-sensitive rules**: `references/context-and-tradeoffs.md`
   - **Source notes and authority breakdown**: `references/source-notes.md`

## Review defaults

Start from these defaults unless the case-specific evidence says otherwise:

| Topic | Default judgment |
|---|---|
| Blocking on async | usually a defect or interop boundary smell |
| `async void` | only acceptable for event handlers |
| `ValueTask` | avoid by default; justify with measurements or a very hot path |
| `ConfigureAwait(false)` | good library default, not an app-wide default |
| `Task.Run` | use to offload CPU work when needed, not to fake async I/O |
| Fire-and-forget | assume unsafe until lifecycle, scope, and exception handling are explicit |
| `Task.WhenAll` | prefer for independent concurrent operations |
| `Task.WhenAny` | always inspect winner and define what happens to losers |
| Cancellation | accept and propagate token until the point of no cancellation |

## Output contract

When you review or design code, label your reasoning like this:

- **Fact**: official runtime or API behavior
- **Expert guidance**: interpretation from strong experts when it adds design meaning
- **Synthesis**: your recommendation for this exact case

Do not present contextual advice as a universal law.

## Common traps

- Calling `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` inside normal async-capable code
- Recommending `ConfigureAwait(false)` everywhere because “it is .NET Core” or “it prevents deadlocks”
- Recommending `Task.Run` inside ASP.NET Core request code just to make code “more async”
- Recommending `ValueTask` for every hot-looking method without checking completion behavior, call frequency, or single-consumer assumptions
- Ignoring cancellation after plumbing a `CancellationToken`
- Using `Task.WhenAny` without awaiting the returned winner task or handling the remaining tasks
- Treating fire-and-forget as harmless when it touches scoped services, `HttpContext`, or unobserved failures

## Rationalization traps

| Rationalization | Better reasoning |
|---|---|
| “It works, so `.Result` is fine.” | Lack of failure under one context does not make blocking safe or scalable. |
| “`ValueTask` is always faster.” | It trades simplicity for niche allocation wins and stricter consumption rules. |
| “`ConfigureAwait(false)` everywhere is modern guidance.” | Library and app code have different constraints. Blanket rules are weak. |
| “`Task.Run` makes server code asynchronous.” | It only queues work; it does not turn blocking I/O into true async I/O. |
| “Fire-and-forget is okay because logging exists.” | Logging does not solve scope lifetime, shutdown, retries, or error propagation. |

## Deliverable shape

For code review or implementation help, prefer:

1. a short context classification
2. the concrete problem
3. the corrected pattern
4. the context-dependent tradeoff, if any
5. the smallest safe code change

## API shape and testability

- Prefer `Async` suffixes for awaitable-returning methods unless an established contract or event pattern dictates otherwise.
- Prefer `Task`-returning seams over hidden background work so tests can await completion, faults, and cancellation.
- For timers, queues, retries, or background pipelines, recommend abstractions that let tests control time and observe completion.
- When reviewing an async API, ask whether callers can compose it, cancel it, await it, and assert its failure behavior.

## Hard boundaries

- Do not endorse sync-over-async as a normal design choice.
- Do not suggest `async void` except for event handlers.
- Do not suggest `ValueTask` unless the constraints are understood.
- Do not claim `ConfigureAwait(false)` is always needed or always unnecessary.
- Do not approve fire-and-forget unless ownership, exception handling, and lifetime are explicit.
