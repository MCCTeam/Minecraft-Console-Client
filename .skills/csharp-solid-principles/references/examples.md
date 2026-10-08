# Realistic C# SOLID examples — Bad vs Good

Use these examples as patterns, not templates to copy blindly. Adapt names, async behavior, error handling, and DI registration to the user's domain.

## S — Single Responsibility Principle

### Scenario
An order checkout use case must calculate a total, charge a payment provider, persist the order, and send a receipt.

### Bad — one class owns unrelated policies and infrastructure

```csharp
public sealed class CheckoutService
{
    public async Task<Guid> CheckoutAsync(
        Cart cart,
        string cardToken,
        CancellationToken cancellationToken)
    {
        // Pricing policy
        var subtotal = cart.Items.Sum(x => x.UnitPrice * x.Quantity);
        var discount = subtotal >= 500m ? subtotal * 0.10m : 0m;
        var total = subtotal - discount;

        // Payment infrastructure
        using var http = new HttpClient();
        var response = await http.PostAsJsonAsync(
            "https://payments.example/charges",
            new { cardToken, amount = total },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        // Persistence infrastructure
        await using var connection = new SqlConnection(
            Environment.GetEnvironmentVariable("ORDERS_DB"));
        await connection.OpenAsync(cancellationToken);
        // INSERT order ...

        // Notification infrastructure
        using var smtp = new SmtpClient("smtp.example");
        smtp.Send("sales@example.com", cart.Email, "Receipt", $"Paid {total:C}");

        return Guid.NewGuid();
    }
}
```

Why this hurts:
- pricing rules, payment-provider changes, database changes, and email changes all modify the same class;
- focused tests require infrastructure seams for unrelated concerns;
- an operational change can accidentally disturb business policy.

### Good — cohesive orchestration with focused collaborators

```csharp
public interface IOrderPricing
{
    Money Calculate(Cart cart);
}

public interface IPaymentGateway
{
    Task<PaymentReceipt> ChargeAsync(
        string paymentToken,
        Money amount,
        CancellationToken cancellationToken);
}

public interface IOrderRepository
{
    Task SaveAsync(Order order, CancellationToken cancellationToken);
}

public interface IReceiptSender
{
    Task SendAsync(Order order, CancellationToken cancellationToken);
}

public sealed class CheckoutService
{
    private readonly IOrderPricing _pricing;
    private readonly IPaymentGateway _payments;
    private readonly IOrderRepository _orders;
    private readonly IReceiptSender _receipts;

    public CheckoutService(
        IOrderPricing pricing,
        IPaymentGateway payments,
        IOrderRepository orders,
        IReceiptSender receipts)
    {
        _pricing = pricing;
        _payments = payments;
        _orders = orders;
        _receipts = receipts;
    }

    public async Task<Guid> CheckoutAsync(
        Cart cart,
        string paymentToken,
        CancellationToken cancellationToken)
    {
        var total = _pricing.Calculate(cart);
        var payment = await _payments.ChargeAsync(
            paymentToken, total, cancellationToken);

        var order = Order.Create(cart, total, payment);
        await _orders.SaveAsync(order, cancellationToken);
        await _receipts.SendAsync(order, cancellationToken);

        return order.Id;
    }
}
```

What improved:
- the application service owns the checkout workflow;
- pricing, payment, persistence, and receipt delivery can change independently;
- tests can verify orchestration without exercising HTTP/SQL/SMTP.

Caveat: four dependencies are not automatically “too many.” They are reasonable if they all participate in one coherent checkout workflow. Split further only if the workflow itself contains independent responsibilities.

---

## O — Open/Closed Principle

### Scenario
Shipping cost depends on shipping service, and new carriers are added regularly.

### Bad — a central calculator changes for every new carrier

