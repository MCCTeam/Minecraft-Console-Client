using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes;

public class PacketPalette1204 : PacketTypePalette
    {
        private readonly Dictionary<int, PacketTypesIn> typeIn = new()
        {
            { 0x00, PacketTypesIn.Bundle },                     // Added in 1.19.4
            { 0x01, PacketTypesIn.SpawnEntity },                // Changed in 1.19 (Wiki name: Spawn Entity) 
            { 0x02, PacketTypesIn.SpawnExperienceOrb },         // (Wiki name: Spawn Exeprience Orb)
            { 0x03, PacketTypesIn.EntityAnimation },            // (Wiki name: Entity Animation (clientbound))
            { 0x04, PacketTypesIn.Statistics },                 // (Wiki name: Award Statistics)
            { 0x05, PacketTypesIn.BlockChangedAck },            // Added 1.19 (Wiki name: Acknowledge Block Change)  
            { 0x06, PacketTypesIn.BlockBreakAnimation },        // (Wiki name: Set Block Destroy Stage)
            { 0x07, PacketTypesIn.BlockEntityData },            //
            { 0x08, PacketTypesIn.BlockAction },                //
            { 0x09, PacketTypesIn.BlockChange },                // (Wiki name: Block Update)
            { 0x0A, PacketTypesIn.BossBar },                    //
            { 0x0B, PacketTypesIn.ServerDifficulty },           // (Wiki name: Change Difficulty)
            { 0x0C, PacketTypesIn.ChunkBatchFinished },         // Added in 1.20.2  
            { 0x0D, PacketTypesIn.ChunkBatchStarted },          // Added in 1.20.2  
            { 0x0E, PacketTypesIn.ChunksBiomes },               // Added in 1.19.4
            { 0x0F, PacketTypesIn.ClearTiles },                 //
            { 0x10, PacketTypesIn.TabComplete },                // (Wiki name: Command Suggestions Response)
            { 0x11, PacketTypesIn.DeclareCommands },            // (Wiki name: Commands)
            { 0x12, PacketTypesIn.CloseWindow },                // (Wiki name: Close Container (clientbound))
            { 0x13, PacketTypesIn.WindowItems },                // (Wiki name: Set Container Content)
            { 0x14, PacketTypesIn.WindowProperty },             // (Wiki name: Set Container Property)
            { 0x15, PacketTypesIn.SetSlot },                    // (Wiki name: Set Container Slot)
            { 0x16, PacketTypesIn.SetCooldown },                //
            { 0x17, PacketTypesIn.ChatSuggestions },            // Added in 1.19.1
            { 0x18, PacketTypesIn.PluginMessage },              // (Wiki name: Plugin Message (clientbound))
            { 0x19, PacketTypesIn.DamageEvent },                // Added in 1.19.4
            { 0x1A, PacketTypesIn.HideMessage },                // Added in 1.19.1
            { 0x1B, PacketTypesIn.Disconnect },                 //
            { 0x1C, PacketTypesIn.ProfilelessChatMessage },     // Added in 1.19.3 (Wiki name: Disguised Chat Message)
            { 0x1D, PacketTypesIn.EntityStatus },               // (Wiki name: Entity Event)
            { 0x1E, PacketTypesIn.Explosion },                  // Changed in 1.19 (Location fields are now Double instead of Float) (Wiki name: Explosion) 
            { 0x1F, PacketTypesIn.UnloadChunk },                // (Wiki name: Forget Chunk)
            { 0x20, PacketTypesIn.ChangeGameState },            // (Wiki name: Game Event)
            { 0x21, PacketTypesIn.OpenHorseWindow },            // (Wiki name: Horse Screen Open)
            { 0x22, PacketTypesIn.HurtAnimation },              // Added in 1.19.4
            { 0x23, PacketTypesIn.InitializeWorldBorder },      //
            { 0x24, PacketTypesIn.KeepAlive },                  //
            { 0x25, PacketTypesIn.ChunkData },                  //
            { 0x26, PacketTypesIn.Effect },                     // (Wiki name: World Event)
            { 0x27, PacketTypesIn.Particle },                   // Changed in 1.19 (Wiki name: Level Particle)  (No need to be implemented)
            { 0x28, PacketTypesIn.UpdateLight },                // (Wiki name: Light Update)
            { 0x29, PacketTypesIn.JoinGame },                   // Changed in 1.20.2 (Wiki name: Login (play)) 
            { 0x2A, PacketTypesIn.MapData },                    // (Wiki name: Map Item Data)
            { 0x2B, PacketTypesIn.TradeList },                  // (Wiki name: Merchant Offers)
            { 0x2C, PacketTypesIn.EntityPosition },             // (Wiki name: Move Entity Position)
            { 0x2D, PacketTypesIn.EntityPositionAndRotation },  // (Wiki name: Move Entity Position and Rotation)
            { 0x2E, PacketTypesIn.EntityRotation },             // (Wiki name: Move Entity Rotation)
            { 0x2F, PacketTypesIn.VehicleMove },                // (Wiki name: Move Vehicle)
            { 0x30, PacketTypesIn.OpenBook },                   //
            { 0x31, PacketTypesIn.OpenWindow },                 // (Wiki name: Open Screen)
            { 0x32, PacketTypesIn.OpenSignEditor },             //
            { 0x33, PacketTypesIn.Ping },                       // (Wiki name: Ping (play))
            { 0x34, PacketTypesIn.PingResponse },               // Added in 1.20.2 
            { 0x35, PacketTypesIn.CraftRecipeResponse },        // (Wiki name: Place Ghost Recipe)
            { 0x36, PacketTypesIn.PlayerAbilities },            //
            { 0x37, PacketTypesIn.ChatMessage },                // Changed in 1.19 (Completely changed) (Wiki name: Player Chat Message)
            { 0x38, PacketTypesIn.EndCombatEvent },             // (Wiki name: End Combat)
            { 0x39, PacketTypesIn.EnterCombatEvent },           // (Wiki name: Enter Combat)
            { 0x3A, PacketTypesIn.DeathCombatEvent },           // (Wiki name: Combat Death)
            { 0x3B, PacketTypesIn.PlayerRemove },               // Added in 1.19.3 (Not used)
            { 0x3C, PacketTypesIn.PlayerInfo },                 // Changed in 1.19 (Heavy changes) 
            { 0x3D, PacketTypesIn.FacePlayer },                 // (Wiki name: Player Look At)
            { 0x3E, PacketTypesIn.PlayerPositionAndLook },      // (Wiki name: Synchronize Player Position)
            { 0x3F, PacketTypesIn.UnlockRecipes },              // (Wiki name: Update Recipe Book)
            { 0x40, PacketTypesIn.DestroyEntities },            // (Wiki name: Remove Entites)
            { 0x41, PacketTypesIn.RemoveEntityEffect },         //
            { 0x42, PacketTypesIn.ResetScore },                 // Added in 1.20.3
            { 0x43, PacketTypesIn.RemoveResourcePack },         // Added in 1.20.3
            { 0x44, PacketTypesIn.ResourcePackSend },           // (Wiki name: Add Resource pack (play))
            { 0x45, PacketTypesIn.Respawn },                    // Changed in 1.20.2 
            { 0x46, PacketTypesIn.EntityHeadLook },             // (Wiki name: Set Head Rotation)
            { 0x47, PacketTypesIn.MultiBlockChange },           // (Wiki name: Update Section Blocks)
            { 0x48, PacketTypesIn.SelectAdvancementTab },       //
            { 0x49, PacketTypesIn.ServerData },                 // Added in 1.19
            { 0x4A, PacketTypesIn.ActionBar },                  // (Wiki name: Set Action Bar Text)
            { 0x4B, PacketTypesIn.WorldBorderCenter },          // (Wiki name: Set Border Center)
            { 0x4C, PacketTypesIn.WorldBorderLerpSize },        //
            { 0x4D, PacketTypesIn.WorldBorderSize },            // (Wiki name: Set World Border Size)
            { 0x4E, PacketTypesIn.WorldBorderWarningDelay },    // (Wiki name: Set World Border Warning Delay)
            { 0x4F, PacketTypesIn.WorldBorderWarningReach },    // (Wiki name: Set Border Warning Distance)
            { 0x50, PacketTypesIn.Camera },                     // (Wiki name: Set Camera)
            { 0x51, PacketTypesIn.HeldItemChange },             // (Wiki name: Set Held Item)
            { 0x52, PacketTypesIn.UpdateViewPosition },         // (Wiki name: Set Center Chunk)
            { 0x53, PacketTypesIn.UpdateViewDistance },         // (Wiki name: Set Render Distance)
            { 0x54, PacketTypesIn.SpawnPosition },              // (Wiki name: Set Default Spawn Position)
            { 0x55, PacketTypesIn.DisplayScoreboard },          // (Wiki name: Set Display Objective)
            { 0x56, PacketTypesIn.EntityMetadata },             // (Wiki name: Set Entity Metadata)
            { 0x57, PacketTypesIn.AttachEntity },               // (Wiki name: Link Entities)
            { 0x58, PacketTypesIn.EntityVelocity },             // (Wiki name: Set Entity Velocity)
            { 0x59, PacketTypesIn.EntityEquipment },            // (Wiki name: Set Equipment)
            { 0x5A, PacketTypesIn.SetExperience },              // Changed in 1.20.2 
            { 0x5B, PacketTypesIn.UpdateHealth },               // (Wiki name: Set Health)
            { 0x5C, PacketTypesIn.ScoreboardObjective },        // (Wiki name: Update Objectives) - Changed in 1.20.3
            { 0x5D, PacketTypesIn.SetPassengers },              //
            { 0x5E, PacketTypesIn.Teams },                      // (Wiki name: Update Teams)
            { 0x5F, PacketTypesIn.UpdateScore },                // (Wiki name: Update Score)
            { 0x60, PacketTypesIn.UpdateSimulationDistance },   // (Wiki name: Set Simulation Distance)
            { 0x61, PacketTypesIn.SetTitleSubTitle },           // (Wiki name: Set Subtitle Test)
            { 0x62, PacketTypesIn.TimeUpdate },                 // (Wiki name: Set Time)
            { 0x63, PacketTypesIn.SetTitleText },               // (Wiki name: Set Title)
            { 0x64, PacketTypesIn.SetTitleTime },               // (Wiki name: Set Title Animation Times)
            { 0x65, PacketTypesIn.EntitySoundEffect },          // (Wiki name: Sound Entity)
            { 0x66, PacketTypesIn.SoundEffect },                // Changed in 1.19 (Added "Seed" field) (Wiki name: Sound Effect)  (No need to be implemented)
            { 0x67, PacketTypesIn.StartConfiguration },         // Added in 1.20.2 
            { 0x68, PacketTypesIn.StopSound },                  //
            { 0x69, PacketTypesIn.SystemChat },                 // Added in 1.19 (Wiki name: System Chat Message)
            { 0x6A, PacketTypesIn.PlayerListHeaderAndFooter },  // (Wiki name: Set Tab List Header And Footer)
            { 0x6B, PacketTypesIn.NBTQueryResponse },           // (Wiki name: Tag Query Response)
            { 0x6C, PacketTypesIn.CollectItem },                // (Wiki name: Pickup Item)
            { 0x6D, PacketTypesIn.EntityTeleport },             // (Wiki name: Teleport Entity)
            { 0x6E, PacketTypesIn.SetTickingState },            // Added in 1.20.3
            { 0x6F, PacketTypesIn.StepTick },                   // Added in 1.20.3
            { 0x70, PacketTypesIn.Advancements },               // (Wiki name: Update Advancements) (Unused)
            { 0x71, PacketTypesIn.EntityProperties },           // (Wiki name: Update Attributes)
            { 0x72, PacketTypesIn.EntityEffect },               // Changed in 1.19 (Added "Has Factor Data" and "Factor Codec" fields) (Wiki name: Entity Effect) 
            { 0x73, PacketTypesIn.DeclareRecipes },             // (Wiki name: Update Recipes) (Unused)
            { 0x74, PacketTypesIn.Tags },                       // (Wiki name: Update Tags)
        };

        private readonly Dictionary<int, PacketTypesOut> typeOut = new()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },             // (Wiki name: Confirm Teleportation)
            { 0x01, PacketTypesOut.QueryBlockNBT },               // (Wiki name: Query Block Entity Tag)
            { 0x02, PacketTypesOut.SetDifficulty },               // (Wiki name: Change Difficulty)
            { 0x03, PacketTypesOut.MessageAcknowledgment },       // Added in 1.19.1
            { 0x04, PacketTypesOut.ChatCommand },                 // Added in 1.19
            { 0x05, PacketTypesOut.ChatMessage },                 // Changed in 1.19 (Completely changed) (Wiki name: Chat)
            { 0x06, PacketTypesOut.PlayerSession },               // Added in 1.19.3
            { 0x07, PacketTypesOut.ChunkBatchReceived },          // Added in 1.20.2 
            { 0x08, PacketTypesOut.ClientStatus },                // (Wiki name: Client Command)
            { 0x09, PacketTypesOut.ClientSettings },              // (Wiki name: Client Information)
            { 0x0A, PacketTypesOut.TabComplete },                 // (Wiki name: Command Suggestions Request)
            { 0x0B, PacketTypesOut.AcknowledgeConfiguration },    // Added in 1.20.2 
            { 0x0C, PacketTypesOut.ClickWindowButton },           // (Wiki name: Click Container Button)
            { 0x0D, PacketTypesOut.ClickWindow },                 // (Wiki name: Click Container)
            { 0x0E, PacketTypesOut.CloseWindow },                 // (Wiki name: Close Container (serverbound))
            { 0x0F, PacketTypesOut.ChangeContainerSlotState },    // Added in 1.20.3
            { 0x10, PacketTypesOut.PluginMessage },               // (Wiki name: Serverbound Plugin Message)
            { 0x11, PacketTypesOut.EditBook },                    //
            { 0x12, PacketTypesOut.EntityNBTRequest },            // (Wiki name: Query Entity Tag)
            { 0x13, PacketTypesOut.InteractEntity },              // (Wiki name: Interact)
            { 0x14, PacketTypesOut.GenerateStructure },           // (Wiki name: Jigsaw Generate)
            { 0x15, PacketTypesOut.KeepAlive },                   // (Wiki name: Serverbound Keep Alive (play))
            { 0x16, PacketTypesOut.LockDifficulty },              //
            { 0x17, PacketTypesOut.PlayerPosition },              // (Wiki name: Move Player Position)
            { 0x18, PacketTypesOut.PlayerPositionAndRotation },   // (Wiki name: Set Player Position and Rotation)
            { 0x19, PacketTypesOut.PlayerRotation },              // (Wiki name: Set Player Rotation)
            { 0x1A, PacketTypesOut.PlayerMovement },              // (Wiki name: Set Player On Ground)
            { 0x1B, PacketTypesOut.VehicleMove },                 // (Wiki name: Move Vehicle (serverbound))
            { 0x1C, PacketTypesOut.SteerBoat },                   // (Wiki name: Paddle Boat)
            { 0x1D, PacketTypesOut.PickItem },                    //
            { 0x1E, PacketTypesOut.PingRequest },                 // Added in 1.20.2 
            { 0x1F, PacketTypesOut.CraftRecipeRequest },          // (Wiki name: Place recipe)
            { 0x20, PacketTypesOut.PlayerAbilities },             //
            { 0x21, PacketTypesOut.PlayerDigging },               // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Player Action) 
            { 0x22, PacketTypesOut.EntityAction },                // (Wiki name: Player Command)
            { 0x23, PacketTypesOut.SteerVehicle },                // (Wiki name: Player Input)
            { 0x24, PacketTypesOut.Pong },                        // (Wiki name: Pong (play))
            { 0x25, PacketTypesOut.SetDisplayedRecipe },          // (Wiki name: Recipe Book Change Settings)
            { 0x26, PacketTypesOut.SetRecipeBookState },          // (Wiki name: Recipe Book Seen Recipe)
            { 0x27, PacketTypesOut.NameItem },                    // (Wiki name: Rename Item)
            { 0x28, PacketTypesOut.ResourcePackStatus },          // (Wiki name: Resource Pack (serverbound))
            { 0x29, PacketTypesOut.AdvancementTab },              // (Wiki name: Seen Advancements)
            { 0x2A, PacketTypesOut.SelectTrade },                 //
            { 0x2B, PacketTypesOut.SetBeaconEffect },             // Changed in 1.19 (No need to be implemented yet)
            { 0x2C, PacketTypesOut.HeldItemChange },              // (Wiki name: Set Carried Item (serverbound))
            { 0x2D, PacketTypesOut.UpdateCommandBlock },          // (Wiki name: Program Command Block)
            { 0x2E, PacketTypesOut.UpdateCommandBlockMinecart },  // (Wiki name: Program Command Block Minecart)
            { 0x2F, PacketTypesOut.CreativeInventoryAction },     // (Wiki name: Set Creative Mode Slot)
            { 0x30, PacketTypesOut.UpdateJigsawBlock },           // (Wiki name: Program Jigsaw Block)
            { 0x31, PacketTypesOut.UpdateStructureBlock },        // (Wiki name: Program Structure Block)
            { 0x32, PacketTypesOut.UpdateSign },                  // (Wiki name: Update Sign)
            { 0x33, PacketTypesOut.Animation },                   // (Wiki name: Swing Arm)
            { 0x34, PacketTypesOut.Spectate },                    // (Wiki name: Teleport To Entity)
            { 0x35, PacketTypesOut.PlayerBlockPlacement },        // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Use Item On) 
            { 0x36, PacketTypesOut.UseItem },                     // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Use Item) 
        };

        private readonly Dictionary<int, ConfigurationPacketTypesIn> configurationTypesIn = new()
        {
            { 0x00, ConfigurationPacketTypesIn.PluginMessage },
            { 0x01, ConfigurationPacketTypesIn.Disconnect },
            { 0x02, ConfigurationPacketTypesIn.FinishConfiguration },
            { 0x03, ConfigurationPacketTypesIn.KeepAlive },
            { 0x04, ConfigurationPacketTypesIn.Ping },
            { 0x05, ConfigurationPacketTypesIn.RegistryData },
            { 0x06, ConfigurationPacketTypesIn.RemoveResourcePack },
            { 0x07, ConfigurationPacketTypesIn.ResourcePack },
            { 0x08, ConfigurationPacketTypesIn.FeatureFlags },
            { 0x09, ConfigurationPacketTypesIn.UpdateTags },
        };

        private readonly Dictionary<int, ConfigurationPacketTypesOut> configurationTypesOut = new()
        {
            { 0x00, ConfigurationPacketTypesOut.ClientInformation },
            { 0x01, ConfigurationPacketTypesOut.PluginMessage },
            { 0x02, ConfigurationPacketTypesOut.FinishConfiguration },
            { 0x03, ConfigurationPacketTypesOut.KeepAlive },
            { 0x04, ConfigurationPacketTypesOut.Pong },
            { 0x05, ConfigurationPacketTypesOut.ResourcePackResponse }
        };
        
        protected override Dictionary<int, PacketTypesIn> GetListIn() => typeIn;
        protected override Dictionary<int, PacketTypesOut> GetListOut() => typeOut;
        protected override Dictionary<int, ConfigurationPacketTypesIn> GetConfigurationListIn() => configurationTypesIn!;
        protected override Dictionary<int, ConfigurationPacketTypesOut> GetConfigurationListOut() => configurationTypesOut!;
    }