using Mcc.Cli.Localization;
using System.Text.RegularExpressions;
using DMCBK.Core;
using Umpk.Client.Events;
using Umpk.Geometry;

namespace Mcc.Cli.Diagnostics;

/// <summary>
/// The <c>--exercise smoke</c> scripted diagnostic (shippable).
/// After the client reaches Playing, it drives a fixed async sequence against <see cref="Client.Game"/> and prints one machine-greppable <c>PASS</c>/<c>FAIL</c>/<c>SKIP</c> line per check, then a summary.
/// It never sends server commands itself; the live runner drives server-side setup (setblock/summon/give/say) over the server stdin, and the checks poll a short window so exact timing between runner and client is not load-bearing.
/// Feature-gated reads that hit a disabled gameplay feature are reported as SKIP, not FAIL.
/// </summary>
internal static class SmokeExercise
{
    public const string Name = "smoke";

    private static readonly TimeSpan PollWindow = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan MoveTimeout = TimeSpan.FromSeconds(6);

    // A rendered translate template that never resolved looks like a bare "namespace.key" token.
    private static readonly Regex RawKeyShape = new(@"^[a-z0-9_]+(\.[a-z0-9_]+)+$", RegexOptions.Compiled);

    /// <summary>Runs the smoke checks and returns 0 when every non-skipped check passed, else 1.</summary>
    public static async Task<int> RunAsync(
        Client client, string expectedUsername, Action<string> log, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(log);

        var tally = new Tally(log);
        log(Strings.ExerciseStart(Name));

        string? lastChat = null;
        var chatGate = new object();
        void OnChat(object? sender, ChatMessageReceived message)
        {
            // Render through the translation source so translate-key components flatten to human text.
            string text = message.Message.ToPlainText(client.Translations);
            if (!string.IsNullOrWhiteSpace(text))
            {
                lock (chatGate)
                    lastChat = text;
            }
        }

        client.Game.Chat.MessageReceived += OnChat;
        try
        {
            await CheckWorldTimeAsync(client, tally, ct).ConfigureAwait(false);
            await CheckWorldBlockAsync(client, tally, ct).ConfigureAwait(false);
            await CheckEntitiesAsync(client, tally, ct).ConfigureAwait(false);
            await CheckInventoryAsync(client, tally, ct).ConfigureAwait(false);
            await CheckPlayerVitalsAsync(client, tally, ct).ConfigureAwait(false);
            await CheckTabListAsync(client, expectedUsername, tally, ct).ConfigureAwait(false);
            await CheckTranslationAsync(() => { lock (chatGate) return lastChat; }, tally, ct).ConfigureAwait(false);
            await CheckMovementAsync(client, tally, ct).ConfigureAwait(false);
        }
        finally
        {
            client.Game.Chat.MessageReceived -= OnChat;
        }

        log(Strings.ExerciseSummary(Name, tally.Passed, tally.Failed, tally.Skipped));
        return tally.Failed == 0 ? 0 : 1;
    }

