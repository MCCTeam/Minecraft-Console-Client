//MCCScript 1.0

MCC.LoadBot(new OreMiner());

//MCCScript Extensions

/// <summary>
/// This bot can mine blocks that auto-spawn at given locations
/// </summary>
public class OreMiner: ChatBot
{
    // === CONFIG - REPLACE BLOCK LOCATION x y z VALUES HERE ===
    List<Location> location = new List<Location>()
    {
        new Location(x, y, z),
        new Location(x2, y2, z2),
        new Location(x3, y3, z3),
        // Add more here
    };
    // === END OF CONFIG ===
    int index = 0;

    public override void Initialize()
    {
        LogToConsole("Bot enabled!");
    }

    public override void Update()
    {
        Material blockType = GetWorld().GetBlock(location[index]).Type;
        switch (blockType)
        {
            //Adjust here block types to mine
            case Material.DiamondOre:
            case Material.EmeraldOre:
            case Material.GoldOre:
            case Material.IronOre:
            case Material.CoalOre:
            case Material.LapisOre:
            case Material.RedstoneOre:
            case Material.NetherQuartzOre:
                DigBlock(location[index]);
                break;
        }
        index++;
        if (index >= location.Count)
            index = 0;
    }
}
