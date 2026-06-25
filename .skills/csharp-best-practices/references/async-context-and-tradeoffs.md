---
description: >-
  Context-specific async guidance for library code, ui apps, asp.net core,
  background work, task.run, configureawait, and performance-sensitive design.
metadata:
  tags: [configureawait, task.run, asp.net core, ui, library, performance]
  source: mixed
---

# Context and Tradeoffs

## Library code versus app code

### General-purpose library code
- Prefer APIs that expose true async for I/O-bound work.
- Do not add async wrappers around purely compute-bound methods just to look modern. Expose sync compute APIs and let callers decide whether to offload.
- `ConfigureAwait(false)` is a strong default when the library does not need the caller’s context.
- Avoid ambient assumptions about a UI thread, request context, or test framework behavior.

### App code
- Prefer the style that fits the app model.
- UI code often needs the original context after `await`.
- ASP.NET Core request code normally does not need `Task.Run` just to stay responsive, because it already runs on thread pool threads.
- Do not present “ASP.NET Core has no synchronization context” as proof that every `ConfigureAwait(false)` discussion is obsolete.

## `Task.Run` boundaries

### Good uses
- Offload CPU-bound work so a UI thread can stay responsive.
- Offload CPU work from a caller when that scheduling boundary is deliberate.

### Weak uses
- Wrapping synchronous I/O to pretend it is true async I/O.
- Calling `Task.Run` and immediately awaiting it in ASP.NET Core request handling when no CPU offload goal exists.
- Using `Task.Run` to hide blocking APIs instead of fixing the underlying API choice.

## Fire-and-forget

### Assume unsafe until proven otherwise
A background task needs answers for all of these:
- Who owns its lifetime?
- How are exceptions observed?
- How does shutdown cancel it?
- Does it touch scoped services or request-bound objects?
- Does work need retries, backpressure, or queueing?

### Safer alternatives
- Await the task normally.
- Queue work to an owned background component.
- In ASP.NET Core, prefer hosted services or a dedicated background queue pattern for long-lived work.
- If scoped services are required in background processing, create an explicit scope instead of capturing request scope objects.

## `ConfigureAwait`

### Strong recommendation
- In general-purpose libraries, use `ConfigureAwait(false)` unless the continuation must run in the captured context.

### Weak recommendation
- “Always use it in app code.”
- “Never use it on .NET Core.”
- “Use it once at the first await and you are done.”

### Review note
If code after the `await` needs a specific context, say so explicitly. If it does not, the recommendation depends on whether the code is app-level or general-purpose library code.

## Performance guidance

### Correctness first
Do not trade API clarity for speculative micro-optimizations.

### `ValueTask` is performance-specialized
Recommend it only when most of these are true:
1. the method is called very frequently
2. it often completes synchronously or from a reusable source
3. allocation reduction matters on measurements
4. consumers can respect single-consumer semantics
5. task combinator ergonomics are not central to the API

### Throttling and concurrency control
- `Task.WhenAll` expresses concurrency; it does not limit it.
- For bounded concurrency, use an async gate such as `SemaphoreSlim.WaitAsync`, or platform helpers such as `Parallel.ForEachAsync` when the workload fits.
- Always define what happens to remaining work after the first completion or first failure.
