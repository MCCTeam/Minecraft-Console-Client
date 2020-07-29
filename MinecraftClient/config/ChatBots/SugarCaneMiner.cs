//MCCScript 1.0

MCC.LoadBot(new SugarCaneMiner());

//MCCScript Extensions

public class SugarCaneMiner : ChatBot
{
    // === CONFIG - REPLACE SURGAR CANE LOCATION x y z VALUES HERE ===
    // You need to stand in front of the sugar cane
    Location sugarCane = new Location(x, y, z);
    bool fullHeight = true;
    // === END OF CONFIG ===

    public override void Initialize()
    {
        LogToConsole("Bot enabled!");
    }

    public override void Update()
    {
        if (DetectSugarCane(sugarCane, fullHeight))
        {
            DigBlock(sugarCane);
        }
    }

    public bool DetectSugarCane(Location sugarCaneLoc, bool fullHeight)
    {
        Material blockType = GetWorld().GetBlock(sugarCaneLoc).Type;
        if (blockType == Material.SugarCane)
        {
            blockType = GetWorld().GetBlock(new Location(sugarCaneLoc.X, sugarCaneLoc.Y - 1, sugarCaneLoc.Z)).Type;
            if (blockType == Material.SugarCane)
            {
                blockType = GetWorld().GetBlock(new Location(sugarCaneLoc.X, sugarCaneLoc.Y - 2, sugarCaneLoc.Z)).Type;
                if (blockType != Material.SugarCane)
                {
                    if (!fullHeight)
                        return true;
                    blockType = GetWorld().GetBlock(new Location(sugarCaneLoc.X, sugarCaneLoc.Y + 1, sugarCaneLoc.Z)).Type;
                    if (blockType == Material.SugarCane)
                        return true;
                }
            }
        }
        return false;
    }
}
