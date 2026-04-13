namespace MinecraftClient.Pathing.Goals
{
    public interface IGoal
    {
        bool IsInGoal(int x, int y, int z);
        double Heuristic(int x, int y, int z);
    }
}
