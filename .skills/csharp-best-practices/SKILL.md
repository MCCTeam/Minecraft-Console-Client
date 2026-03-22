---
name: csharp-best-practices
description: >
  C# 14 / .NET 10 coding conventions, idiomatic patterns, and performance best practices
  for the Minecraft Console Client codebase. Use when writing, reviewing, or modifying C# code.
version: 0.4.0
---

# C# 14 / .NET 10 Best Practices

Target: **.NET 10**, **C# 14**, nullable enabled.
Sources: [MS C# Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) · [.NET Runtime Style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) · [C# 14 Proposals](https://github.com/dotnet/csharplang/blob/main/Language-Version-History.md) · [C# 13 Docs](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13)

## Naming

| Element | Style | Example |
|---|---|---|
| Type, method, property, const, enum member | PascalCase | `PacketHandler`, `MaxRetries`, `GameMode.Survival` |
| Interface | `I` + PascalCase | `IChatBot` |
| Private instance field | `_camelCase` | `_handler` |
| Private static field | `s_camelCase` | `s_defaultTimeout` |
| Thread-static field | `t_camelCase` | `t_cachedBuffer` |
| Local, parameter | camelCase | `packetId` |
| Type parameter | `T` + PascalCase | `TResult` |
| Namespace | PascalCase | `MinecraftClient.Protocol` |
| Async methods | Suffix `Async` | `ConnectAsync()`, `ReadPacketAsync()` |

```csharp
// CORRECT: naming conventions
private readonly Dictionary<int, Entity> _entities = new();
private static readonly TimeSpan s_reconnectDelay = TimeSpan.FromSeconds(5);
public int PacketCount { get; private set; }
public async Task<bool> ConnectAsync(CancellationToken ct) { }
```

```csharp
// WRONG: naming violations
private Dictionary<int, Entity> entities = new();   // missing _
private static TimeSpan reconnectDelay;              // missing s_
public int packet_count { get; set; }                // snake_case
public async Task<bool> Connect(CancellationToken ct) { }  // missing Async suffix
```

## C# 14 Features

### Extension Members (C# 14)

Declare extension methods, properties, and operators inside `extension(...)` blocks. Replaces `this`-parameter pattern for new extensions.

```csharp
// CORRECT: extension property + method (C# 14)
public static class EntityExtensions
{
    extension(Entity entity)
    {
        public bool IsAlive => entity.Health > 0;
        public void Heal(int amount) => entity.Health = Math.Min(entity.Health + amount, 20);
    }
    extension<T>(IEnumerable<T> items)
    {
        public bool IsEmpty => !items.GetEnumerator().MoveNext();
    }
}
```

```csharp
// WRONG: classic extension method when C# 14 extension block is available
public static bool IsAlive(this Entity entity) => entity.Health > 0;
```

### `field` Keyword in Properties (C# 14)

Access the auto-generated backing field without declaring it. Mix auto and full accessors.

```csharp
// CORRECT: lazy init with field keyword
public string DisplayName => field ??= ComputeDisplayName();

// CORRECT: INotifyPropertyChanged pattern
public bool IsConnected
{
    get;
    set
    {
        if (field == value) return;
        field = value;
        OnPropertyChanged();
    }
}
```

```csharp
// WRONG: manual backing field when field keyword suffices
private string? _displayName;
public string DisplayName => _displayName ??= ComputeDisplayName();
```

### Null-Conditional Assignment (C# 14)

Assign through `?.` — RHS is only evaluated when receiver is non-null.

```csharp
// CORRECT: null-conditional assignment
player?.Health = 20;
connection?.OnDisconnect += HandleDisconnect;
inventory?[slot] = newItem;
```

```csharp
// WRONG: manual null check for simple assignment
if (player is not null)
    player.Health = 20;
```

### Simple Lambda Parameters with Modifiers (C# 14)

Omit types on lambda parameters while still applying modifiers.

```csharp
// CORRECT: modifiers without explicit types
TryParse<int> parse = (text, out result) => int.TryParse(text, out result);
ReadOnlySpan<int> data = [1, 2, 3];
ProcessSpan((scoped span) => span.Length);
```

```csharp
// WRONG: fully explicit types just for a modifier
TryParse<int> parse = (string text, out int result) => int.TryParse(text, out result);
```

### First-Class Span Types (C# 14)

Implicit conversions between `T[]`, `Span<T>`, and `ReadOnlySpan<T>` — no explicit cast needed. Extension methods on `ReadOnlySpan<T>` apply to arrays and spans automatically.

```csharp
// CORRECT: pass array where ReadOnlySpan<T> is expected (C# 14)
int[] data = [1, 2, 3];
bool found = data.StartsWith(1);  // ReadOnlySpan<int> extension resolved
ReadOnlySpan<byte> span = stackalloc byte[4];
```

### Unbound Generics in `nameof` (C# 14)

```csharp
// CORRECT: no need to pick a dummy type argument
string name = nameof(Dictionary<,>);  // "Dictionary"
string prop = nameof(List<>.Count);   // "Count"
```

```csharp
// WRONG: arbitrary type argument just to satisfy nameof
string name = nameof(Dictionary<object, object>);
```

### Partial Events and Constructors (C# 14)

Separate declaration from implementation for source-generator scenarios.

```csharp
// CORRECT: partial constructor for source-gen interop
partial class ServerConnection
{
    partial ServerConnection(string host, int port);
}
partial class ServerConnection
{
    partial ServerConnection(string host, int port) { /* generated */ }
}
```

### `#:` Ignored Directives (C# 14)

For file-based `dotnet run app.cs` programs — ignored by the compiler.

```csharp
#!/usr/bin/dotnet run
#:package System.CommandLine@2.0.0-*
Console.WriteLine("Hello");
```

## C# 13 Features

### `Lock` Object (C# 13)

Use `System.Threading.Lock` instead of `lock(obj)` on arbitrary objects.

```csharp
// CORRECT: dedicated Lock type
private readonly Lock _lock = new();
public void Enqueue(ChatMessage msg) { lock (_lock) _queue.Add(msg); }
```

```csharp
// WRONG: locking on an object reference
private readonly object _syncRoot = new();
lock (_syncRoot) { }
```

### `params` Collections (C# 13)

`params` now works with `ReadOnlySpan<T>`, `Span<T>`, `IEnumerable<T>`, and other collection types.

```csharp
// CORRECT: params span avoids array allocation
public void Log(params ReadOnlySpan<string> messages)
{
    foreach (var msg in messages) Console.WriteLine(msg);
}
```

### Partial Properties (C# 13)

```csharp
// CORRECT: partial property for source generators
partial class Config
{
    public partial string Host { get; set; }
}
partial class Config
{
    public partial string Host { get => _host; set => _host = value; }
    private string _host = "";
}
```

## C# 12 Features

### Primary Constructors

Use for simple parameter capture. Parameters are `camelCase`, mutable — assign to `readonly` fields when immutability matters.

```csharp
// CORRECT: primary constructor captures dependencies
public class ChatLogger(string logFilePath, bool appendMode) : ChatBot
{
    private readonly StreamWriter _writer = new(logFilePath, appendMode);
    public override void GetText(string text) => _writer.WriteLine(text);
}
```

```csharp
// WRONG: verbose constructor boilerplate for simple capture
public class ChatLogger : ChatBot
{
    private readonly StreamWriter _writer;
    public ChatLogger(string logFilePath, bool appendMode)
    {
        _writer = new StreamWriter(logFilePath, appendMode);
    }
    public override void GetText(string text) => _writer.WriteLine(text);
}
```

### Collection Expressions

Use `[...]` and `..` spread for arrays, lists, spans.

```csharp
// CORRECT: collection expressions (C# 12)
int[] ids = [1, 2, 3];
List<string> names = ["Steve", "Alex"];
ReadOnlySpan<byte> header = [0xFE, 0x01];           // no heap alloc
int[] combined = [..firstArray, ..secondArray, 42];
IReadOnlyList<string> empty = [];
```

```csharp
// WRONG: verbose initialization
int[] ids = new int[] { 1, 2, 3 };
var names = new List<string> { "Steve", "Alex" };
ReadOnlySpan<byte> header = new byte[] { 0xFE, 0x01 };  // allocates
var combined = firstArray.Concat(secondArray).Append(42).ToArray();
```

### Type Aliases

```csharp
// CORRECT: alias complex types for readability
using Coordinate = (int X, int Y, int Z);
using PacketMap = System.Collections.Generic.Dictionary<int, System.Action<byte[]>>;
```

### Default Lambda Parameters

```csharp
// CORRECT: C# 12
var greet = (string name, string prefix = "Player") => $"{prefix} {name}";
```

## Modern Syntax (C# 10–14)

### File-Scoped Namespaces

```csharp
// CORRECT: file-scoped namespace — one per file, less nesting
namespace MinecraftClient.ChatBots;

public class MyBot : ChatBot { }
```

```csharp
// WRONG: block-scoped namespace adds unnecessary nesting
namespace MinecraftClient.ChatBots
{
    public class MyBot : ChatBot { }
}
```

### Target-Typed `new`

Use when the type is obvious from the left-hand side.

```csharp
// CORRECT: target-typed new
private readonly Dictionary<string, int> _scores = new();
List<Entity> entities = new(capacity: 256);
```

```csharp
// WRONG: redundant type name
private readonly Dictionary<string, int> _scores = new Dictionary<string, int>();
```

### Pattern Matching

Prefer patterns over type-casting chains and complex boolean logic.

```csharp
// CORRECT: is-pattern with declaration and property patterns
if (entity is Player { Health: > 0 } player)
    SendMessage($"{player.Name} is alive");
```

```csharp
// WRONG: manual cast and multi-step check
if (entity is Player)
{
    var player = (Player)entity;
    if (player.Health > 0)
        SendMessage($"{player.Name} is alive");
}
```

```csharp
// CORRECT: switch expression
public string GetStatusLabel(GameMode mode) => mode switch
{
    GameMode.Survival  => "Survival",
    GameMode.Creative  => "Creative",
    GameMode.Adventure => "Adventure",
    GameMode.Spectator => "Spectator",
    _ => throw new ArgumentOutOfRangeException(nameof(mode))
};
```

```csharp
// WRONG: switch statement with returns
public string GetStatusLabel(GameMode mode)
{
    switch (mode)
    {
        case GameMode.Survival: return "Survival";
        case GameMode.Creative: return "Creative";
        default: throw new ArgumentOutOfRangeException(nameof(mode));
    }
}
```

```csharp
// CORRECT: property patterns for compound conditions
if (response is { StatusCode: >= 200 and < 300, Content.Length: > 0 })
    ProcessResponse(response);
```

```csharp
// WRONG: multiple chained conditions
if (response != null && response.StatusCode >= 200
    && response.StatusCode < 300 && response.Content != null
    && response.Content.Length > 0)
    ProcessResponse(response);
```

```csharp
// CORRECT: relational, logical, and list patterns
if (health is > 0 and <= 6) LogToConsole("Low health!");
if (args is [var command, var target, ..]) ProcessCommand(command, target);
```

### Raw String Literals

Use for JSON, regex, multi-line strings.

```csharp
// CORRECT: raw string literal
string json = """
    { "username": "Steve", "action": "connect" }
    """;
string pattern = """<\w+>""";
```

```csharp
// WRONG: escaped quotes
string json = "{ \"username\": \"Steve\", \"action\": \"connect\" }";
```

### Records

Use `record` for immutable data carriers and DTOs. Use `record struct` for small value types.

```csharp
// CORRECT: record for data carrier
public record PlayerInfo(string Name, Guid Uuid, GameMode Mode);
public record struct ChunkCoord(int X, int Z);
var updated = info with { Mode = GameMode.Creative };
```

```csharp
// WRONG: full class for a simple data carrier
public class PlayerInfo
{
    public string Name { get; set; } = string.Empty;
    public Guid Uuid { get; set; }
    public GameMode Mode { get; set; }
}
```

```csharp
// CORRECT: compact constructor for record validation
public record OrderItem(string ProductId, int Quantity, decimal UnitPrice)
{
    public OrderItem
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ProductId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(UnitPrice);
    }
}
```

### Required Members

```csharp
// CORRECT: required + init enforces initialization without constructor boilerplate
public class ServerConfig
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public string? Password { get; init; }
}
var config = new ServerConfig { Host = "mc.example.com", Port = 25565 };
```

## Nullable Reference Types

Project has nullable enabled. Follow these rules:

```csharp
// CORRECT: guard at API boundaries with .NET 8 throw helpers
public void Connect(string host, IProtocolHandler handler)
{
    ArgumentNullException.ThrowIfNull(handler);
    ArgumentException.ThrowIfNullOrEmpty(host);
}
```

```csharp
// WRONG: manual null checks
if (handler == null) throw new ArgumentNullException(nameof(handler));
if (string.IsNullOrWhiteSpace(host))
    throw new ArgumentException("Host is required", nameof(host));
```

```csharp
// CORRECT: 'is not null' pattern
if (currentPlayer is not null)
    currentPlayer.Update();
```

```csharp
// WRONG: comparison operator for null check
if (currentPlayer != null)
    currentPlayer.Update();
```

```csharp
// CORRECT: explicit nullable handling
public Entity? FindEntity(int id)
{
    return _entities.TryGetValue(id, out var entity) ? entity : null;
}

// CORRECT: null-coalescing / null-conditional
string name = player?.CustomName ?? player?.Name ?? "Unknown";

// CORRECT: null-forgiving only when proven safe (after ThrowIfNull or equivalent)
string val = GetRequiredValue()!;

// CORRECT: annotate return values
[return: MaybeNull]
public T Find<T>(Predicate<T> match) { }

[MemberNotNull(nameof(_connection))]
private void EnsureConnected() { }
```

```csharp
// WRONG: hiding nullability with null-forgiving
public string GetName(Player? player)
{
    return player!.Name;  // hides potential NullReferenceException
}
```

## Async / Await

```csharp
// CORRECT: propagate CancellationToken through every async I/O call
public async Task<string> FetchDataAsync(Uri uri, CancellationToken ct = default)
{
    using var response = await _httpClient.GetAsync(uri, ct);
    return await response.Content.ReadAsStringAsync(ct);
}
```

```csharp
// WRONG: CancellationToken not passed downstream
public async Task<string> FetchDataAsync(Uri uri)
{
    using var response = await _httpClient.GetAsync(uri, default);
    return await response.Content.ReadAsStringAsync(default);
}
```

```csharp
// CORRECT: ValueTask when result is often available synchronously
public ValueTask<int> GetCachedCountAsync()
{
    if (_cache.TryGetValue("count", out int count))
        return ValueTask.FromResult(count);
    return new ValueTask<int>(LoadCountFromDbAsync());
}
```

```csharp
// WRONG: Task allocates unnecessarily when result is cached
public async Task<int> GetCachedCountAsync()
{
    if (_cache.TryGetValue("count", out int count))
        return count;  // allocates a Task
    return await LoadCountFromDbAsync();
}
```

```csharp
// CORRECT: async Task for async event handlers
public async Task HandleEventAsync(GameEvent e, CancellationToken ct)
{
    await notificationService.SendAsync(e.PlayerId, ct);
}
```

```csharp
// WRONG: async void — exceptions are unobservable, cannot be awaited
public async void HandleEvent(GameEvent e)
{
    await notificationService.SendAsync(e.PlayerId, default);
}
```

```csharp
// CORRECT: await the result
var packet = await reader.ReadPacketAsync(ct);
```

```csharp
// WRONG: .Result / .Wait() causes deadlocks
var packet = reader.ReadPacketAsync(ct).Result;
var packet2 = reader.ReadPacketAsync(ct).GetAwaiter().GetResult();
```

```csharp
// CORRECT: ConfigureAwait(false) in library code
var data = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);

// CORRECT: IAsyncEnumerable for streaming
public async IAsyncEnumerable<ChatMessage> ReadChatStreamAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    while (!ct.IsCancellationRequested)
        yield return await _reader.ReadNextAsync(ct);
}

// CORRECT: await using for async disposal
await using var conn = new McConnection(host, port);
```

## LINQ

### Prefer Method Syntax for Most Operations

```csharp
// CORRECT: method syntax for common operations
var onlinePlayers = players
    .Where(p => p.IsOnline)
    .OrderBy(p => p.Name)
    .Select(p => new PlayerListItem(p.Id, p.Name))
    .ToList();
```

```csharp
// AVOID: query syntax for simple operations
var onlinePlayers = (
    from p in players
    where p.IsOnline
    orderby p.Name
    select new PlayerListItem(p.Id, p.Name)
).ToList();
```

### Use Query Syntax for Joins

```csharp
// CORRECT: query syntax makes joins readable
var results =
    from entity in entities
    join player in players on entity.OwnerId equals player.Id
    where entity.Health > 0
    select new { entity.Name, player.Name };
```

```csharp
// AVOID: method syntax for complex joins is hard to read
var results = entities
    .Join(players,
        e => e.OwnerId,
        p => p.Id,
        (e, p) => new { e, p })
    .Where(x => x.e.Health > 0)
    .Select(x => new { x.e.Name, PlayerName = x.p.Name });
```

### Materialize to Avoid Multiple Enumeration

```csharp
// CORRECT: materialize once, iterate many times
var online = players.Where(p => p.IsOnline).ToList();
Console.WriteLine(online.Count);
foreach (var p in online) { }
```

```csharp
// WRONG: enumerates the query twice
var filtered = players.Where(p => p.IsOnline);
Console.WriteLine(filtered.Count());     // first enumeration
foreach (var p in filtered) { }          // second enumeration
```

### Use Any() Over Count() > 0

```csharp
// CORRECT: short-circuits on first match
if (entities.Any(e => e.IsHostile))
    TriggerAlert();
```

```csharp
// WRONG: counts the entire collection
if (entities.Count(e => e.IsHostile) > 0)
    TriggerAlert();
```

### Prefer FirstOrDefault with Null Handling

```csharp
// CORRECT: explicit null handling
var target = players.FirstOrDefault(p => p.Name == name)
    ?? throw new InvalidOperationException($"Player '{name}' not found");
```

### TryGetNonEnumeratedCount

```csharp
// CORRECT: avoid full enumeration just to get count (.NET 6+)
if (source.TryGetNonEnumeratedCount(out int count))
    buffer = new Entity[count];
```

### Avoid LINQ in Hot Paths

```csharp
// CORRECT: manual loop with Span in performance-critical code
Span<byte> data = stackalloc byte[256];
int found = 0;
for (int i = 0; i < data.Length; i++)
    if (data[i] == target) found++;
```

```csharp
// AVOID: LINQ allocates enumerators and delegates on hot paths
int found = data.ToArray().Count(b => b == target);
```

## Performance (.NET 8+)

### Span\<T\> / Memory\<T\>

```csharp
// CORRECT: zero-allocation slicing
ReadOnlySpan<char> command = input.AsSpan()[1..];  // skip '/'

// CORRECT: stack-allocated parsing
public static int ParseVarInt(ReadOnlySpan<byte> data, out int bytesRead)
{
    int result = 0; bytesRead = 0; byte cur;
    do { cur = data[bytesRead]; result |= (cur & 0x7F) << (bytesRead * 7); bytesRead++; }
    while ((cur & 0x80) != 0);
    return result;
}
```

### FrozenDictionary / FrozenSet (.NET 8)

Build once, read many — ~50% faster lookups than Dictionary.

```csharp
// CORRECT: FrozenDictionary for read-heavy lookup tables (palettes, protocol maps)
using System.Collections.Frozen;
private static readonly FrozenDictionary<int, string> s_blockNames =
    new Dictionary<int, string> { [0] = "air", [1] = "stone" }.ToFrozenDictionary();
```

### SearchValues\<T\> (.NET 8)

Hardware-accelerated set search.

```csharp
// CORRECT: precompute once, scan with SIMD
private static readonly SearchValues<char> s_separators = SearchValues.Create(" \t\n\r,;");
int idx = input.AsSpan().IndexOfAny(s_separators);
```

### CompositeFormat (.NET 8)

Parse format string once, reuse.

```csharp
// CORRECT: avoids re-parsing the format string each call
private static readonly CompositeFormat s_logFmt = CompositeFormat.Parse("[{0:HH:mm:ss}] {1}: {2}");
string msg = string.Format(CultureInfo.InvariantCulture, s_logFmt, DateTime.Now, player, text);
```

### ArrayPool / stackalloc

```csharp
// CORRECT: rent from pool for temporary buffers
byte[] buf = ArrayPool<byte>.Shared.Rent(4096);
try { int n = stream.Read(buf.AsSpan(0, 4096)); ProcessPacket(buf.AsSpan(0, n)); }
finally { ArrayPool<byte>.Shared.Return(buf); }

// CORRECT: stackalloc for small, fixed-size buffers (< 512 bytes)
Span<byte> header = stackalloc byte[5];
```

## String Handling

```csharp
// CORRECT: explicit StringComparison — always
bool match = name.Equals("Steve", StringComparison.OrdinalIgnoreCase);
int idx = text.IndexOf("hello", StringComparison.Ordinal);
```

```csharp
// WRONG: allocates a lowered copy
bool match = name.ToLower() == "steve";
```

```csharp
// CORRECT: string.Create for perf-critical formatting
string hex = string.Create(data.Length * 2, data, static (span, bytes) =>
{
    for (int i = 0; i < bytes.Length; i++)
        bytes[i].TryFormat(span[(i * 2)..], out _, "X2");
});

// CORRECT: StringBuilder for loops
var sb = new StringBuilder(256);
foreach (var item in inventory)
    sb.Append(item.Name).Append(" x").Append(item.Count).AppendLine();
```

```csharp
// WRONG: O(n²) string concatenation in loop
string combined = "";
foreach (var s in items) combined += s + ", ";
```

## Collections — Choosing the Right Type

| Scenario | Type | Notes |
|---|---|---|
| General key-value | `Dictionary<K,V>` | O(1) lookup |
| Build once, read many | `FrozenDictionary<K,V>` | .NET 8+; faster reads |
| Thread-safe | `ConcurrentDictionary<K,V>` | Lock-free reads |
| Immutable snapshots | `ImmutableDictionary<K,V>` | Persistent structure |
| Membership test | `HashSet<T>` / `FrozenSet<T>` | FrozenSet for static |
| Priority queue | `PriorityQueue<E,P>` | .NET 6+ |
| Synchronization | `System.Threading.Lock` | C# 13; prefer over `lock(obj)` |
| Producer-consumer | `Channel<T>` | Over `BlockingCollection<T>` |
| Temp buffer | `ArrayPool<T>` / `stackalloc` | Zero/low alloc |

## Error Handling

```csharp
// CORRECT: Try* pattern for expected failures
if (int.TryParse(input, out int value)) ProcessValue(value);
if (_registry.TryGetValue(packetId, out var handler)) handler.Invoke(data);
```

```csharp
// WRONG: using exceptions for control flow
try { return dict[key]; }
catch (KeyNotFoundException) { return null; }  // use TryGetValue
```

```csharp
// CORRECT: exception filters (catch-when)
try { await ConnectAsync(ct); }
catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
{
    LogToConsole("Connection refused, retrying...");
}
```

```csharp
// CORRECT: throw helpers (smaller IL, better inlining)
ArgumentNullException.ThrowIfNull(handler);
ArgumentOutOfRangeException.ThrowIfNegative(timeout);
ArgumentOutOfRangeException.ThrowIfGreaterThan(timeout, MaxTimeout);
ObjectDisposedException.ThrowIf(_disposed, this);
```

```csharp
// WRONG: generic exceptions
throw new Exception($"Entity {id} not found");
```

```csharp
// CORRECT: specific, meaningful exception types
throw new EntityNotFoundException(id);
// or use null-coalescing with throw
return await FindEntityAsync(id, ct)
    ?? throw new EntityNotFoundException(id);
```

```csharp
// AVOID: catching Exception without filtering
try { DoWork(); }
catch (Exception) { /* swallowed */ }
```

## Warning Suppression

```csharp
// CORRECT: fix the warning by handling null properly
public string GetDisplayName(Player? player)
{
    return player?.DisplayName ?? "Unknown";
}
```

```csharp
// WRONG: suppressing nullable warning with pragma
#pragma warning disable CS8602
public string GetDisplayName(Player? player)
{
    return player.DisplayName;  // NullReferenceException at runtime
}
#pragma warning restore CS8602
```

```csharp
// WRONG: suppressing with attribute
[SuppressMessage("Usage", "CA1062:Validate arguments of public methods")]
public void Process(Packet packet)
{
    // missing null check
}
```

Project-wide `.editorconfig` is the only acceptable place for warning policy:

```text
# .editorconfig - project-wide policy decisions only
dotnet_diagnostic.CA2007.severity = none
```

## Resource Management

```csharp
// CORRECT: using declaration — disposed at end of scope
using var stream = new FileStream(path, FileMode.Open);
using var reader = new StreamReader(stream);

// CORRECT: IAsyncDisposable
await using var conn = await CreateConnectionAsync();
```

```csharp
// CORRECT: Dispose pattern
public class PacketReader : IDisposable
{
    private Stream? _stream;
    private bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        _stream?.Dispose(); _stream = null; _disposed = true;
    }
}
```

## Security

```csharp
// CORRECT: secure random for tokens
byte[] token = RandomNumberGenerator.GetBytes(32);

// CORRECT: constant-time comparison for secrets
bool valid = CryptographicOperations.FixedTimeEquals(expected, actual);

// CORRECT: validate external input
if (Uri.TryCreate(userInput, UriKind.Absolute, out var uri)
    && uri.Scheme is "http" or "https")
    await FetchAsync(uri);
```

```csharp
// WRONG: predictable random for security-sensitive values
var rng = new Random();

// WRONG: timing side-channel on secret comparison
bool eq = secret1.SequenceEqual(secret2);
```

## Miscellaneous Idioms

```csharp
// CORRECT: var when type is obvious from RHS
var entities = new Dictionary<int, Entity>();
var timer = Stopwatch.StartNew();

// CORRECT: explicit type when var would be unclear
Stream responseStream = GetResponse();
int count = items.Count;

// CORRECT: expression-bodied members for one-liners
public override string ToString() => $"[{X}, {Y}, {Z}]";
public bool IsAlive => Health > 0;

// CORRECT: discards for unused values
_ = int.TryParse(s, out int result);
(_, int y, _) = GetCoordinates();

// CORRECT: nameof for resilient refactoring (unbound generics in C# 14)
throw new ArgumentException("Invalid value", nameof(packetId));
LogToConsole($"{nameof(AutoEat)}: eating {item.Name}");
string typeName = nameof(Dictionary<,>);  // "Dictionary"

// CORRECT: static lambdas prevent accidental closure allocations
list.Sort(static (a, b) => a.Id.CompareTo(b.Id));

// CORRECT: index/range operators
var last = items[^1];
var slice = data[3..^1];

// CORRECT: tuple deconstruction
var (x, y, z) = GetPosition();

// CORRECT: string interpolation with alignment and format specifiers
LogToConsole($"Health: {health,6:F1} | Hunger: {hunger,6:F1}");
```
