using DMCBK.Core;
using DMCBK.Core.Localization;
using Umpk.Text;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Announces status effects in the console as they are gained and as they expire, which the legacy client did (McClient.cs:4200-4225) and the new one did not do at all.
/// <para>
/// Legacy announced straight off the effect packet.
/// UMPK surfaces its own <c>EntityEffectApplied</c>/<c>EntityEffectRemoved</c> events, but those carry the NUMERIC effect id and no session filter, so a host consuming them would have to re-implement the id-to-registry-name mapping and work out which entity is the local player.
/// The snapshot (<see cref="PlayerApi.GetEffectsAsync"/>) already carries the resolved registry id for the player alone, so this diffs that instead.
/// The visible behaviour is the same; the announcement can lag the packet by up to one poll.
/// </para>
/// <para>
/// The gain rule is legacy's <c>ShouldAnnouncePlayerEffectGain</c> (McClient.cs:4200-4208): announce when the effect was absent, or when it is present at a DIFFERENT amplifier, so a refresh of the same level is silent and a Speed I to Speed II upgrade is not.
/// </para>
/// </summary>
internal sealed class EffectAnnouncer : IAsyncDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly GameApi _game;
    private readonly ITranslationSource _translations;
    private readonly Action<string> _write;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, int> _current = new(StringComparer.Ordinal);

    private Task? _loop;

    public EffectAnnouncer(GameApi game, ITranslationSource translations, Action<string> write)
    {
        _game = game;
        _translations = translations;
        _write = write;
    }

    public void Start() => _loop ??= Task.Run(() => RunAsync(_cts.Token));

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await SafeTickAsync(timer, ct).ConfigureAwait(false))
        {
            IReadOnlyList<EffectSnapshot> effects;
            try
            {
                effects = await _game.Player.GetEffectsAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
            {
                // Between sessions the slate is blank, so a re-join announces afresh rather than reporting every carried-over effect as expired.
                _current.Clear();
                continue;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                continue;
            }

            Diff(effects);
        }
    }

    private static async Task<bool> SafeTickAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private void Diff(IReadOnlyList<EffectSnapshot> effects)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (EffectSnapshot effect in effects)
        {
            seen.Add(effect.EffectId);
            if (_current.TryGetValue(effect.EffectId, out int amplifier) && amplifier == effect.Amplifier)
                continue;

            _current[effect.EffectId] = effect.Amplifier;
            _write(Mcc.Cli.Localization.MccStrings.Format(
                "bot.effect.gained", WithArticle(effect), EffectText.ShortDuration(effect)));
        }

        foreach (string gone in _current.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _current.Remove(gone);
            _write(Mcc.Cli.Localization.MccStrings.Format("bot.effect.expired", EffectText.Name(gone, _translations)));
        }
    }

    // Legacy's GetDisplayNameWithArticle: "a Speed II" / "an Absorption", picked off the first letter, with the two articles themselves in the corpus so a translation can drop or change them.
    private string WithArticle(EffectSnapshot effect)
    {
        string name = EffectText.DisplayName(effect, _translations);
        if (name.Length == 0)
            return name;

        bool vowel = name[0] is 'a' or 'e' or 'i' or 'o' or 'u' or 'A' or 'E' or 'I' or 'O' or 'U';
        string article = Mcc.Cli.Localization.MccStrings.Get(vowel ? "effect.article.an" : "effect.article.a");
        return string.IsNullOrEmpty(article) ? name : $"{article} {name}";
    }
}
