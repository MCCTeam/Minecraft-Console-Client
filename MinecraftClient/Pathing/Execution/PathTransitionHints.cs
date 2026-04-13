namespace MinecraftClient.Pathing.Execution
{
    public sealed record PathTransitionHints(
        int DesiredHeadingX,
        int DesiredHeadingZ,
        double MinExitSpeed,
        double MaxExitSpeed,
        bool RequireStableFooting,
        bool RequireGrounded,
        bool RequireJumpReady,
        bool AllowAirBrake,
        int HorizonTicks)
    {
        public static PathTransitionHints Default { get; } = new(
            DesiredHeadingX: 0,
            DesiredHeadingZ: 0,
            MinExitSpeed: 0.0,
            MaxExitSpeed: double.PositiveInfinity,
            RequireStableFooting: false,
            RequireGrounded: false,
            RequireJumpReady: false,
            AllowAirBrake: false,
            HorizonTicks: 8);
    }
}
