//MCCScript 1.0

MCC.LoadBot(new CobblestoneMiner());

//MCCScript Extensions

public class CobblestoneMiner: ChatBot
{
    // === CONFIG - REPLACE COBBLESTONE LOCATION x y z VALUES HERE ===
    // You need to stand in front of the cobblestone block to mine
    // Also make sure the Cobblestone will regenerate e.g. using water and lava
    Location cobblestone = new Location(x, y, z);
    // === END OF CONFIG ===

    public override void Initialize()
    {
        LogToConsole("Bot enabled!");
    }

    public override void Update()
    {
        Material blockType = GetWorld().GetBlock(cobblestone).Type;
        switch (blockType)
        {
            case Material.Stone:
                DigBlock(cobblestone);
                break;
            case Material.Cobblestone:
                DigBlock(cobblestone);
                break;
        }
    }
}
