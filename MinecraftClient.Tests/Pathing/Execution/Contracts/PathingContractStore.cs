using System.Text.Json;
using System.Text.Json.Serialization;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Tests.Pathing.Execution.Contracts;

public sealed class PathingContractStore
{
    private readonly IReadOnlyDictionary<string, PathingPlannerContract> planners;
    private readonly IReadOnlyDictionary<string, PathingTimingBudget> timings;

    private PathingContractStore(
        IReadOnlyDictionary<string, PathingPlannerContract> planners,
        IReadOnlyDictionary<string, PathingTimingBudget> timings)
    {
        this.planners = planners;
        this.timings = timings;
    }

    public static PathingContractStore LoadFromRepositoryRoot()
    {
        string rootPath = FindRepositoryRoot();
        string pathingDir = Path.Combine(rootPath, "MinecraftClient.Tests", "TestData", "Pathing");

        string plannerJson = File.ReadAllText(Path.Combine(pathingDir, "pathing-planner-contracts.json"));
        string timingJson = File.ReadAllText(Path.Combine(pathingDir, "pathing-timing-budgets.json"));

        return LoadFromJson(plannerJson, timingJson);
    }

    public static PathingContractStore LoadFromJson(string plannerJson, string timingJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plannerJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(timingJson);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        PathingPlannerContract[] plannerContracts = JsonSerializer.Deserialize<PathingPlannerContract[]>(plannerJson, options)
            ?? throw new InvalidOperationException("Failed to deserialize planner contracts.");
        PathingTimingBudget[] timingBudgets = JsonSerializer.Deserialize<PathingTimingBudget[]>(timingJson, options)
            ?? throw new InvalidOperationException("Failed to deserialize timing budgets.");

        Dictionary<string, PathingPlannerContract> plannerByScenario = BuildPlannerDictionary(plannerContracts);
        Dictionary<string, PathingTimingBudget> timingByScenario = BuildTimingDictionary(timingBudgets);
        ValidateScenarioSetConsistency(plannerByScenario, timingByScenario);
        ValidateScenarioSegmentAlignment(plannerByScenario, timingByScenario);

        return new PathingContractStore(
            plannerByScenario,
            timingByScenario);
    }

    public PathingPlannerContract GetPlanner(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return planners.TryGetValue(id, out PathingPlannerContract? contract)
            ? contract
            : throw new KeyNotFoundException($"Planner contract '{id}' was not found.");
    }

    public PathingTimingBudget GetTiming(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return timings.TryGetValue(id, out PathingTimingBudget? budget)
            ? budget
            : throw new KeyNotFoundException($"Timing budget '{id}' was not found.");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MinecraftClient.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root from current test execution directory.");
    }

    private static Dictionary<string, PathingPlannerContract> BuildPlannerDictionary(IEnumerable<PathingPlannerContract> contracts)
    {
        var result = new Dictionary<string, PathingPlannerContract>(StringComparer.Ordinal);
        foreach (PathingPlannerContract contract in contracts)
        {
            PathingPlannerContract normalized = ValidateAndNormalizePlanner(contract);
            if (!result.TryAdd(normalized.ScenarioId, normalized))
                throw new InvalidDataException($"Duplicate planner contract scenario id '{normalized.ScenarioId}'.");
        }

        return result;
    }

    private static Dictionary<string, PathingTimingBudget> BuildTimingDictionary(IEnumerable<PathingTimingBudget> budgets)
    {
        var result = new Dictionary<string, PathingTimingBudget>(StringComparer.Ordinal);
        foreach (PathingTimingBudget budget in budgets)
        {
            PathingTimingBudget normalized = ValidateAndNormalizeTiming(budget);
            if (!result.TryAdd(normalized.ScenarioId, normalized))
                throw new InvalidDataException($"Duplicate timing budget scenario id '{normalized.ScenarioId}'.");
        }

        return result;
    }

