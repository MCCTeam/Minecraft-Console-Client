namespace MinecraftClient.Mapping
{
    public static class DirectionExtensions
    {
        public static Direction GetOpposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.SouthEast:
                    return Direction.NorthEast;
                case Direction.SouthWest:
                    return Direction.NorthWest;

                case Direction.NorthEast:
                    return Direction.SouthEast;
                case Direction.NorthWest:
                    return Direction.SouthWest;

                case Direction.West:
                    return Direction.East;
                case Direction.East:
                    return Direction.West;

                case Direction.North:
                    return Direction.South;
                case Direction.South:
                    return Direction.North;

                case Direction.Down:
                    return Direction.Up;
                case Direction.Up:
                    return Direction.Down;
                default:
                    return Direction.Up;

            }
        }
    }
}
