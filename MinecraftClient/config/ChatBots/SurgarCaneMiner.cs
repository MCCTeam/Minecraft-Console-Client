//MCCScript 1.0

MCC.LoadBot(new SurgarCaneMiner());

//MCCScript Extensions

public class SurgarCaneMiner : ChatBot
{
    // === CONFIG - REPLACE SURGAR CANE LOCATION x y z VALUES HERE ===
    // You need to stand in front of the sugar cane
    Location SugarCane = new Location(x, y, z);
    // === END OF CONFIG ===

    public override void Initialize()
    {
        LogToConsole("Bot enabled!");
    }

    public override void Update()
    {
        Material blockType = GetWorld().GetBlock(SugarCane).Type;
        switch (blockType)
        {
            case Material.SugarCane:
                PlayerDigging(0, SugarCane, 1);
                Thread.Sleep(50);
                PlayerDigging(2, SugarCane, 1);
                break;
            default:
           
                break;
        }
    }
}