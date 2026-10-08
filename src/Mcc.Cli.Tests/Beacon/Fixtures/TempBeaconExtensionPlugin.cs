using DMCBK.Core.Beacon;

namespace Mcc.Cli.Tests.Beacon.Fixtures;

/// <summary>
/// Temporary language-extension exercise plugin for the Beacon interop tests below.
/// It lives in the test fixtures and never ships: it registers two custom functions (a pure one and a capability-gated stateful one), one mutable variable namespace, and one suppressible capability-gated event, all under unique temp-fixture names.
/// </summary>
internal sealed class TempBeaconExtensionPlugin : IDisposable
{
    public const string PluginId = "temp-fixture";
    public const string EconCapability = "temp.econ";
    public const string FreeCapability = "temp.free";
    public const string ShoutFunction = "temp_shout";
    public const string DepositFunction = "temp_deposit";
    public const string VaultVariable = "tempvault";
    public const string SaleEvent = "temp_sale";

    private readonly List<IDisposable> _handles = [];
    private readonly Dictionary<string, object?> _vault = new(StringComparer.Ordinal) { ["balance"] = 0 };
    private bool _disposed;

    /// <summary>How many times scripts called <c>temp_deposit</c>.</summary>
    public int DepositCalls { get; private set; }

    /// <summary>Attaches every extension point to <paramref name="engine"/>.</summary>
    public void Attach(BeaconEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ObjectDisposedException.ThrowIf(_disposed, this);
        _handles.Add(engine.Bridge.RegisterFunction(new BeaconExtensionFunction(
            ShoutFunction, PluginId, FreeCapability, "Uppercase text (pure, no state).", ["text"],
            call => Task.FromResult(Shout(call)))));
        _handles.Add(engine.Bridge.RegisterFunction(new BeaconExtensionFunction(
            DepositFunction, PluginId, EconCapability, "Add to the vault, return the new balance.", ["amount"],
            call => Task.FromResult(Deposit(call)))));
        _handles.Add(engine.Bridge.RegisterVariable(new BeaconVariableRegistration(
            VaultVariable, PluginId, EconCapability, "Fixture vault ledger.",
            _ => new Dictionary<string, object?>(_vault, StringComparer.Ordinal))));
        _handles.Add(engine.Bridge.RegisterEvent(
            PluginId, SaleEvent, ["item", "price"], "A fixture sale.",
            suppressible: true, capability: EconCapability));
    }

    /// <summary>Writes vault state from the C# side (script reads observe it on next read).</summary>
    public void SetBalance(int balance) => _vault["balance"] = balance;

    /// <summary>Reads vault state from the C# side.</summary>
    public int GetBalance() => (int)_vault["balance"]!;

    /// <summary>Fires the fixture sale event on <paramref name="engine"/>.</summary>
    public Task<BeaconFireResult> FireSaleAsync(BeaconEngine engine, string item, double price)
    {
        ArgumentNullException.ThrowIfNull(engine);
        return engine.Bridge.FireEventAsync(
            SaleEvent,
            new Dictionary<string, BeaconValue>(StringComparer.Ordinal)
            {
                ["item"] = BeaconValue.Text(item),
                ["price"] = BeaconValue.Number(price),
            });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (IDisposable handle in _handles)
            handle.Dispose();

        _handles.Clear();
        BeaconProviders.WithdrawPlugin(PluginId);
    }

    private static BeaconValue Shout(BeaconExtensionCall call)
    {
        if (call.Args.Count != 1 || call.Args[0] is not BeaconTextValue text)
        {
            throw new InvalidOperationException(
                $"Extern '{ShoutFunction}' from plugin '{PluginId}' needs 1 text argument.");
        }

        return BeaconValue.Text(text.Value.ToUpperInvariant());
    }

    private BeaconValue Deposit(BeaconExtensionCall call)
    {
        if (call.Args.Count != 1 || call.Args[0] is not BeaconNumberValue number)
        {
            throw new InvalidOperationException(
                $"Extern '{DepositFunction}' from plugin '{PluginId}' needs 1 number argument.");
        }

        DepositCalls++;
        int next = GetBalance() + (int)number.Value;
        _vault["balance"] = next;
        return BeaconValue.Number(next);
    }
}