    private static PathingPlannerContract ValidateAndNormalizePlanner(PathingPlannerContract contract)
    {
        if (string.IsNullOrWhiteSpace(contract.ScenarioId))
            throw new InvalidDataException("Planner contract has blank scenario id.");

        if (contract.Segments is null)
            throw new InvalidDataException($"Planner contract '{contract.ScenarioId}' has null segments.");

        if (contract.ExpectedStatus != PathStatus.Failed && contract.Segments.Count == 0)
            throw new InvalidDataException($"Planner contract '{contract.ScenarioId}' must contain at least one segment unless the expected status is Failed.");

        var normalizedSegments = new List<PathingPlannerSegmentContract>(contract.Segments.Count);
        for (int i = 0; i < contract.Segments.Count; i++)
        {
            PathingPlannerSegmentContract segment = contract.Segments[i];
            normalizedSegments.Add(segment);
        }

        return contract with { Segments = normalizedSegments.AsReadOnly() };
    }

    private static PathingTimingBudget ValidateAndNormalizeTiming(PathingTimingBudget budget)
    {
        if (string.IsNullOrWhiteSpace(budget.ScenarioId))
            throw new InvalidDataException("Timing budget has blank scenario id.");

        if (budget.Segments is null)
            throw new InvalidDataException($"Timing budget '{budget.ScenarioId}' has null segments.");

        if (budget.ExpectedTotalTicks < 0 || budget.MaxTotalTicks < 0)
            throw new InvalidDataException($"Timing budget '{budget.ScenarioId}' total ticks must be nonnegative.");

        if (budget.ExpectedTotalTicks > budget.MaxTotalTicks)
            throw new InvalidDataException($"Timing budget '{budget.ScenarioId}' has ExpectedTotalTicks greater than MaxTotalTicks.");

        if (budget.Segments.Count == 0 && (budget.ExpectedTotalTicks != 0 || budget.MaxTotalTicks != 0))
            throw new InvalidDataException($"Timing budget '{budget.ScenarioId}' must use zero totals when it has no segments.");

        var normalizedSegments = new List<PathingSegmentTimingBudget>(budget.Segments.Count);
        for (int i = 0; i < budget.Segments.Count; i++)
        {
            PathingSegmentTimingBudget segment = budget.Segments[i];
            if (segment.ExpectedTicks < 0 || segment.MaxTicks < 0)
                throw new InvalidDataException($"Timing budget '{budget.ScenarioId}' segment {i} has negative tick values.");

            if (segment.ExpectedTicks > segment.MaxTicks)
                throw new InvalidDataException($"Timing budget '{budget.ScenarioId}' segment {i} has ExpectedTicks greater than MaxTicks.");

            normalizedSegments.Add(segment);
        }

        return budget with { Segments = normalizedSegments.AsReadOnly() };
    }

    private static void ValidateScenarioSetConsistency(
        IReadOnlyDictionary<string, PathingPlannerContract> plannersByScenario,
        IReadOnlyDictionary<string, PathingTimingBudget> timingsByScenario)
    {
        string[] plannerOnly = plannersByScenario.Keys.Except(timingsByScenario.Keys, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        string[] timingOnly = timingsByScenario.Keys.Except(plannersByScenario.Keys, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();

        if (plannerOnly.Length == 0 && timingOnly.Length == 0)
            return;

        string plannerOnlyList = plannerOnly.Length == 0 ? "<none>" : string.Join(", ", plannerOnly);
        string timingOnlyList = timingOnly.Length == 0 ? "<none>" : string.Join(", ", timingOnly);
        throw new InvalidDataException(
            $"Planner/timing scenario set mismatch. Missing timing entries for: {plannerOnlyList}. Missing planner entries for: {timingOnlyList}.");
    }

    private static void ValidateScenarioSegmentAlignment(
        IReadOnlyDictionary<string, PathingPlannerContract> plannersByScenario,
        IReadOnlyDictionary<string, PathingTimingBudget> timingsByScenario)
    {
        foreach ((string scenarioId, PathingPlannerContract planner) in plannersByScenario)
        {
            PathingTimingBudget timing = timingsByScenario[scenarioId];
            if (planner.Segments.Count != timing.Segments.Count)
            {
                throw new InvalidDataException(
                    $"Scenario '{scenarioId}' segment count mismatch. Planner has {planner.Segments.Count} segments, timing has {timing.Segments.Count}.");
            }

            for (int i = 0; i < planner.Segments.Count; i++)
            {
                MoveType plannerMove = planner.Segments[i].MoveType;
                MoveType timingMove = timing.Segments[i].MoveType;
                if (plannerMove != timingMove)
                {
                    throw new InvalidDataException(
                        $"Scenario '{scenarioId}' segment {i} move mismatch. Planner has {plannerMove}, timing has {timingMove}.");
                }
            }
        }
    }
}
