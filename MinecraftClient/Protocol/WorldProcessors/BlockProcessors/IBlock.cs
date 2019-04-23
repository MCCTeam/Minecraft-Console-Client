namespace MinecraftClient.Protocol.WorldProcessors.BlockProcessors
{
    public interface IBlock
    {
        bool CanHarmPlayers();
        bool IsSolid();
        bool IsLiquid();
    }
}