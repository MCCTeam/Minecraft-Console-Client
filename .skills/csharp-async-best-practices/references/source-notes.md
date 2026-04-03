---
description: >-
  Authority notes and citations for the c# async best practices skill, separating
  official documentation, expert interpretation, and synthesized guidance.
metadata:
  tags: [sources, citations, authority, notes]
  source: external
---

# Source Notes

## Official facts

- Microsoft Learn, "Implementing the Task-based Asynchronous Pattern"
  - https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-task-based-asynchronous-pattern
  - Return types, cancellation behavior, `Task.Run` boundaries, and TAP implementation guidance.
- Microsoft Learn, "Consuming the Task-based Asynchronous Pattern"
  - https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/consuming-the-task-based-asynchronous-pattern
  - `await`, `WhenAll`, `WhenAny`, cancellation propagation, and exception behavior.
- Microsoft Learn, "Async return types"
  - https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-return-types
  - `Task`, `Task<T>`, `async void`, generalized async return types.
- Microsoft Learn, `ValueTask` API reference
  - https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask
  - single-consumer warnings and default-to-`Task` guidance.
- Microsoft Learn, ASP.NET Core best practices
  - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices
  - avoid blocking calls, avoid unnecessary `Task.Run`, background-work cautions.
- Microsoft Learn, hosted services in ASP.NET Core
  - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
  - safe long-lived background work and cancellation during shutdown.

## Expert guidance used only when technically grounded

- Stephen Toub, ".NET Blog: ConfigureAwait FAQ"
  - https://devblogs.microsoft.com/dotnet/configureawait-faq/
  - best source for context capture semantics and library-vs-app guidance.
- Stephen Toub, ".NET Blog: Understanding the Whys, Whats, and Whens of ValueTask"
  - https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
  - performance rationale and tradeoffs behind `ValueTask<T>`.
- Stephen Toub, ".NET Blog: Await, and UI, and deadlocks! Oh my!"
  - https://devblogs.microsoft.com/dotnet/await-and-ui-and-deadlocks-oh-my/
  - canonical deadlock explanation for context-bound code.
- Stephen Toub, ".NET Blog: Task Exception Handling in .NET 4.5"
  - https://devblogs.microsoft.com/dotnet/task-exception-handling-in-net-4-5/
  - explains `await` versus blocking exception shape and why `WhenAll` matters.
- Andrew Arnott, "Recommended patterns for CancellationToken"
  - https://devblogs.microsoft.com/premier-developer/recommended-patterns-for-cancellationtoken/
  - practical cancellation design heuristics; useful, but not treated as a language/runtime spec.
- Stephen Cleary, "Async/Await - Best Practices in Asynchronous Programming"
  - https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming
  - useful design interpretation, but older and treated as contextual guidance rather than current official policy.

## Where the skill is intentionally cautious

- `ConfigureAwait`: strong guidance exists for libraries, weaker guidance for app code. Blanket rules are rejected.
- `Task.Run`: valid for deliberate CPU offload, weak as a server-side patch for blocking I/O.
- `ValueTask`: supported and useful, but easy to misuse. The skill defaults to `Task` unless evidence is present.
- Fire-and-forget: acceptable only with explicit ownership and lifecycle design, especially in server code.
