using System;

namespace MinecraftClient.Mapping;

public static class DirectionExtensions
{
    public static Direction GetOpposite(this Direction direction) => direction switch
    {
        Direction.SouthEast => Direction.NorthEast,
        Direction.SouthWest => Direction.NorthWest,
        Direction.NorthEast => Direction.SouthEast,
        Direction.NorthWest => Direction.SouthWest,
        Direction.West => Direction.East,
        Direction.East => Direction.West,
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.Down => Direction.Up,
        Direction.Up => Direction.Down,
        _ => Direction.Up,
    };

    public static Direction[] HORIZONTAL =
    [
        Direction.South,
        Direction.West,
        Direction.North,
        Direction.East,
    ];

    public static Direction FromRotation(double rotation)
    {
        double floor = Math.Floor((rotation / 90.0) + 0.5);
        int value = (int)floor & 3;

        return FromHorizontal(value);
    }

    public static Direction FromHorizontal(int value)
    {
        return HORIZONTAL[Math.Abs(value % HORIZONTAL.Length)];
    }
}
