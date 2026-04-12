namespace MinecraftClient.Pathing.Execution
{
    public readonly record struct TransitionBrakingDecision(bool HoldForward, bool HoldSprint, bool HoldBack)
    {
        public static TransitionBrakingDecision CarryMomentum(bool preserveSprint) =>
            new(true, preserveSprint, false);

        public static TransitionBrakingDecision Coast =>
            new(false, false, false);

        public static TransitionBrakingDecision Brake =>
            new(false, false, true);
    }
}
