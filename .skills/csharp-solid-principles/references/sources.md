# Sources and interpretation notes

These sources ground the skill. Prefer primary/official material for definitions and framework behavior, then use expert commentary for interpretation and practical tradeoffs.

## Microsoft / official .NET

### Architectural principles — .NET
URL: https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles

Why it matters:
- describes dependency inversion as dependencies pointing toward abstractions rather than implementation details;
- connects DIP to loose coupling, testability, modularity, and dependency injection;
- describes explicit dependencies via constructors;
- states SRP as one responsibility / one reason to change;
- emphasizes separation of concerns and warns against coupling to the wrong abstraction.

Use for: SRP, DIP, explicit dependencies, non-dogmatic separation of concerns.

### Dependency injection guidelines — .NET
URL: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines

Why it matters:
- advises against direct instantiation of dependent classes inside services;
- recommends small, well-factored, testable services;
- notes that many injected dependencies can signal too many responsibilities;
- warns against service locator and static service access;
- documents disposal and lifetime concerns.

Use for: DIP/DI implementation guidance and .NET-specific code review.

### Service lifetimes — .NET
URL: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes

Why it matters:
- defines transient, scoped, and singleton lifetimes;
- warns against injecting scoped services into singletons without an explicit scope;
- notes singleton services must be thread-safe.

Use for: validating DI refactors.

### Interfaces — C# language specification
URL: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/interfaces

Why it matters:
- defines an interface as a contract that implementing classes/structs must adhere to.

Use for: LSP/ISP discussions about contract semantics.

### Options pattern — .NET
URL: https://learn.microsoft.com/en-us/dotnet/core/extensions/options

Why it matters:
- explicitly connects isolated configuration classes to the Interface Segregation Principle: consumers depend only on settings they use.

Use for: a Microsoft-backed example of ISP in practical .NET design.

### C# Best Practices: Dangers of Violating SOLID Principles in C# — Microsoft archive
URL: https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp

Why it matters:
- discusses all five SOLID principles with C#-specific examples;
- describes LSP in terms of safe substitution and ISP in terms of focused interface purpose.

Note: archived MSDN Magazine content; useful as historical Microsoft-published guidance, but framework-specific details should defer to current docs.

### Developing ASP.NET Core MVC apps — Working with dependencies
URL: https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps

Why it matters:
- connects dependency injection to loose coupling, testability, DIP, and OCP;
- warns about “static cling” and infrastructure side effects hidden behind static access.

Use for: ASP.NET Core architecture reviews.

## .NET ecosystem experts

### Mark Seemann — Dependency inversion without inversion of control
URL: https://blog.ploeh.dk/2025/01/27/dependency-inversion-without-inversion-of-control/

Why it matters:
- argues clearly that DIP, IoC, and DI overlap but are not the same concept.

Use for: correcting the common “DIP = DI” misconception.

### Mark Seemann — SOLID or COLDS?
URL: https://blog.ploeh.dk/2009/09/29/SOLIDorCOLDS/

Why it matters:
- offers the useful expert interpretation that ISP is closely related to SRP applied at the interface/API level;
- emphasizes that APIs/contracts matter more than implementation details to consumers.

Use for: explaining the relationship between SRP and ISP. Treat this as expert opinion, not a normative definition.

### Mark Seemann — SOLID concrete
URL: https://blog.ploeh.dk/2011/10/25/SOLIDconcrete/

Why it matters:
- discusses role interfaces and the idea that abstractions can be general while concrete implementations remain specific.

Use for: ISP/DIP abstraction design.

### Steve Smith (Ardalis) — Creating a SOLID Visual Studio Solution
URL: https://ardalis.com/creating-solid-vs-solution/

Why it matters:
- provides .NET-specific guidance on making infrastructure depend on Core abstractions rather than business logic depend on Data/Infrastructure;
- demonstrates a composition-root-oriented solution structure.

Use for: project/reference direction and DIP.

### Steve Smith (Ardalis) — What are Abstractions in Software Development
URL: https://ardalis.com/what-are-abstractions-in-software-development/

Why it matters:
- emphasizes that good abstractions should be stable and that frequently changing abstractions may indicate a poor boundary;
- ties abstraction design to OCP and dependency stability.

Use for: avoiding speculative or unstable interfaces.

## Primary / foundational sources

### Barbara Liskov and Jeannette Wing — A Behavioral Notion of Subtyping
URL: https://www.cs.cmu.edu/~svc/papers/view-publications-lw94.html Full paper listing: https://www.cs.cmu.edu/~wing/publications/LiskovWing94.pdf

Why it matters:
- foundational formulation of behavioral subtyping;
- the key idea is that properties guaranteed for supertype objects should remain true for subtype objects.

Use for: LSP as a behavioral contract, including invariants and expectations beyond signatures.

### Robert C. Martin — Principles and Patterns
URL: https://objectmentor.com/resources/articles/Principles_and_Patterns.pdf

Why it matters:
- historical primary source discussing object-oriented principles and dependency management;
- presents OCP as extension without modification and discusses other principles that became associated with SOLID.

Use for: historical grounding. Prefer current Microsoft docs for .NET framework behavior.

## How to reconcile sources

- Treat Microsoft documentation as authoritative for current .NET runtime/framework behavior.
- Treat Liskov/Wing as primary for behavioral subtyping theory.
- Treat Robert C. Martin as foundational SOLID design literature.
- Treat Seemann and Smith as expert interpretations from the .NET ecosystem, especially useful for practical dependency and abstraction tradeoffs.
- When sources differ in emphasis, preserve the shared intent: manage change, keep contracts honest, and reduce harmful coupling. Avoid turning any source into rigid “one true architecture” rules.