```csharp
public enum ShippingMethod
{
    Standard,
    Express,
    SameDay
}

public sealed class ShippingCalculator
{
    public decimal Calculate(ShippingMethod method, Parcel parcel)
    {
        return method switch
        {
            ShippingMethod.Standard => 4.99m + parcel.WeightKg * 0.50m,
            ShippingMethod.Express  => 9.99m + parcel.WeightKg * 0.90m,
            ShippingMethod.SameDay  => 19.99m + parcel.WeightKg * 1.25m,
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }
}
```

This is not inherently bad. It becomes an OCP pressure point when shipping methods are an **open, recurring extension axis** and this switch (or similar switches) must be edited repeatedly across the system.

### Good — extension through carrier policies

```csharp
public interface IShippingRatePolicy
{
    string Method { get; }
    decimal Calculate(Parcel parcel);
}

public sealed class StandardShippingRate : IShippingRatePolicy
{
    public string Method => "standard";
    public decimal Calculate(Parcel parcel) => 4.99m + parcel.WeightKg * 0.50m;
}

public sealed class ExpressShippingRate : IShippingRatePolicy
{
    public string Method => "express";
    public decimal Calculate(Parcel parcel) => 9.99m + parcel.WeightKg * 0.90m;
}

public sealed class ShippingCalculator
{
    private readonly IReadOnlyDictionary<string, IShippingRatePolicy> _policies;

    public ShippingCalculator(IEnumerable<IShippingRatePolicy> policies)
    {
        _policies = policies.ToDictionary(
            x => x.Method,
            StringComparer.OrdinalIgnoreCase);
    }

    public decimal Calculate(string method, Parcel parcel)
    {
        if (!_policies.TryGetValue(method, out var policy))
        {
            throw new NotSupportedException($"Shipping method '{method}' is not supported.");
        }

        return policy.Calculate(parcel);
    }
}
```

Composition root:

```csharp
services.AddSingleton<IShippingRatePolicy, StandardShippingRate>();
services.AddSingleton<IShippingRatePolicy, ExpressShippingRate>();
services.AddSingleton<ShippingCalculator>();
```

Now a new carrier policy can normally be added as a new class plus registration rather than modifying stable calculation logic.

Caveat: if the shipping domain is closed and tiny, the exhaustive enum switch may be clearer and safer. OCP is about protecting proven variation points, not eliminating all conditionals.

---

## L — Liskov Substitution Principle

### Scenario
A payment abstraction promises to process any validated payment request that meets the documented contract.

### Bad — one implementation secretly strengthens the precondition

```csharp
public sealed record PaymentRequest(
    decimal Amount,
    string Currency,
    string CountryCode);

public interface IPaymentProcessor
{
    // Contract: process any validated request for a supported currency.
    Task<PaymentResult> ProcessAsync(
        PaymentRequest request,
        CancellationToken cancellationToken);
}

public sealed class StandardPaymentProcessor : IPaymentProcessor
{
    public Task<PaymentResult> ProcessAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        return SendToProviderAsync(request, cancellationToken);
    }
}

public sealed class DomesticOnlyPaymentProcessor : IPaymentProcessor
{
    public Task<PaymentResult> ProcessAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.CountryCode, "US", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("International payments are not allowed.");
        }

        return SendToDomesticProviderAsync(request, cancellationToken);
    }
}
```

A caller written against `IPaymentProcessor` cannot safely substitute the domestic-only implementation because that implementation rejects valid requests allowed by the abstraction.

### Good — make the restriction explicit outside the processor contract

```csharp
public interface IPaymentEligibilityPolicy
{
    PaymentEligibility Check(PaymentRequest request);
}

public sealed class DomesticOnlyEligibilityPolicy : IPaymentEligibilityPolicy
{
    public PaymentEligibility Check(PaymentRequest request)
    {
        return string.Equals(request.CountryCode, "US", StringComparison.OrdinalIgnoreCase)
            ? PaymentEligibility.Allowed()
            : PaymentEligibility.Rejected("Only domestic payments are supported.");
    }
}

public sealed class PaymentService
{
    private readonly IPaymentEligibilityPolicy _eligibility;
    private readonly IPaymentProcessor _processor;

    public PaymentService(
        IPaymentEligibilityPolicy eligibility,
        IPaymentProcessor processor)
    {
        _eligibility = eligibility;
        _processor = processor;
    }

    public async Task<PaymentResult> PayAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        var eligibility = _eligibility.Check(request);
        if (!eligibility.IsAllowed)
        {
            return PaymentResult.Rejected(eligibility.Reason!);
        }

        return await _processor.ProcessAsync(request, cancellationToken);
    }
}
```

