namespace MinecraftClient.Protocol.WorldProcessors.BlockProcessors._114Pre5
{
    internal class Block114Pre5 : IBlock
    {
        private readonly Material _type;

        public Block114Pre5(short id)
        {
            _type = !MaterialMapping.Mapping.TryGetValue(id, out var mat) ? Material.Unknown : mat;
        }
        
        public Block114Pre5(Material mat)
        {
            _type = mat;
        }

        public bool CanHarmPlayers()
        {
            return _type == Material.CanHarm || _type == Material.Lava;
        }

        public bool IsSolid()
        {
            return _type == Material.Solid || _type == Material.Walkable || _type == Material.Undestroyable ||
                   _type == Material.Ore || _type == Material.HasInterface || _type == Material.CanUse ||
                   _type == Material.Bed;
        }

        public bool IsLiquid()
        {
            return _type == Material.Water || _type == Material.Lava;
        }
    }
}