//MCCScript 1.0

MCC.LoadBot(new TreeFarmer());

//MCCScript Extensions

public class TreeFarmer : ChatBot
{
    // You need to stand in front of the sapling and put items in your inventory hotbar:
    // - First slot on the left (Slot 0): Axe for digging the tree
    // - Second slot on the left (Slot 1): Sapling stack for planting the tree
    // Then set sapling location below, login with MCC and load this script

    // === CONFIG - REPLACE SAPLING LOCATION x y z VALUES HERE ===
    Location sapling = new Location(x, y, z);
    // === END OF CONFIG ===

    public override void Update()
    {
        Material blockType = GetWorld().GetBlock(sapling).Type;
        switch (blockType)
        {
            case Material.OakSapling:
                // Still a sapling, nothing to do
                break;
            case Material.OakLog:
                // Tree has grown, dig 4 blocks
                ChangeSlot(0);
                DigBlock(sapling);
                Thread.Sleep(100);
                // 1
                DigBlock(new Location(sapling.X, sapling.Y + 1, sapling.Z));
                Thread.Sleep(100);
                // 2
                DigBlock(new Location(sapling.X, sapling.Y + 2, sapling.Z));
                Thread.Sleep(100);
                // 3
                DigBlock(new Location(sapling.X, sapling.Y + 3, sapling.Z));
                Thread.Sleep(100);
                // 4
                DigBlock(new Location(sapling.X, sapling.Y + 4, sapling.Z);
                Thread.Sleep(100);
                break;
            case Material.Air:
                // No tree, plant something
                ChangeSlot(1);
                SendPlaceBlock(sapling);
                break;
            default:
                // Other block, cannot grow trees here
                LogToConsole("Block at " + sapling + " is not a sapling: " + blockType + "...");
                break;
        }
    }
    public override void Initialize()
    {

    }
}