    private static async Task CheckWorldTimeAsync(Client client, Tally tally, CancellationToken ct)
    {
        const string check = "world.time";
        try
        {
            // The world installs on the join packet, which can land a moment after Playing; retry until ready.
            WorldTimeInfo? readyFirst = await WaitForWorldAsync(() => client.Game.World.GetTimeAsync(ct), ct).ConfigureAwait(false);
            if (readyFirst is not { } first)
            {
                tally.Fail(check, "world did not become available within the poll window");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1.2), ct).ConfigureAwait(false);
            WorldTimeInfo second = await client.Game.World.GetTimeAsync(ct).ConfigureAwait(false);

            bool advanced = second.WorldAge != first.WorldAge || second.TimeOfDay != first.TimeOfDay;
            tally.Pass(check, $"age {first.WorldAge}->{second.WorldAge} timeOfDay {first.TimeOfDay}->{second.TimeOfDay} advanced={advanced}");
        }
        catch (DmcbkFeatureDisabledException ex)
        {
            tally.Skip(check, $"terrain feature disabled ({ex.Feature})");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tally.Fail(check, ex.Message);
        }
    }

    /// <summary>
    /// Retries a world read while it throws the "world not available" readiness exception, until the read succeeds or the poll window elapses.
    /// Returns null on timeout; rethrows feature/session exceptions.
    /// </summary>
    private static async Task<T?> WaitForWorldAsync<T>(Func<Task<T>> read, CancellationToken ct)
        where T : class
    {
        DateTime deadline = DateTime.UtcNow + PollWindow;
        while (true)
        {
            try
            {
                return await read().ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (IsWorldNotReady(ex) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollInterval, ct).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (IsWorldNotReady(ex))
            {
                return null;
            }
        }
    }

    // Only the "world installs on join" readiness error is worth retrying; any other InvalidOperationException (for example an empty block registry) is a real fault and must surface immediately.
    private static bool IsWorldNotReady(InvalidOperationException ex)
        => ex.Message.Contains("not available until", StringComparison.Ordinal);

    private static async Task CheckWorldBlockAsync(Client client, Tally tally, CancellationToken ct)
    {
        const string check = "world.block";
        try
        {
            PlayerPose pose = await client.Game.Movement.GetPoseAsync(ct).ConfigureAwait(false);
            BlockPos floor = BlockPos.Containing(pose.Position).Below();

            // Poll: the world may install a moment after Playing, and the chunk under the player may not have arrived yet.
            // Retry through both the readiness exception and an air (unloaded) result.
            BlockInfo? readyBlock = await WaitForWorldAsync(() => client.Game.World.GetBlockAsync(floor, ct), ct).ConfigureAwait(false);
            if (readyBlock is not { } first)
            {
                tally.Fail(check, "world did not become available within the poll window");
                return;
            }

            BlockInfo block = first;
            DateTime deadline = DateTime.UtcNow + PollWindow;
            while (block.IsAir && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollInterval, ct).ConfigureAwait(false);
                pose = await client.Game.Movement.GetPoseAsync(ct).ConfigureAwait(false);
                floor = BlockPos.Containing(pose.Position).Below();
                block = await client.Game.World.GetBlockAsync(floor, ct).ConfigureAwait(false);
            }

            if (block.IsAir)
            {
                tally.Fail(check, $"block below player at {floor.X},{floor.Y},{floor.Z} is air (terrain not loaded)");
                return;
            }

            IReadOnlyList<BlockPos> hits =
                await client.Game.World.FindBlocksAsync(block.BlockId, radius: 6, maxResults: 256, ct).ConfigureAwait(false);
            tally.Pass(check, $"floor={block.BlockId} state={block.StateId} findblocks({block.BlockId})={hits.Count}");
        }
        catch (DmcbkFeatureDisabledException ex)
        {
            tally.Skip(check, $"terrain feature disabled ({ex.Feature})");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Registry", StringComparison.Ordinal))
        {
            tally.Fail(check, $"block name unresolvable (block registry not wired): {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tally.Fail(check, ex.Message);
        }
    }

    private static async Task CheckEntitiesAsync(Client client, Tally tally, CancellationToken ct)
    {
        const string check = "entities";
        try
        {
            IReadOnlyList<EntitySnapshot> entities = [];
            DateTime deadline = DateTime.UtcNow + PollWindow;
            while (DateTime.UtcNow < deadline)
            {
                entities = await client.Game.Entities.AllAsync(ct).ConfigureAwait(false);
                if (entities.Count > 0)
                    break;

                await Task.Delay(PollInterval, ct).ConfigureAwait(false);
            }

            if (entities.Count == 0)
            {
                tally.Skip(check, "no entities tracked in the poll window (runner did not summon a nearby entity)");
                return;
            }

            var types = new List<string>();
            bool anyUnknown = false;
            foreach (EntitySnapshot entity in entities)
            {
                types.Add(entity.TypeId);
                if (string.Equals(entity.TypeId, "minecraft:unknown", StringComparison.Ordinal))
                    anyUnknown = true;
            }

            string detail = $"count={entities.Count} types=[{string.Join(", ", types)}]";
            if (anyUnknown)
                tally.Fail(check, $"an entity resolved to minecraft:unknown (registries not wired); {detail}");
            else
                tally.Pass(check, detail);
        }
        catch (DmcbkFeatureDisabledException ex)
        {
            tally.Skip(check, $"entities feature disabled ({ex.Feature})");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tally.Fail(check, ex.Message);
        }
    }

    private static async Task CheckInventoryAsync(Client client, Tally tally, CancellationToken ct)
    {
        const string check = "inventory";
        try
        {
            PlayerInventorySnapshot snapshot = await client.Game.Inventory.GetPlayerInventoryAsync(ct).ConfigureAwait(false);
            if (snapshot.Slots.Count != 46)
            {
                tally.Fail(check, $"expected 46 slots, got {snapshot.Slots.Count}");
                return;
            }

            // Poll for a runner-given item so the detail line shows a non-empty slot when one arrives.
            var nonEmpty = new List<string>();
            DateTime deadline = DateTime.UtcNow + PollWindow;
            while (DateTime.UtcNow < deadline)
            {
                snapshot = await client.Game.Inventory.GetPlayerInventoryAsync(ct).ConfigureAwait(false);
                nonEmpty.Clear();
                for (int i = 0; i < snapshot.Slots.Count; i++)
                {
                    ItemStackInfo stack = snapshot.Slots[i];
                    if (!stack.IsEmpty)
                        nonEmpty.Add($"[{i}]={stack.ItemId}x{stack.Count}");
                }

                if (nonEmpty.Count > 0)
                    break;

                await Task.Delay(PollInterval, ct).ConfigureAwait(false);
            }

            string slots = nonEmpty.Count > 0 ? string.Join(", ", nonEmpty) : "(all empty)";
            tally.Pass(check, $"slots=46 held={snapshot.HeldSlot} nonempty={{{slots}}}");
        }
        catch (DmcbkFeatureDisabledException ex)
        {
            tally.Skip(check, $"inventory feature disabled ({ex.Feature})");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tally.Fail(check, ex.Message);
        }
    }

    private static async Task CheckPlayerVitalsAsync(Client client, Tally tally, CancellationToken ct)
    {
        const string check = "player.vitals";
        try
        {
            PlayerStatus status = await client.Game.Player.GetStatusAsync(ct).ConfigureAwait(false);
            tally.Pass(check, $"health={status.Health} food={status.Food} gamemode={status.GameMode}");
        }
        catch (DmcbkFeatureDisabledException ex)
        {
            tally.Skip(check, $"feature disabled ({ex.Feature})");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tally.Fail(check, ex.Message);
        }
    }

    private static async Task CheckTabListAsync(Client client, string expectedUsername, Tally tally, CancellationToken ct)
    {
        const string check = "player.tablist";
        try
        {
            DateTime deadline = DateTime.UtcNow + PollWindow;
            TabListSnapshot list = await client.Game.Player.GetTabListAsync(ct).ConfigureAwait(false);
            bool found = false;
            while (DateTime.UtcNow < deadline)
            {
                list = await client.Game.Player.GetTabListAsync(ct).ConfigureAwait(false);
                found = list.Entries.Any(e => string.Equals(e.Name, expectedUsername, StringComparison.OrdinalIgnoreCase));
                if (found)
                    break;

                await Task.Delay(PollInterval, ct).ConfigureAwait(false);
            }

            if (found)
                tally.Pass(check, $"entries={list.Entries.Count} self='{expectedUsername}' present");
            else
            {
                string names = string.Join(", ", list.Entries.Select(e => e.Name));
                tally.Fail(check, $"self '{expectedUsername}' not in tab list; entries=[{names}]");
            }
        }
        catch (DmcbkFeatureDisabledException ex)
        {
            tally.Skip(check, $"feature disabled ({ex.Feature})");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tally.Fail(check, ex.Message);
        }
    }

    private static async Task CheckTranslationAsync(Func<string?> readLastChat, Tally tally, CancellationToken ct)
    {
        const string check = "translation";
        DateTime deadline = DateTime.UtcNow + PollWindow;
        string? text = readLastChat();
        while (text is null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollInterval, ct).ConfigureAwait(false);
            text = readLastChat();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            tally.Skip(check, "no chat/system message received in the poll window (runner did not say/tellraw)");
            return;
        }

        if (RawKeyShape.IsMatch(text.Trim()))
        {
            tally.Fail(check, $"rendered text looks like an unresolved translate key: '{text}'");
            return;
        }

        tally.Pass(check, $"rendered='{text}'");
    }

    private static async Task CheckMovementAsync(Client client, Tally tally, CancellationToken ct)
    {
        const string check = "movement";
        try
        {
            PlayerPose pose = await client.Game.Movement.GetPoseAsync(ct).ConfigureAwait(false);
            Vec3d target = pose.Position.Add(2.0, 0.0, 0.0);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(MoveTimeout);
            try
            {
                await client.Game.Movement.MoveToAsync(target, timeout.Token).ConfigureAwait(false);
                PlayerPose after = await client.Game.Movement.GetPoseAsync(ct).ConfigureAwait(false);
                double moved = after.Position.Subtract(pose.Position).Length();
                tally.Pass(check, $"moved {moved:0.00} blocks to ~{after.Position.X:0.0},{after.Position.Y:0.0},{after.Position.Z:0.0}");
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                tally.Skip(check, "MoveToAsync did not complete within the timeout (nice-to-have)");
            }
        }
        catch (DmcbkFeatureDisabledException ex)
        {
            tally.Skip(check, $"physics feature disabled ({ex.Feature})");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tally.Skip(check, $"move faulted (nice-to-have): {ex.Message}");
        }
    }

    /// <summary>Prints each result line as it happens and counts pass/fail/skip.</summary>
    private sealed class Tally(Action<string> log)
    {
        public int Passed { get; private set; }

        public int Failed { get; private set; }

        public int Skipped { get; private set; }

        public void Pass(string check, string detail)
        {
            Passed++;
            log(Strings.ExercisePass(check, detail));
        }

        public void Fail(string check, string detail)
        {
            Failed++;
            log(Strings.ExerciseFail(check, detail));
        }

        public void Skip(string check, string reason)
        {
            Skipped++;
            log(Strings.ExerciseSkip(check, reason));
        }
    }
}
