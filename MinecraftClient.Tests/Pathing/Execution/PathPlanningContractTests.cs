using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Tests.Pathing.Execution.Contracts;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathPlanningContractTests
{
    [Fact]
    public void Get_ManagerAcceptedAscendChain_LoadsExactPlannerContract()
    {
        var store = PathingContractStore.LoadFromRepositoryRoot();

        PathingPlannerContract contract = store.GetPlanner("manager-accepted-ascend-chain");

        Assert.Equal("manager-accepted-ascend-chain", contract.ScenarioId);
        Assert.Equal(PathStatus.Success, contract.ExpectedStatus);
        Assert.Equal(6, contract.Segments.Count);

        PathingPlannerSegmentContract firstSegment = contract.Segments[0];
        Assert.Equal(MoveType.Diagonal, firstSegment.MoveType);
        Assert.Equal(new PathingBlock(171, 80, 160), firstSegment.StartBlock);
        Assert.Equal(new PathingBlock(172, 80, 161), firstSegment.EndBlock);

        PathingPlannerSegmentContract lastSegment = contract.Segments[5];
        Assert.Equal(MoveType.Ascend, lastSegment.MoveType);
        Assert.Equal(new PathingBlock(176, 82, 162), lastSegment.StartBlock);
        Assert.Equal(new PathingBlock(177, 83, 162), lastSegment.EndBlock);
    }

    [Fact]
    public void Get_ManagerAcceptedAscendChain_LoadsTimingScaffoldShape()
    {
        var store = PathingContractStore.LoadFromRepositoryRoot();

        PathingTimingBudget timing = store.GetTiming("manager-accepted-ascend-chain");

        Assert.Equal("manager-accepted-ascend-chain", timing.ScenarioId);
        Assert.Equal(6, timing.Segments.Count);
        Assert.True(timing.ExpectedTotalTicks <= timing.MaxTotalTicks);

        PathingSegmentTimingBudget firstSegment = timing.Segments[0];
        Assert.Equal(MoveType.Diagonal, firstSegment.MoveType);
        Assert.True(firstSegment.ExpectedTicks <= firstSegment.MaxTicks);

        foreach (PathingSegmentTimingBudget segment in timing.Segments)
        {
            Assert.True(segment.ExpectedTicks <= segment.MaxTicks);
        }
    }

    [Fact]
    public void LoadFromJson_RejectsTimingSegment_WhenExpectedExceedsMax()
    {
        const string plannerJson = """
[
  {
    "scenarioId": "manager-accepted-ascend-chain",
    "expectedStatus": "Success",
    "segments": [
      {
        "moveType": "Diagonal",
        "startBlock": { "x": 171, "y": 80, "z": 160 },
        "endBlock": { "x": 172, "y": 80, "z": 161 }
      }
    ]
  }
]
""";
        const string timingJson = """
[
  {
    "scenarioId": "manager-accepted-ascend-chain",
    "expectedTotalTicks": 1,
    "maxTotalTicks": 1,
    "segments": [
      { "moveType": "Diagonal", "expectedTicks": 2, "maxTicks": 1 }
    ]
  }
]
""";

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => PathingContractStore.LoadFromJson(plannerJson, timingJson));
        Assert.Contains("manager-accepted-ascend-chain", error.Message);
    }

    [Fact]
    public void LoadFromJson_Rejects_WhenPlannerAndTimingScenarioSetsMismatch()
    {
        const string plannerJson = """
[
  {
    "scenarioId": "planner-only",
    "expectedStatus": "Success",
    "segments": [
      {
        "moveType": "Traverse",
        "startBlock": { "x": 0, "y": 80, "z": 0 },
        "endBlock": { "x": 1, "y": 80, "z": 0 }
      }
    ]
  }
]
""";
        const string timingJson = """
[
  {
    "scenarioId": "timing-only",
    "expectedTotalTicks": 1,
    "maxTotalTicks": 1,
    "segments": [
      { "moveType": "Traverse", "expectedTicks": 1, "maxTicks": 1 }
    ]
  }
]
""";

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => PathingContractStore.LoadFromJson(plannerJson, timingJson));
        Assert.Contains("planner-only", error.Message);
        Assert.Contains("timing-only", error.Message);
    }

    [Fact]
    public void LoadFromJson_RejectsDuplicatePlannerScenarioId()
    {
        const string plannerJson = """
[
  {
    "scenarioId": "dup",
    "expectedStatus": "Success",
    "segments": [
      {
        "moveType": "Traverse",
        "startBlock": { "x": 0, "y": 80, "z": 0 },
        "endBlock": { "x": 1, "y": 80, "z": 0 }
      }
    ]
  },
  {
    "scenarioId": "dup",
    "expectedStatus": "Success",
    "segments": [
      {
        "moveType": "Traverse",
        "startBlock": { "x": 1, "y": 80, "z": 0 },
        "endBlock": { "x": 2, "y": 80, "z": 0 }
      }
    ]
  }
]
""";
        const string timingJson = """
[
  {
    "scenarioId": "dup",
    "expectedTotalTicks": 1,
    "maxTotalTicks": 1,
    "segments": [
      { "moveType": "Traverse", "expectedTicks": 1, "maxTicks": 1 }
    ]
  }
]
""";

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => PathingContractStore.LoadFromJson(plannerJson, timingJson));
        Assert.Contains("Duplicate planner contract scenario id", error.Message);
    }

    [Fact]
    public void LoadFromJson_Rejects_WhenPlannerAndTimingMoveSequenceDiffer()
    {
        const string plannerJson = """
[
  {
    "scenarioId": "sequence-mismatch",
    "expectedStatus": "Success",
    "segments": [
      {
        "moveType": "Traverse",
        "startBlock": { "x": 0, "y": 80, "z": 0 },
        "endBlock": { "x": 1, "y": 80, "z": 0 }
      },
      {
        "moveType": "Ascend",
        "startBlock": { "x": 1, "y": 80, "z": 0 },
        "endBlock": { "x": 2, "y": 81, "z": 0 }
      }
    ]
  }
]
""";
        const string timingJson = """
[
  {
    "scenarioId": "sequence-mismatch",
    "expectedTotalTicks": 2,
    "maxTotalTicks": 2,
    "segments": [
      { "moveType": "Traverse", "expectedTicks": 1, "maxTicks": 1 },
      { "moveType": "Diagonal", "expectedTicks": 1, "maxTicks": 1 }
    ]
  }
]
""";

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => PathingContractStore.LoadFromJson(plannerJson, timingJson));
        Assert.Contains("sequence-mismatch", error.Message);
        Assert.Contains("segment 1", error.Message);
    }

    [Theory]
    [InlineData("same-move-ascend-staircase")]
    [InlineData("same-move-descend-staircase")]
    [InlineData("rejected-3x1-invalid-goal")]
    public void Scenario_PlannerMatchesContract(string scenarioId)
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
        PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);
        PathingPlannerContract contract = PathingContractStore.LoadFromRepositoryRoot().GetPlanner(scenarioId);

        PathingContractAssert.PlannerMatches(contract, PathSegmentBuilder.FromPath(planResult.Path), planResult);
    }
}
