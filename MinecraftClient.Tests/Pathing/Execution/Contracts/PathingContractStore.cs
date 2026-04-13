using System.Text.Json;
using System.Text.Json.Serialization;

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

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        string plannerJson = File.ReadAllText(Path.Combine(pathingDir, "pathing-planner-contracts.json"));
        string timingJson = File.ReadAllText(Path.Combine(pathingDir, "pathing-timing-budgets.json"));

        Dictionary<string, PathingPlannerContract> plannerContracts = JsonSerializer.Deserialize<Dictionary<string, PathingPlannerContract>>(plannerJson, options)
            ?? throw new InvalidOperationException("Failed to deserialize planner contracts.");
        Dictionary<string, PathingTimingBudget> timingBudgets = JsonSerializer.Deserialize<Dictionary<string, PathingTimingBudget>>(timingJson, options)
            ?? throw new InvalidOperationException("Failed to deserialize timing budgets.");

        return new PathingContractStore(plannerContracts, timingBudgets);
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
}
