namespace MinecraftClient.Pathing.Core
{
    public readonly record struct EntryPreparationState(
        EntryPreparationKind Kind,
        int OriginX,
        int OriginY,
        int OriginZ,
        int ForwardX,
        int ForwardZ,
        byte RequiredSteps,
        byte BackwardSteps,
        byte ReturnSteps)
    {
        public static EntryPreparationState None => default;

        public bool IsNone => Kind == EntryPreparationKind.None;

        public bool IsPrepared =>
            Kind != EntryPreparationKind.None &&
            BackwardSteps == RequiredSteps &&
            ReturnSteps == RequiredSteps;

        public EntryPreparationState AdvanceBackward() =>
            this with { BackwardSteps = (byte)(BackwardSteps + 1) };

        public EntryPreparationState AdvanceReturn() =>
            this with { ReturnSteps = (byte)(ReturnSteps + 1) };
    }
}