Now every `IPaymentProcessor` can honor one consistent processing contract, while market restrictions are modeled as policy.

Other common LSP failures in C#:
- an implementation returns `null` where the interface contract and nullable annotations promise non-null;
- a subtype ignores `CancellationToken` even though callers rely on prompt cancellation;
- a “read-only” implementation throws `NotSupportedException` for write members required by a broad interface;
- a derived class mutates state so base-class invariants no longer hold;
- an implementation silently no-ops where callers expect a durable side effect.

Caveat: different implementations may have different performance, logging, or internal algorithms. LSP concerns observable contract compatibility, not identical internals.

---

## I — Interface Segregation Principle

### Scenario
An order system has read paths, write paths, and administrative exports.

### Bad — every client depends on one oversized interface

```csharp
public interface IOrderService
{
    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> SearchAsync(
        OrderQuery query,
        CancellationToken cancellationToken);
    Task SaveAsync(Order order, CancellationToken cancellationToken);
    Task CancelAsync(Guid id, CancellationToken cancellationToken);
    Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken);
    Task<byte[]> ExportAllToCsvAsync(CancellationToken cancellationToken);
}

public sealed class OrderLookupEndpoint
{
    private readonly IOrderService _orders;

    public OrderLookupEndpoint(IOrderService orders) => _orders = orders;

    public Task<Order?> HandleAsync(Guid id, CancellationToken cancellationToken)
        => _orders.GetAsync(id, cancellationToken);
}
```

The lookup endpoint depends on write, delete, and export capabilities it neither needs nor should conceptually possess.

### Good — client-oriented role interfaces

```csharp
public interface IOrderReader
{
    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> SearchAsync(
        OrderQuery query,
        CancellationToken cancellationToken);
}

public interface IOrderWriter
{
    Task SaveAsync(Order order, CancellationToken cancellationToken);
    Task CancelAsync(Guid id, CancellationToken cancellationToken);
}

public interface IOrderAdministration
{
    Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken);
    Task<byte[]> ExportAllToCsvAsync(CancellationToken cancellationToken);
}

public sealed class SqlOrderStore :
    IOrderReader,
    IOrderWriter,
    IOrderAdministration
{
    // One implementation may support several coherent roles.
}

public sealed class OrderLookupEndpoint
{
    private readonly IOrderReader _orders;

    public OrderLookupEndpoint(IOrderReader orders) => _orders = orders;

    public Task<Order?> HandleAsync(Guid id, CancellationToken cancellationToken)
        => _orders.GetAsync(id, cancellationToken);
}
```

Benefits:
- consumers see only capabilities they need;
- authorization and architecture become easier to reason about;
- tests mock smaller contracts;
- specialized implementations need not fake unrelated members.

Caveat: do not create `IGetOrder`, `ISearchOrder`, `ISaveOrder`, etc. solely to minimize method count. Group members by coherent client role and change cadence.

---

## D — Dependency Inversion Principle

### Scenario
An invoice application service creates invoices, persists them, emails the customer, and timestamps the operation.

### Bad — high-level policy depends on infrastructure details

```csharp
public sealed class InvoiceService
{
    public async Task<Invoice> CreateAsync(
        Customer customer,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            customer.Id,
            amount,
            DateTimeOffset.UtcNow);

        await using var connection = new SqlConnection(
            Environment.GetEnvironmentVariable("BILLING_DB"));
        await connection.OpenAsync(cancellationToken);
        // INSERT invoice ...

        using var smtp = new SmtpClient("smtp.example");
        smtp.Send("billing@example.com", customer.Email, "Invoice", invoice.Id.ToString());

        return invoice;
    }
}
```

