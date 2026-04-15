using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class GroundedTemplateConvergenceTests
{
    [Fact]
    public void WalkTemplate_FinalStop_Completes_WhenFootprintStaysInsideTargetBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 160, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }

    [Fact]
    public void WalkTemplate_FinalStop_Completes_WhenCenterStopsInsideTargetBlockNearEdge()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var segment = new PathSegment
        {
            Start = new Location(2.5, 80, 0.5),
            End = new Location(3.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(segment, null);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(3.2897, 80.0, 0.5),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        var input = new MovementInput();
        TemplateState state = template.Tick(new Location(physics.Position.X, physics.Position.Y, physics.Position.Z), physics, input, world);

        Assert.Equal(TemplateState.Complete, state);
    }

    [Fact]
    public void WalkTemplate_FinalStop_Completes_FromLiveNearGoalState_WithoutFailure()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 95, max: 115);
        var segment = new PathSegment
        {
            Start = new Location(102.5, 80, 100.5),
            End = new Location(103.5, 80, 100.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(segment, null);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(103.36, 80.0, 100.50),
            DeltaMovement = new Vec3d(0.0346, 0.0, 0.0),
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 40, out _);

        Assert.Equal(TemplateState.Complete, state);
    }

    [Fact]
    public void WalkTemplate_PrepareJump_CompletesWithoutSettlingOnRunUpBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(1.5, 80, 0.5),
            End = new Location(3.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(current, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 60, out _);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(physics.DeltaMovement.X > 0.02);
    }

    [Fact]
    public void WalkTemplate_PrepareJump_WithPlannerHints_CompletesOnRunUpBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(1.5, 80, 0.5),
            End = new Location(3.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(current, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 120, out Location finalPos);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(
            TemplateFootingHelper.IsCenterInsideTargetBlock(finalPos, current.End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
    }

    [Fact]
    public void WalkTemplate_PrepareJump_SnapsYawImmediatelyDuringRunUp()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(1.5, 80, 0.5),
            End = new Location(3.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(current, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 90f);
        var input = new MovementInput();

        TemplateState state = template.Tick(current.Start, physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.InRange(physics.Yaw, 269.9f, 270.1f);
        Assert.True(input.Forward);
        Assert.True(input.Sprint);
    }

    [Fact]
    public void WalkTemplate_FinalStop_RetainsSmoothYawOutsideJumpEntry()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
        var input = new MovementInput();

        TemplateState state = template.Tick(segment.Start, physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.InRange(physics.Yaw, 124.9f, 125.1f);
    }

    [Fact]
    public void AscendTemplate_DiagonalPrepareJump_WithPlannerHints_CompletesOnRunUpBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -1, max: 6);
        FlatWorldTestBuilder.ClearBox(world, -1, 79, -1, 6, 84, 6);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 1, 80, 1);
        FlatWorldTestBuilder.SetSolid(world, 3, 80, 3);

        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 81, 1.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 1, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(1.5, 81, 1.5),
            End = new Location(3.5, 81, 3.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new AscendTemplate(current, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 315f);
        var input = new MovementInput();
        var trace = new List<string>();
        TemplateState state = TemplateState.InProgress;
        Location finalPos = current.Start;
        for (int tick = 0; tick < 140; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            if (tick < 20 || state != TemplateState.InProgress)
            {
                trace.Add(
                    $"tick={tick} state={state} pos={pos} yaw={physics.Yaw:F1} vel={physics.DeltaMovement} " +
                    $"onGround={physics.OnGround} input(F={input.Forward},J={input.Jump},S={input.Sprint})");
            }

            if (state != TemplateState.InProgress)
            {
                finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
                break;
            }

            physics.ApplyInput(input);
            physics.Tick(world);
            finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        }

        Assert.True(
            state == TemplateState.Complete,
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
        Assert.True(
            TemplateFootingHelper.IsCenterInsideTargetBlock(finalPos, current.End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
    }

    [Fact]
    public void AscendTemplate_AfterTraversePrepareJump_CompletesWithinTwentyTicks()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -1, max: 6);
        FlatWorldTestBuilder.ClearBox(world, -1, 79, -1, 6, 84, 2);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 1, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, 80, 0);
        FlatWorldTestBuilder.SetSolid(world, 3, 81, 0);

        var traverse = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.0, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var ascend = new PathSegment
        {
            Start = new Location(1.5, 80, 0.5),
            End = new Location(2.5, 81, 0.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(2.5, 81, 0.5),
            End = new Location(3.5, 82, 0.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.FinalStop
        };

        var physics = TemplateSimulationRunner.CreateGroundedPhysics(traverse.Start, yaw: 270f);

        TemplateState traverseState = TemplateSimulationRunner.Run(new WalkTemplate(traverse, ascend), physics, world, maxTicks: 80, out _);

        var template = new AscendTemplate(ascend, next);
        var input = new MovementInput();
        TemplateState state = TemplateState.InProgress;
        int elapsedTicks = 0;
        Location finalPos = ascend.Start;
        for (; elapsedTicks < 80; elapsedTicks++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            if (state != TemplateState.InProgress)
            {
                finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
                break;
            }

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Assert.Equal(TemplateState.Complete, traverseState);
        Assert.Equal(TemplateState.Complete, state);
        Assert.True(elapsedTicks <= 20, $"elapsedTicks={elapsedTicks} finalPos={finalPos} vel={physics.DeltaMovement}");
    }

    [Fact]
    public void AscendTemplate_PrepareJump_CompletesFromOppositeYawWithinTwentyTicks()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 338, max: 344);
        FlatWorldTestBuilder.ClearBox(world, 340, 80, 338, 344, 84, 342);
        FlatWorldTestBuilder.FillSolid(world, 341, 80, 339, 341, 80, 341);
        FlatWorldTestBuilder.FillSolid(world, 342, 81, 339, 342, 81, 341);

        var segment = new PathSegment
        {
            Start = new Location(340.5, 80, 340.5),
            End = new Location(341.5, 81, 340.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(341.5, 81, 340.5),
            End = new Location(342.5, 82, 340.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new AscendTemplate(segment, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
        var input = new MovementInput();
        TemplateState state = TemplateState.InProgress;
        int elapsedTicks = 0;
        Location finalPos = segment.Start;
        var trace = new List<string>();
        for (; elapsedTicks < 80; elapsedTicks++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            if (elapsedTicks < 20 || state != TemplateState.InProgress)
            {
                trace.Add(
                    $"tick={elapsedTicks} state={state} pos={pos} yaw={physics.Yaw:F1} vel={physics.DeltaMovement} " +
                    $"onGround={physics.OnGround} input(F={input.Forward},J={input.Jump},S={input.Sprint})");
            }
            if (state != TemplateState.InProgress)
            {
                finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
                break;
            }

            physics.ApplyInput(input);
            physics.Tick(world);
            finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        }

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(elapsedTicks <= 20, $"elapsedTicks={elapsedTicks} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
        Assert.True(
            TemplateFootingHelper.IsCenterInsideTargetBlock(finalPos, segment.End),
            $"elapsedTicks={elapsedTicks} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
    }

    [Fact]
    public void AscendTemplate_PrepareJump_SnapsYawImmediatelyFromOppositeYaw()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 338, max: 344);
        FlatWorldTestBuilder.ClearBox(world, 340, 80, 338, 344, 84, 342);
        FlatWorldTestBuilder.FillSolid(world, 341, 80, 339, 341, 80, 341);
        FlatWorldTestBuilder.FillSolid(world, 342, 81, 339, 342, 81, 341);

        var segment = new PathSegment
        {
            Start = new Location(340.5, 80, 340.5),
            End = new Location(341.5, 81, 340.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(341.5, 81, 340.5),
            End = new Location(342.5, 82, 340.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new AscendTemplate(segment, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
        var input = new MovementInput();

        TemplateState state = template.Tick(segment.Start, physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.InRange(physics.Yaw, 269.9f, 270.1f);
        Assert.True(input.Forward);
        Assert.True(input.Sprint);
        Assert.True(input.Jump);
    }

    [Fact]
    public void WalkTemplate_PrepareJump_FreezeForTurn_SnapsExitHeadingImmediately()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(0, 1, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(1.5, 80, 0.5),
            End = new Location(1.5, 80, 1.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(current, next);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.5, 80.0, 0.5),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 180f,
            Pitch = 0f
        };
        var input = new MovementInput();

        TemplateState state = template.Tick(new Location(1.5, 80, 0.5), physics, input, world);

        Assert.Equal(TemplateState.Complete, state);
        Assert.InRange(physics.Yaw, -0.1f, 0.1f);
        Assert.False(input.Forward);
        Assert.False(input.Sprint);
        Assert.False(input.Back);
    }

    [Fact]
    public void AscendTemplate_PrepareJump_CompletesFromOffCenterRunUpState()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 338, max: 344);
        FlatWorldTestBuilder.ClearBox(world, 340, 80, 338, 344, 84, 342);
        FlatWorldTestBuilder.FillSolid(world, 341, 80, 339, 341, 80, 341);
        FlatWorldTestBuilder.FillSolid(world, 342, 81, 339, 342, 81, 341);

        var segment = new PathSegment
        {
            Start = new Location(340.5, 80, 340.5),
            End = new Location(341.5, 81, 340.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(341.5, 81, 340.5),
            End = new Location(342.5, 82, 340.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new AscendTemplate(segment, next);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(340.25, 80.0, 340.25),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f,
            Pitch = 0f
        };
        var input = new MovementInput();
        TemplateState state = TemplateState.InProgress;
        Location finalPos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
        var trace = new List<string>();
        for (int tick = 0; tick < 80; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            if (tick < 20 || state != TemplateState.InProgress)
            {
                trace.Add(
                    $"tick={tick} state={state} pos={pos} yaw={physics.Yaw:F1} vel={physics.DeltaMovement} " +
                    $"onGround={physics.OnGround} input(F={input.Forward},J={input.Jump},S={input.Sprint})");
            }

            if (state != TemplateState.InProgress)
            {
                finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
                break;
            }

            physics.ApplyInput(input);
            physics.Tick(world);
            finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        }

        Assert.True(
            state == TemplateState.Complete,
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
        Assert.True(
            TemplateFootingHelper.IsCenterInsideTargetBlock(finalPos, segment.End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
    }

    [Fact]
    public void WalkTemplate_DiagonalPrepareJumpIntoAscend_CompletesFromTargetBlockEntry()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 418, max: 426);
        FlatWorldTestBuilder.ClearBox(world, 418, 79, 418, 426, 84, 424);
        FlatWorldTestBuilder.SetSolid(world, 420, 79, 420);
        FlatWorldTestBuilder.SetSolid(world, 421, 79, 421);
        FlatWorldTestBuilder.SetSolid(world, 422, 79, 422);
        FlatWorldTestBuilder.SetSolid(world, 423, 80, 422);
        FlatWorldTestBuilder.SetSolid(world, 424, 81, 422);

        var current = new PathSegment
        {
            Start = new Location(421.5, 80, 421.5),
            End = new Location(422.5, 80, 422.5),
            MoveType = MoveType.Diagonal,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.0, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(422.5, 80, 422.5),
            End = new Location(423.5, 81, 422.5),
            MoveType = MoveType.Ascend,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.0, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };

        var template = new WalkTemplate(current, next);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(422.25, 80.0, 422.25),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 315f,
            Pitch = 0f
        };
        var input = new MovementInput();
        TemplateState state = TemplateState.InProgress;
        Location finalPos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
        var trace = new List<string>();
        for (int tick = 0; tick < 80; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            if (tick < 20 || state != TemplateState.InProgress)
            {
                trace.Add(
                    $"tick={tick} state={state} pos={pos} yaw={physics.Yaw:F1} vel={physics.DeltaMovement} " +
                    $"onGround={physics.OnGround} input(F={input.Forward},B={input.Back},S={input.Sprint})");
            }

            if (state != TemplateState.InProgress)
            {
                finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
                break;
            }

            physics.ApplyInput(input);
            physics.Tick(world);
            finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        }

        Assert.True(
            state == TemplateState.Complete,
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
        Assert.True(
            TemplateFootingHelper.IsCenterInsideTargetBlock(finalPos, current.End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
    }

    [Fact]
    public void WalkTemplate_TurnIntoParkour_CompletesOnlyWhenTurnEntryIsSlowAndJumpReady()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 108, max: 128);

        var current = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(121.5, 80, 110.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.Turn,
            ExitHints = new PathTransitionHints(0, 1, 0.08, 0.16, false, true, true, true, 12)
        };
        var next = new PathSegment
        {
            Start = new Location(121.5, 80, 110.5),
            End = new Location(121.5, 80, 111.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(0, 1, 0.12, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };

        var template = new WalkTemplate(current, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);
        double horizontalSpeed = Math.Sqrt(physics.DeltaMovement.X * physics.DeltaMovement.X + physics.DeltaMovement.Z * physics.DeltaMovement.Z);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(
            TemplateFootingHelper.IsCenterInsideSupportStrip(finalPos, current.End, next.End),
            $"finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.InRange(horizontalSpeed, 0.08, 0.20);
    }

    [Fact]
    public void DescendTemplate_LandingRecovery_CompletesOnLandingBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        FlatWorldTestBuilder.ClearBox(world, 1, 79, 0, 1, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 1, 78, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 79, 0.5),
            MoveType = MoveType.Descend,
            ExitTransition = PathTransitionType.LandingRecovery
        };

        var template = new DescendTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 240, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }

    [Fact]
    public void DescendTemplate_FinalStop_WithWallAndMisalignedYaw_CompletesOnLandingBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 198, max: 204);
        FlatWorldTestBuilder.ClearBox(world, 198, 79, 198, 204, 84, 202);
        FlatWorldTestBuilder.FillSolid(world, 201, 79, 199, 203, 79, 201);
        FlatWorldTestBuilder.SetSolid(world, 200, 80, 200);
        FlatWorldTestBuilder.SetSolid(world, 200, 80, 199);
        FlatWorldTestBuilder.SetSolid(world, 201, 80, 199);
        FlatWorldTestBuilder.SetSolid(world, 202, 80, 199);
        FlatWorldTestBuilder.SetSolid(world, 201, 81, 199);
        FlatWorldTestBuilder.SetSolid(world, 202, 81, 199);

        var segment = new PathSegment
        {
            Start = new Location(200.5, 81, 200.5),
            End = new Location(201.5, 80, 200.5),
            MoveType = MoveType.Descend,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new DescendTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 0f);

        var input = new MovementInput();
        var trace = new List<string>();
        TemplateState state = TemplateState.InProgress;
        Location finalPos = segment.Start;
        for (int tick = 0; tick < 240; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            if (tick < 20 || state != TemplateState.InProgress || !physics.OnGround)
            {
                trace.Add($"tick={tick} state={state} pos={pos} vel={physics.DeltaMovement} onGround={physics.OnGround} input(F={input.Forward},B={input.Back},S={input.Sprint})");
            }

            if (state != TemplateState.InProgress)
            {
                finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
                break;
            }

            physics.ApplyInput(input);
            physics.Tick(world);
            finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        }

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }

    [Fact]
    public void DescendTemplate_AppliesAirBrake_WhenPlannerRequiresBrake()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -2, max: 4);
        FlatWorldTestBuilder.ClearBox(world, -2, 79, -2, 4, 84, 2);
        FlatWorldTestBuilder.SetSolid(world, 1, 79, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 81, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Descend,
            ExitTransition = PathTransitionType.LandingRecovery,
            ExitHints = new PathTransitionHints(1, 0, 0.0, 0.0, true, true, false, true, 12)
        };

        var template = new DescendTemplate(segment, null);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.38, 80.56, 0.5),
            DeltaMovement = new Vec3d(0.42, -0.22, 0.0),
            OnGround = false,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(segment, null, pos, physics, world);
        var input = new MovementInput();
        template.Tick(pos, physics, input, world);

        Assert.Equal(TransitionBrakingDecision.Brake, decision);
        Assert.True(input.Back, $"decision={decision} input(F={input.Forward},B={input.Back},S={input.Sprint})");
        Assert.False(input.Forward, $"decision={decision} input(F={input.Forward},B={input.Back},S={input.Sprint})");
    }
}
