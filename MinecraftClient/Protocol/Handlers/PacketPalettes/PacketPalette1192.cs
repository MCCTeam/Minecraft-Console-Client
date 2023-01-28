using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes
{
    public class PacketPalette1192 : PacketTypePalette
    {
        private readonly Dictionary<int, PacketTypesIn> typeIn = new()
        {
            { 0x00, PacketTypesIn.SpawnEntity },                // Changed in 1.19 (Wiki name: Spawn Entity) 
            { 0x01, PacketTypesIn.SpawnExperienceOrb },         // (Wiki name: Spawn Exeprience Orb)
            { 0x02, PacketTypesIn.SpawnPlayer },                //
            { 0x03, PacketTypesIn.EntityAnimation },            // (Wiki name: Entity Animation (clientbound))
            { 0x04, PacketTypesIn.Statistics },                 // (Wiki name: Award Statistics)
            { 0x05, PacketTypesIn.BlockChangedAck },            // Added 1.19 (Wiki name: Acknowledge Block Change)  
            { 0x06, PacketTypesIn.BlockBreakAnimation },        // (Wiki name: Set Block Destroy Stage)
            { 0x07, PacketTypesIn.BlockEntityData },            //
            { 0x08, PacketTypesIn.BlockAction },                //
            { 0x09, PacketTypesIn.BlockChange },                // (Wiki name: Block Update)
            { 0x0A, PacketTypesIn.BossBar },                    //
            { 0x0B, PacketTypesIn.ServerDifficulty },           // (Wiki name: Change Difficulty)
            { 0x0C, PacketTypesIn.ChatPreview },                // Added 1.19
            { 0x0D, PacketTypesIn.ClearTiles },                 //
            { 0x0E, PacketTypesIn.TabComplete },                // (Wiki name: Command Suggestions Response)
            { 0x0F, PacketTypesIn.DeclareCommands },            // (Wiki name: Commands)
            { 0x10, PacketTypesIn.CloseWindow },                // (Wiki name: Close Container (clientbound))
            { 0x11, PacketTypesIn.WindowItems },                // (Wiki name: Set Container Content)
            { 0x12, PacketTypesIn.WindowProperty },             // (Wiki name: Set Container Property)
            { 0x13, PacketTypesIn.SetSlot },                    // (Wiki name: Set Container Slot)
            { 0x14, PacketTypesIn.SetCooldown },                //
            { 0x15, PacketTypesIn.ChatSuggestions },            // Added 1.19.1
            { 0x16, PacketTypesIn.PluginMessage },              // (Wiki name: Plugin Message (clientbound))
            { 0x17, PacketTypesIn.NamedSoundEffect },           // Changed in 1.19 (Added "Speed" field) (Wiki name: Custom Sound Effect)  (No need to be implemented)
            { 0x18, PacketTypesIn.HideMessage },                // Added 1.19.1
            { 0x19, PacketTypesIn.Disconnect },                 //
            { 0x1A, PacketTypesIn.EntityStatus },               // (Wiki name: Entity Event)
            { 0x1B, PacketTypesIn.Explosion },                  // Changed in 1.19 (Location fields are now Double instead of Float) (Wiki name: Explosion) 
            { 0x1C, PacketTypesIn.UnloadChunk },                // (Wiki name: Forget Chunk)
            { 0x1D, PacketTypesIn.ChangeGameState },            // (Wiki name: Game Event)
            { 0x1E, PacketTypesIn.OpenHorseWindow },            // (Wiki name: Horse Screen Open)
            { 0x1F, PacketTypesIn.InitializeWorldBorder },      //
            { 0x20, PacketTypesIn.KeepAlive },                  //
            { 0x21, PacketTypesIn.ChunkData },                  //
            { 0x22, PacketTypesIn.Effect },                     // (Wiki name: Level Event)
            { 0x23, PacketTypesIn.Particle },                   // Changed in 1.19 ("Particle Data" field is now "Max Speed", it's the same Float data type) (Wiki name: Level Particle)  (No need to be implemented)
            { 0x24, PacketTypesIn.UpdateLight },                // (Wiki name: Light Update)
            { 0x25, PacketTypesIn.JoinGame },                   // Changed in 1.19 (lot's of changes) (Wiki name: Login (play)) 
            { 0x26, PacketTypesIn.MapData },                    // (Wiki name: Map Item Data)
            { 0x27, PacketTypesIn.TradeList },                  // (Wiki name: Merchant Offers)
            { 0x28, PacketTypesIn.EntityPosition },             // (Wiki name: Move Entity Position)
            { 0x29, PacketTypesIn.EntityPositionAndRotation },  // (Wiki name: Move Entity Position and Rotation)
            { 0x2A, PacketTypesIn.EntityRotation },             // (Wiki name: Move Entity Rotation)
            { 0x2B, PacketTypesIn.VehicleMove },                // (Wiki name: Move Vehicle)
            { 0x2C, PacketTypesIn.OpenBook },                   //
            { 0x2D, PacketTypesIn.OpenWindow },                 // (Wiki name: Open Screen)
            { 0x2E, PacketTypesIn.OpenSignEditor },             //
            { 0x2F, PacketTypesIn.Ping },                       // (Wiki name: Ping (play))
            { 0x30, PacketTypesIn.CraftRecipeResponse },        // (Wiki name: Place Ghost Recipe)
            { 0x31, PacketTypesIn.PlayerAbilities },            //
            { 0x32, PacketTypesIn.MessageHeader },              // Added 1.19.1
            { 0x33, PacketTypesIn.ChatMessage },                // Changed in 1.19 (Completely changed) (Wiki name: Player Chat Message)
            { 0x34, PacketTypesIn.EndCombatEvent },             // (Wiki name: Player Combat End)
            { 0x35, PacketTypesIn.EnterCombatEvent },           // (Wiki name: Player Combat Enter)
            { 0x36, PacketTypesIn.DeathCombatEvent },           // (Wiki name: Player Combat Kill)
            { 0x37, PacketTypesIn.PlayerInfo },                 // Changed in 1.19 (Heavy changes) 
            { 0x38, PacketTypesIn.FacePlayer },                 // (Wiki name: Player Look At)
            { 0x39, PacketTypesIn.PlayerPositionAndLook },      // (Wiki name: Player Position)
            { 0x3A, PacketTypesIn.UnlockRecipes },              // (Wiki name: Recipe)
            { 0x3B, PacketTypesIn.DestroyEntities },            // (Wiki name: Remove Entites)
            { 0x3C, PacketTypesIn.RemoveEntityEffect },         //
            { 0x3D, PacketTypesIn.ResourcePackSend },           // (Wiki name: Resource Pack)
            { 0x3E, PacketTypesIn.Respawn },                    // Changed in 1.19 (Heavy changes) 
            { 0x3F, PacketTypesIn.EntityHeadLook },             // (Wiki name: Rotate Head)
            { 0x40, PacketTypesIn.MultiBlockChange },           // (Wiki name: Sections Block Update)
            { 0x41, PacketTypesIn.SelectAdvancementTab },       //
            { 0x42, PacketTypesIn.ServerData },                 // Added in 1.19
            { 0x43, PacketTypesIn.ActionBar },                  // (Wiki name: Set Action Bar Text)
            { 0x44, PacketTypesIn.WorldBorderCenter },          // (Wiki name: Set Border Center)
            { 0x45, PacketTypesIn.WorldBorderLerpSize },        //
            { 0x46, PacketTypesIn.WorldBorderSize },            // (Wiki name: Set World Border Size)
            { 0x47, PacketTypesIn.WorldBorderWarningDelay },    // (Wiki name: Set World Border Warning Delay)
            { 0x48, PacketTypesIn.WorldBorderWarningReach },    // (Wiki name: Set Border Warning Distance)
            { 0x49, PacketTypesIn.Camera },                     // (Wiki name: Set Camera)
            { 0x4A, PacketTypesIn.HeldItemChange },             // (Wiki name: Set Carried Item (clientbound))
            { 0x4B, PacketTypesIn.UpdateViewPosition },         // (Wiki name: Set Chunk Cache Center)
            { 0x4C, PacketTypesIn.UpdateViewDistance },         // (Wiki name: Set Chunk Cache Radius)
            { 0x4D, PacketTypesIn.SpawnPosition },              // (Wiki name: Set Default Spawn Position)
            { 0x4E, PacketTypesIn.SetDisplayChatPreview },      // Added in 1.19 (Wiki name: Set Display Chat Preview)
            { 0x4F, PacketTypesIn.DisplayScoreboard },          // (Wiki name: Set Display Objective)
            { 0x50, PacketTypesIn.EntityMetadata },             // (Wiki name: Set Entity Metadata)
            { 0x51, PacketTypesIn.AttachEntity },               // (Wiki name: Set Entity Link)
            { 0x52, PacketTypesIn.EntityVelocity },             // (Wiki name: Set Entity Motion)
            { 0x53, PacketTypesIn.EntityEquipment },            // (Wiki name: Set Equipment)
            { 0x54, PacketTypesIn.SetExperience },              //
            { 0x55, PacketTypesIn.UpdateHealth },               // (Wiki name: Set Health)
            { 0x56, PacketTypesIn.ScoreboardObjective },        // (Wiki name: Set Objective)
            { 0x57, PacketTypesIn.SetPassengers },              //
            { 0x58, PacketTypesIn.Teams },                      // (Wiki name: Set Player Team)
            { 0x59, PacketTypesIn.UpdateScore },                // (Wiki name: Set Score)
            { 0x5A, PacketTypesIn.UpdateSimulationDistance },   // (Wiki name: Set Simulation Distance)
            { 0x5B, PacketTypesIn.SetTitleSubTitle },           // (Wiki name: Set Subtitle Test)
            { 0x5C, PacketTypesIn.TimeUpdate },                 // (Wiki name: Set Time)
            { 0x5D, PacketTypesIn.SetTitleText },               // (Wiki name: Set Title)
            { 0x5E, PacketTypesIn.SetTitleTime },               // (Wiki name: Set Titles Animation)
            { 0x5F, PacketTypesIn.EntitySoundEffect },          // (Wiki name: Sound Entity)
            { 0x60, PacketTypesIn.SoundEffect },                // Changed in 1.19 (Added "Seed" field) (Wiki name: Sound Effect)  (No need to be implemented)
            { 0x61, PacketTypesIn.StopSound },                  //
            { 0x62, PacketTypesIn.SystemChat },                 // Added in 1.19 (Wiki name: System Chat Message)
            { 0x63, PacketTypesIn.PlayerListHeaderAndFooter },  // (Wiki name: Tab List)
            { 0x64, PacketTypesIn.NBTQueryResponse },           // (Wiki name: Tab Query)
            { 0x65, PacketTypesIn.CollectItem },                // (Wiki name: Take Item Entity)
            { 0x66, PacketTypesIn.EntityTeleport },             // (Wiki name: Teleport Entity)
            { 0x67, PacketTypesIn.Advancements },               // (Wiki name: Update Advancements)
            { 0x68, PacketTypesIn.EntityProperties },           // (Wiki name: Update Attributes)
            { 0x69, PacketTypesIn.EntityEffect },               // Changed in 1.19 (Added "Has Factor Data" and "Factor Codec" fields) (Wiki name: Entity Effect) 
            { 0x6A, PacketTypesIn.DeclareRecipes },             // (Wiki name: Update Recipes)
            { 0x6B, PacketTypesIn.Tags },                       // (Wiki name: Update Tags)
        };

        private readonly Dictionary<int, PacketTypesOut> typeOut = new()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },             // (Wiki name: Confirm Teleportation)
            { 0x01, PacketTypesOut.QueryBlockNBT },               // (Wiki name: Query Block Entity Tag)
            { 0x02, PacketTypesOut.SetDifficulty },               // (Wiki name: Change Difficutly)
            { 0x03, PacketTypesOut.MessageAcknowledgment },       // Added in 1.19.1
            { 0x04, PacketTypesOut.ChatCommand },                 // Added in 1.19
            { 0x05, PacketTypesOut.ChatMessage },                 // Changed in 1.19 (Completely changed) (Wiki name: Chat)
            { 0x06, PacketTypesOut.ChatPreview },                 // Added in 1.19 (Wiki name: Chat Preview (serverbound))
            { 0x07, PacketTypesOut.ClientStatus },                // (Wiki name: Client Command)
            { 0x08, PacketTypesOut.ClientSettings },              // (Wiki name: Client Information)
            { 0x09, PacketTypesOut.TabComplete },                 // (Wiki name: Command Suggestions Request)
            { 0x0A, PacketTypesOut.ClickWindowButton },           // (Wiki name: Click Container Button)
            { 0x0B, PacketTypesOut.ClickWindow },                 // (Wiki name: Click Container)
            { 0x0C, PacketTypesOut.CloseWindow },                 // (Wiki name: Close Container (serverbound))
            { 0x0D, PacketTypesOut.PluginMessage },               // (Wiki name: Plugin Message (serverbound))
            { 0x0E, PacketTypesOut.EditBook },                    //
            { 0x0F, PacketTypesOut.EntityNBTRequest },            // (Wiki name: Query Entity Tag)
            { 0x10, PacketTypesOut.InteractEntity },              // (Wiki name: Interact)
            { 0x11, PacketTypesOut.GenerateStructure },           // (Wiki name: Jigsaw Generate)
            { 0x12, PacketTypesOut.KeepAlive },                   //
            { 0x13, PacketTypesOut.LockDifficulty },              //
            { 0x14, PacketTypesOut.PlayerPosition },              // (Wiki name: Move Player Position)
            { 0x15, PacketTypesOut.PlayerPositionAndRotation },   // (Wiki name: Set Player Position and Rotation)
            { 0x16, PacketTypesOut.PlayerRotation },              // (Wiki name: Set Player Rotation)
            { 0x17, PacketTypesOut.PlayerMovement },              // (Wiki name: Set Player On Ground)
            { 0x18, PacketTypesOut.VehicleMove },                 // (Wiki name: Move Vehicle (serverbound))
            { 0x19, PacketTypesOut.SteerBoat },                   // (Wiki name: Paddle Boat)
            { 0x1A, PacketTypesOut.PickItem },                    //
            { 0x1B, PacketTypesOut.CraftRecipeRequest },          // (Wiki name: Place recipe)
            { 0x1C, PacketTypesOut.PlayerAbilities },             //
            { 0x1D, PacketTypesOut.PlayerDigging },               // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Player Action) 
            { 0x1E, PacketTypesOut.EntityAction },                // (Wiki name: Player Command)
            { 0x1F, PacketTypesOut.SteerVehicle },                // (Wiki name: Player Input)
            { 0x20, PacketTypesOut.Pong },                        // (Wiki name: Pong (play))
            { 0x21, PacketTypesOut.SetDisplayedRecipe },          // (Wiki name: Recipe Book Change Settings)
            { 0x22, PacketTypesOut.SetRecipeBookState },          // (Wiki name: Recipe Book Seen Recipe)
            { 0x23, PacketTypesOut.NameItem },                    // (Wiki name: Rename Item)
            { 0x24, PacketTypesOut.ResourcePackStatus },          // (Wiki name: Resource Pack (serverbound))
            { 0x25, PacketTypesOut.AdvancementTab },              // (Wiki name: Seen Advancements)
            { 0x26, PacketTypesOut.SelectTrade },                 //
            { 0x27, PacketTypesOut.SetBeaconEffect },             // Changed in 1.19 (Added a "Secondary Effect Present" and "Secondary Effect" fields) (Wiki name: Set Beacon)  - (No need to be implemented)
            { 0x28, PacketTypesOut.HeldItemChange },              // (Wiki name: Set Carried Item (serverbound))
            { 0x29, PacketTypesOut.UpdateCommandBlock },          // (Wiki name: Set Command Block)
            { 0x2A, PacketTypesOut.UpdateCommandBlockMinecart },  //
            { 0x2B, PacketTypesOut.CreativeInventoryAction },     // (Wiki name: Set Creative Mode Slot)
            { 0x2C, PacketTypesOut.UpdateJigsawBlock },           // (Wiki name: Set Jigsaw Block)
            { 0x2D, PacketTypesOut.UpdateStructureBlock },        // (Wiki name: Set Structure Block)
            { 0x2E, PacketTypesOut.UpdateSign },                  // (Wiki name: Sign Update)
            { 0x2F, PacketTypesOut.Animation },                   // (Wiki name: Swing)
            { 0x30, PacketTypesOut.Spectate },                    // (Wiki name: Teleport To Entity)
            { 0x31, PacketTypesOut.PlayerBlockPlacement },        // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Use Item On) 
            { 0x32, PacketTypesOut.UseItem },                     // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Use Item) 
        };

        protected override Dictionary<int, PacketTypesIn> GetListIn()
        {
            return typeIn;
        }

        protected override Dictionary<int, PacketTypesOut> GetListOut()
        {
            return typeOut;
        }
    }
}