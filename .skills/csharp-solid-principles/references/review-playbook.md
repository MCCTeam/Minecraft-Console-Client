# SOLID review playbook for C#/.NET

Use this file when performing a design/code review or planning a refactor.

## 1. Establish context before judging structure

Ask or infer:
- Is this domain code, application orchestration, infrastructure, UI/API, or a reusable library?
- Which .NET version/framework constraints matter?
- Is the code stable, or changing frequently?
- Is backward compatibility required?
- What changes have historically been painful?
- Which tests already protect behavior?

SOLID findings are more useful when tied to actual change and contract pressure.

## 2. Find change axes

Typical independent change axes in .NET systems:
- pricing/tax/discount rules;
- provider integrations (payments, email, storage, queues);
- persistence technology;
- serialization/transport;
- jurisdiction/tenant/customer policy;
- workflow steps;
- reporting/formatting;
- authorization capabilities;
- clock/randomness/external state.

A good abstraction usually corresponds to one meaningful axis, not an arbitrary class boundary.

## 3. SRP diagnostic questions

- Which actor or policy causes this type to change?
- Are business rules mixed with SQL/HTTP/files/email/log formatting?
- Do unrelated tests fail when one concern changes?
- Does the constructor have many dependencies from unrelated domains?
- Is the class merely orchestrating several cohesive collaborators? If yes, that can still be one responsibility.

Refactor when responsibilities have independent reasons to change. Do not split by line count alone.

## 4. OCP diagnostic questions

- What new variant was added the last three times this code changed?
- Is the same conditional on that variant repeated in multiple places?
- Is the set open-ended or closed by business definition?
- Would a strategy/handler/decorator make adding the next variant local?
- Would polymorphism make control flow harder to discover than a simple exhaustive switch?

Use extension points for proven variation. Prefer a switch for genuinely closed sets when it is the clearest model.

## 5. LSP diagnostic questions

For every implementation of an interface/base type:
- Does it accept every input the contract says is valid?
- Does it preserve promised outputs and nullability?
- Does it preserve required invariants?
- Does it throw surprising exceptions for supported operations?
- Does it silently ignore required work?
- Does it honor cancellation and async semantics?
- Does it preserve idempotency/ordering guarantees if callers rely on them?

A method signature is not a full contract. Document important behavior in XML docs, tests, or domain types.

## 6. ISP diagnostic questions

- Which members does each consumer actually use?
- Are read-only clients coupled to write/admin methods?
- Do implementations throw `NotSupportedException`?
- Are mocks mostly irrelevant setup?
- Can capability roles be named clearly?

Prefer role interfaces, not arbitrary one-method fragmentation.

## 7. DIP diagnostic questions

- Does Core/Application reference Infrastructure?
- Does business code `new` up infrastructure with side effects?
- Are static globals or service location hiding dependencies?
- Are interfaces named after higher-level capabilities or lower-level technology?
- Is the composition root the place where concrete implementations are selected?
- Are service lifetimes valid?

Remember: `new` is normal for entities, value objects, collections, DTOs, and pure helpers. The concern is direct coupling to replaceable/volatile lower-level details.

## 8. Rank findings

Use this priority order:
1. correctness / contract violations (especially LSP and DI lifetime bugs);
2. changes that require edits across many stable modules;
3. hidden dependencies and infrastructure coupling;
4. responsibilities that routinely change independently;
5. oversized client contracts;
6. stylistic or speculative improvements.

## 9. Recommend the smallest coherent change

Examples:
- extract one pricing policy before introducing a full rules engine;
- split one reader interface from a broad repository before redesigning persistence;
- inject a single external capability before layering an entire architecture;
- replace one invalid inheritance relationship with composition;
- move service resolution to the composition root instead of introducing another container wrapper.

## 10. Protect the refactor with tests

Useful tests by principle:

### SRP
- characterization test for the use case;
- focused tests for extracted policies.

### OCP
- test each strategy/handler independently;
- integration test that registration discovers/selects expected implementations.

### LSP
- contract test suite run against every implementation;
- tests for nullability, exceptions, cancellation, invariants, idempotency, and boundary values.

### ISP
- compile-time dependency reduction is the main benefit;
- targeted tests for each role implementation.

### DIP
- unit tests for policy using fakes/stubs where useful;
- integration tests for infrastructure adapters;
- application-startup test validating DI registrations and scopes.

## 11. Contract-test pattern for LSP

A shared test suite can verify behavior across implementations:

```csharp
public abstract class CustomerStoreContractTests
{
    protected abstract ICustomerStore CreateStore();

    [Fact]
    public async Task Saved_customer_can_be_loaded()
    {
        var store = CreateStore();
        var customer = Customer.Create("alice@example.com");

        await store.SaveAsync(customer, CancellationToken.None);
        var loaded = await store.GetAsync(customer.Id, CancellationToken.None);

        Assert.Equal(customer, loaded);
    }
}
```

Run the same contract tests against SQL, in-memory, or other implementations that claim the same contract.

## 12. Common false positives

- **“A switch violates OCP.”** Not necessarily. Closed-domain switches are often excellent.
- **“One dependency per class.”** Not an SRP rule.
- **“Every class needs an interface.”** Not DIP.
- **“Inheritance is always bad.”** No; it is bad when behavioral substitutability fails or composition is clearer.
- **“Tiny interfaces are always better.”** ISP is client-driven, not member-count-driven.
- **“DI means DIP is satisfied.”** A DI container can wire a badly inverted architecture.
- **“SOLID means layered architecture.”** SOLID can inform many architectures; it does not prescribe one topology.

## 13. Review language

Prefer:
- “This class has two independent reasons to change: tax policy and SMTP delivery.”
- “This abstraction promises writable storage, but this implementation cannot honor it.”
- “Adding a new provider requires editing three stable switches; a strategy boundary would localize the change.”

Avoid:
- “This violates SOLID because it has 300 lines.”
- “You need an interface here because interfaces are best practice.”
- “Never use `switch`.”
