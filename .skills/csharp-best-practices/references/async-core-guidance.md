---
description: >-
  Source-backed core guidance for task, valuetask, cancellation, exception flow,
  blocking, and concurrency in c# async code reviews and implementations.
metadata:
  tags: [csharp, async, task, valuetask, cancellation, exceptions, concurrency]
  source: mixed
---

# Core Guidance

## Facts from official .NET documentation

### 1. Return types and `async void`
- Async methods should normally return `Task` or `Task<T>`.
- `async void` is intended for event handlers; callers cannot await it and exception handling differs.
- TAP methods that return awaitable types conventionally use the `Async` suffix.

### 2. Blocking on async
- `Task<T>.Result` is blocking. Prefer `await` in most cases.
- Blocking can deadlock in context-bound environments and reduces scalability even when it does not deadlock.
- `await` on a faulted task rethrows one exception directly; `.Wait()` and `.Result` wrap failures in `AggregateException`.

### 3. `Task` versus `ValueTask`
- Default to `Task` or `Task<T>` unless there is a demonstrated reason not to.
- `ValueTask` has stricter usage rules. A given instance should generally be awaited only once.
- Do not await the same `ValueTask` multiple times, call `AsTask()` multiple times, or mix consumption techniques on the same instance.
- For synchronously successful `Task`-returning methods, `Task.CompletedTask` is the normal zero-result completion value.

### 4. Cancellation
- If a TAP method supports cancellation, expose a `CancellationToken`.
- Pass the token to nested operations that should participate in cancellation.
- If an async method throws `OperationCanceledException` associated with the method’s token, the returned task transitions to `Canceled`.
- After a method has completed its work successfully, do not report cancellation instead of success.

### 5. Exception flow and task combinators
- `Task.WhenAll` does not block the calling thread.
- If any supplied task faults, the `WhenAll` task faults and aggregates the unwrapped exceptions from the component tasks.
- If none fault and at least one is canceled, the `WhenAll` task is canceled.
- `Task.WhenAny` returns a task that completes successfully with the first completed task as its result, even when that winning task itself is faulted or canceled.
- After `WhenAny`, await the returned winner task to propagate its outcome.
- The remaining tasks continue unless you cancel or otherwise handle them.

## Expert guidance that is strong and technically grounded

### Stephen Toub
- Use `ConfigureAwait(false)` as the general default for general-purpose library code, because library code should not depend on an app model’s context.
- App-level code is different. UI code often needs the captured context. ASP.NET Core also changes the deadlock discussion because it does not install the classic ASP.NET style synchronization context, but that does not make blanket `ConfigureAwait` advice strong.
- `ValueTask<T>` exists mainly to avoid allocations on frequently synchronous success paths. It is not a general replacement for `Task<T>` because `Task` is more flexible for multiple awaits, caching, and combinators.

### Andrew Arnott
- Propagate the token until the point of no cancellation.
- Validate arguments before cancellation checks when argument validation should always run.
- Prefer catching `OperationCanceledException` rather than `TaskCanceledException` in general-purpose logic.
- Keep `CancellationToken` last in the parameter list; make it optional mainly on public APIs, not necessarily on internal methods.

### Stephen Cleary
- “Async all the way” is a strong design guideline, not an absolute law of physics. Sync bridges exist, but they are specialized boundary decisions, not a normal code review recommendation.
- `async void` and sync-over-async both create real observability and composition problems even when a sample appears to work.

## Naming and testability

### Naming
- TAP methods that return awaitable types conventionally use the `Async` suffix. Do not force renames when an interface, base class, or event pattern already dictates the name.

### Testability
- Favor awaitable APIs over hidden work so tests can await completion, assert faults, and drive cancellation deterministically.
- Prefer explicit background components, injected clocks, and owned queues over ad hoc fire-and-forget logic that tests cannot observe.

## Synthesis for agents

### Code review defaults
- Treat `.Result`, `.Wait()`, and `GetAwaiter().GetResult()` as likely defects unless the code is a deliberate sync boundary and the caller explicitly cannot be async.
- Prefer `Task`/`Task<T>` for API design. Require an explicit reason before recommending `ValueTask`.
- Require cancellation behavior to be coherent: accepted, propagated, and not silently dropped.
- Prefer `await Task.WhenAll(...)` for independent operations started before awaiting.
- Treat `Task.WhenAny(...)` as incomplete until the winner is awaited and losers are canceled, observed, or intentionally left running.

### Minimal examples

#### Avoid sync-over-async
```csharp
// bad
var user = client.GetUserAsync(id).Result;

// better
var user = await client.GetUserAsync(id);
```

#### Use `Task.WhenAll` for parallel I/O
```csharp
var userTask = repo.GetUserAsync(id, ct);
var ordersTask = repo.GetOrdersAsync(id, ct);
await Task.WhenAll(userTask, ordersTask);
return new Dashboard(await userTask, await ordersTask);
```

#### Be conservative with `ValueTask`
```csharp
// default
Task<Item?> GetAsync(string key, CancellationToken ct);

// specialized hot path only when justified
ValueTask<Item?> TryGetCachedAsync(string key);
```