The high-level use case knows SQL, environment configuration, SMTP, and the system clock.

### Good — policy depends on capabilities it owns

```csharp
public interface IInvoiceRepository
{
    Task SaveAsync(Invoice invoice, CancellationToken cancellationToken);
}

public interface IInvoiceNotifier
{
    Task SendCreatedAsync(
        Invoice invoice,
        Customer customer,
        CancellationToken cancellationToken);
}

public sealed class InvoiceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IInvoiceNotifier _notifier;
    private readonly TimeProvider _timeProvider;

    public InvoiceService(
        IInvoiceRepository invoices,
        IInvoiceNotifier notifier,
        TimeProvider timeProvider)
    {
        _invoices = invoices;
        _notifier = notifier;
        _timeProvider = timeProvider;
    }

    public async Task<Invoice> CreateAsync(
        Customer customer,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            customer.Id,
            amount,
            _timeProvider.GetUtcNow());

        await _invoices.SaveAsync(invoice, cancellationToken);
        await _notifier.SendCreatedAsync(invoice, customer, cancellationToken);

        return invoice;
    }
}
```

Infrastructure implements those contracts:

```csharp
public sealed class SqlInvoiceRepository : IInvoiceRepository
{
    // SQL-specific implementation belongs in Infrastructure.
}

public sealed class SmtpInvoiceNotifier : IInvoiceNotifier
{
    // SMTP-specific implementation belongs in Infrastructure.
}
```

Composition root:

```csharp
services.AddScoped<IInvoiceRepository, SqlInvoiceRepository>();
services.AddScoped<IInvoiceNotifier, SmtpInvoiceNotifier>();
services.AddSingleton(TimeProvider.System);
services.AddScoped<InvoiceService>();
```

Dependency direction is now policy -> abstraction, while infrastructure -> abstraction. Runtime calls still flow to concrete implementations through composition.

### Bad DI even when interfaces exist — service locator

```csharp
public sealed class RenewalService
{
    private readonly IServiceProvider _services;

    public RenewalService(IServiceProvider services) => _services = services;

    public async Task RenewAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var repository = _services.GetRequiredService<ISubscriptionRepository>();
        var billing = _services.GetRequiredService<IBillingGateway>();
        // ...
    }
}
```

This hides required dependencies and mixes business behavior with object-graph resolution.

Prefer explicit constructor dependencies:

```csharp
public sealed class RenewalService
{
    private readonly ISubscriptionRepository _repository;
    private readonly IBillingGateway _billing;

    public RenewalService(
        ISubscriptionRepository repository,
        IBillingGateway billing)
    {
        _repository = repository;
        _billing = billing;
    }
}
```

Caveat: DIP does not mean “everything needs an interface.” Concrete value objects, pure domain services, and stable framework types can be perfectly reasonable direct dependencies. Invert dependencies at architectural and volatility boundaries that matter.

---

# Cross-principle example: ASP.NET Core service lifetime correctness

SOLID advice must not ignore .NET runtime semantics.

### Bad — singleton captures a scoped `DbContext`

```csharp
services.AddDbContext<AppDbContext>();           // Scoped by default
services.AddSingleton<ReportCache>();

public sealed class ReportCache
{
    private readonly AppDbContext _db;

    public ReportCache(AppDbContext db) => _db = db;
}
```

This is a captive dependency: the singleton outlives the scoped context. It can cause state leakage and correctness problems.

### Better

If the cache itself does not need to be singleton, make it scoped:

```csharp
services.AddScoped<ReportCache>();
```

If singleton semantics are truly required, redesign the boundary so scoped work is performed within an explicit scope or through an appropriate factory rather than capturing the scoped instance.

Do not let a SOLID refactor introduce invalid DI lifetimes.
