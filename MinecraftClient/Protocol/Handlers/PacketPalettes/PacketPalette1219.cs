using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes;

public class PacketPalette1219 : PacketTypePalette
    {
        private readonly Dictionary<int, PacketTypesIn> typeIn = new()
        {
            { 0x00, PacketTypesIn.Bundle },                     // Bundle delimiter
            { 0x01, PacketTypesIn.SpawnEntity },                // Add Entity
            { 0x02, PacketTypesIn.EntityAnimation },            // Animate
            { 0x03, PacketTypesIn.Statistics },                 // Award Stats
            { 0x04, PacketTypesIn.BlockChangedAck },            // Block Changed Ack
            { 0x05, PacketTypesIn.BlockBreakAnimation },        // Block Destruction
            { 0x06, PacketTypesIn.BlockEntityData },            // Block Entity Data
            { 0x07, PacketTypesIn.BlockAction },                // Block Event
            { 0x08, PacketTypesIn.BlockChange },                // Block Update
            { 0x09, PacketTypesIn.BossBar },                    // Boss Event
            { 0x0A, PacketTypesIn.ServerDifficulty },           // Change Difficulty
            { 0x0B, PacketTypesIn.ChunkBatchFinished },         // Chunk Batch Finished
            { 0x0C, PacketTypesIn.ChunkBatchStarted },          // Chunk Batch Start
            { 0x0D, PacketTypesIn.ChunksBiomes },               // Chunks Biomes
            { 0x0E, PacketTypesIn.ClearTiles },                 // Clear Titles
            { 0x0F, PacketTypesIn.TabComplete },                // Command Suggestions
            { 0x10, PacketTypesIn.DeclareCommands },            // Commands
            { 0x11, PacketTypesIn.CloseWindow },                // Container Close
            { 0x12, PacketTypesIn.WindowItems },                // Container Set Content
            { 0x13, PacketTypesIn.WindowProperty },             // Container Set Data
            { 0x14, PacketTypesIn.SetSlot },                    // Container Set Slot
            { 0x15, PacketTypesIn.CookieRequest },              // Cookie Request
            { 0x16, PacketTypesIn.SetCooldown },                // Cooldown
            { 0x17, PacketTypesIn.ChatSuggestions },            // Custom Chat Completions
            { 0x18, PacketTypesIn.PluginMessage },              // Custom Payload
            { 0x19, PacketTypesIn.DamageEvent },                // Damage Event
            { 0x1A, PacketTypesIn.DebugBlockValue },            // Debug Block Value (new in 1.21.9)
            { 0x1B, PacketTypesIn.DebugChunkValue },            // Debug Chunk Value (new in 1.21.9)
            { 0x1C, PacketTypesIn.DebugEntityValue },           // Debug Entity Value (new in 1.21.9)
            { 0x1D, PacketTypesIn.DebugEvent },                 // Debug Event (new in 1.21.9)
            { 0x1E, PacketTypesIn.DebugSample },                // Debug Sample
            { 0x1F, PacketTypesIn.HideMessage },                // Delete Chat
            { 0x20, PacketTypesIn.Disconnect },                 // Disconnect
            { 0x21, PacketTypesIn.ProfilelessChatMessage },     // Disguised Chat
            { 0x22, PacketTypesIn.EntityStatus },               // Entity Event
            { 0x23, PacketTypesIn.EntityPositionSync },         // Entity Position Sync
            { 0x24, PacketTypesIn.Explosion },                  // Explode
            { 0x25, PacketTypesIn.UnloadChunk },                // Forget Level Chunk
            { 0x26, PacketTypesIn.ChangeGameState },            // Game Event
            { 0x27, PacketTypesIn.GameTestHighlightPos },       // Game Test Highlight Pos (new in 1.21.9)
            { 0x28, PacketTypesIn.OpenHorseWindow },            // Horse Screen Open
            { 0x29, PacketTypesIn.HurtAnimation },              // Hurt Animation
            { 0x2A, PacketTypesIn.InitializeWorldBorder },      // Initialize Border
            { 0x2B, PacketTypesIn.KeepAlive },                  // Keep Alive
            { 0x2C, PacketTypesIn.ChunkData },                  // Level Chunk With Light
            { 0x2D, PacketTypesIn.Effect },                     // Level Event
            { 0x2E, PacketTypesIn.Particle },                   // Level Particles
            { 0x2F, PacketTypesIn.UpdateLight },                // Light Update
            { 0x30, PacketTypesIn.JoinGame },                   // Login
            { 0x31, PacketTypesIn.MapData },                    // Map Item Data
            { 0x32, PacketTypesIn.TradeList },                  // Merchant Offers
            { 0x33, PacketTypesIn.EntityPosition },             // Move Entity Pos
            { 0x34, PacketTypesIn.EntityPositionAndRotation },  // Move Entity Pos Rot
            { 0x35, PacketTypesIn.MoveMinecartAlongTrack },     // Move Minecart Along Track
            { 0x36, PacketTypesIn.EntityRotation },             // Move Entity Rot
            { 0x37, PacketTypesIn.VehicleMove },                // Move Vehicle
            { 0x38, PacketTypesIn.OpenBook },                   // Open Book
            { 0x39, PacketTypesIn.OpenWindow },                 // Open Screen
            { 0x3A, PacketTypesIn.OpenSignEditor },             // Open Sign Editor
            { 0x3B, PacketTypesIn.Ping },                       // Ping
            { 0x3C, PacketTypesIn.PingResponse },               // Pong Response
            { 0x3D, PacketTypesIn.CraftRecipeResponse },        // Place Ghost Recipe
            { 0x3E, PacketTypesIn.PlayerAbilities },            // Player Abilities
            { 0x3F, PacketTypesIn.ChatMessage },                // Player Chat
            { 0x40, PacketTypesIn.EndCombatEvent },             // Player Combat End
            { 0x41, PacketTypesIn.EnterCombatEvent },           // Player Combat Enter
            { 0x42, PacketTypesIn.DeathCombatEvent },           // Player Combat Kill
            { 0x43, PacketTypesIn.PlayerRemove },               // Player Info Remove
            { 0x44, PacketTypesIn.PlayerInfo },                 // Player Info Update
            { 0x45, PacketTypesIn.FacePlayer },                 // Player Look At
            { 0x46, PacketTypesIn.PlayerPositionAndLook },      // Player Position
            { 0x47, PacketTypesIn.PlayerRotation },             // Player Rotation
            { 0x48, PacketTypesIn.RecipeBookAdd },              // Recipe Book Add
            { 0x49, PacketTypesIn.RecipeBookRemove },           // Recipe Book Remove
            { 0x4A, PacketTypesIn.RecipeBookSettings },         // Recipe Book Settings
            { 0x4B, PacketTypesIn.DestroyEntities },            // Remove Entities
            { 0x4C, PacketTypesIn.RemoveEntityEffect },         // Remove Mob Effect
            { 0x4D, PacketTypesIn.ResetScore },                 // Reset Score
            { 0x4E, PacketTypesIn.RemoveResourcePack },         // Resource Pack Pop
            { 0x4F, PacketTypesIn.ResourcePackSend },           // Resource Pack Push
            { 0x50, PacketTypesIn.Respawn },                    // Respawn
            { 0x51, PacketTypesIn.EntityHeadLook },             // Rotate Head
            { 0x52, PacketTypesIn.MultiBlockChange },           // Section Blocks Update
            { 0x53, PacketTypesIn.SelectAdvancementTab },       // Select Advancements Tab
            { 0x54, PacketTypesIn.ServerData },                 // Server Data
            { 0x55, PacketTypesIn.ActionBar },                  // Set Action Bar Text
            { 0x56, PacketTypesIn.WorldBorderCenter },          // Set Border Center
            { 0x57, PacketTypesIn.WorldBorderLerpSize },        // Set Border Lerp Size
            { 0x58, PacketTypesIn.WorldBorderSize },            // Set Border Size
            { 0x59, PacketTypesIn.WorldBorderWarningDelay },    // Set Border Warning Delay
            { 0x5A, PacketTypesIn.WorldBorderWarningReach },    // Set Border Warning Distance
            { 0x5B, PacketTypesIn.Camera },                     // Set Camera
            { 0x5C, PacketTypesIn.UpdateViewPosition },         // Set Chunk Cache Center
            { 0x5D, PacketTypesIn.UpdateViewDistance },          // Set Chunk Cache Radius
            { 0x5E, PacketTypesIn.SetCursorItem },              // Set Cursor Item
            { 0x5F, PacketTypesIn.SpawnPosition },              // Set Default Spawn Position
            { 0x60, PacketTypesIn.DisplayScoreboard },          // Set Display Objective
            { 0x61, PacketTypesIn.EntityMetadata },             // Set Entity Data
            { 0x62, PacketTypesIn.AttachEntity },               // Set Entity Link
            { 0x63, PacketTypesIn.EntityVelocity },             // Set Entity Motion
            { 0x64, PacketTypesIn.EntityEquipment },            // Set Equipment
            { 0x65, PacketTypesIn.SetExperience },              // Set Experience
            { 0x66, PacketTypesIn.UpdateHealth },               // Set Health
            { 0x67, PacketTypesIn.SetHeldSlot },                // Set Held Slot
            { 0x68, PacketTypesIn.ScoreboardObjective },        // Set Objective
            { 0x69, PacketTypesIn.SetPassengers },              // Set Passengers
            { 0x6A, PacketTypesIn.SetPlayerInventory },         // Set Player Inventory
            { 0x6B, PacketTypesIn.Teams },                      // Set Player Team
            { 0x6C, PacketTypesIn.UpdateScore },                // Set Score
            { 0x6D, PacketTypesIn.UpdateSimulationDistance },   // Set Simulation Distance
            { 0x6E, PacketTypesIn.SetTitleSubTitle },           // Set Subtitle Text
            { 0x6F, PacketTypesIn.TimeUpdate },                 // Set Time
            { 0x70, PacketTypesIn.SetTitleText },               // Set Title Text
            { 0x71, PacketTypesIn.SetTitleTime },               // Set Titles Animation
            { 0x72, PacketTypesIn.EntitySoundEffect },          // Sound Entity
            { 0x73, PacketTypesIn.SoundEffect },                // Sound
            { 0x74, PacketTypesIn.StartConfiguration },         // Start Configuration
            { 0x75, PacketTypesIn.StopSound },                  // Stop Sound
            { 0x76, PacketTypesIn.StoreCookie },                // Store Cookie
            { 0x77, PacketTypesIn.SystemChat },                 // System Chat
            { 0x78, PacketTypesIn.PlayerListHeaderAndFooter },  // Tab List
            { 0x79, PacketTypesIn.NBTQueryResponse },           // Tag Query
            { 0x7A, PacketTypesIn.CollectItem },                // Take Item Entity
            { 0x7B, PacketTypesIn.EntityTeleport },             // Teleport Entity
            { 0x7C, PacketTypesIn.TestInstanceBlockStatus },    // Test Instance Block Status
            { 0x7D, PacketTypesIn.SetTickingState },            // Ticking State
            { 0x7E, PacketTypesIn.StepTick },                   // Ticking Step
            { 0x7F, PacketTypesIn.Transfer },                   // Transfer
            { 0x80, PacketTypesIn.Advancements },               // Update Advancements
            { 0x81, PacketTypesIn.EntityProperties },           // Update Attributes
            { 0x82, PacketTypesIn.EntityEffect },               // Update Mob Effect
            { 0x83, PacketTypesIn.DeclareRecipes },             // Update Recipes
            { 0x84, PacketTypesIn.Tags },                       // Update Tags
            { 0x85, PacketTypesIn.ProjectilePower },            // Projectile Power
            { 0x86, PacketTypesIn.CustomReportDetails },        // Custom Report Details
            { 0x87, PacketTypesIn.ServerLinks },                // Server Links
            { 0x88, PacketTypesIn.Waypoint },                   // Waypoint
            { 0x89, PacketTypesIn.ClearDialog },                // Clear Dialog
            { 0x8A, PacketTypesIn.ShowDialog }                  // Show Dialog
        };

        private readonly Dictionary<int, PacketTypesOut> typeOut = new()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },             // Accept Teleportation
            { 0x01, PacketTypesOut.QueryBlockNBT },               // Block Entity Tag Query
            { 0x02, PacketTypesOut.BundleItemSelected },          // Bundle Item Selected
            { 0x03, PacketTypesOut.SetDifficulty },               // Change Difficulty
            { 0x04, PacketTypesOut.ChangeGameMode },              // Change Game Mode
            { 0x05, PacketTypesOut.MessageAcknowledgment },       // Chat Ack
            { 0x06, PacketTypesOut.ChatCommand },                 // Chat Command
            { 0x07, PacketTypesOut.SignedChatCommand },           // Chat Command Signed
            { 0x08, PacketTypesOut.ChatMessage },                 // Chat
            { 0x09, PacketTypesOut.PlayerSession },               // Chat Session Update
            { 0x0A, PacketTypesOut.ChunkBatchReceived },          // Chunk Batch Received
            { 0x0B, PacketTypesOut.ClientStatus },                // Client Command
            { 0x0C, PacketTypesOut.ClientTickEnd },               // Client Tick End
            { 0x0D, PacketTypesOut.ClientSettings },              // Client Information
            { 0x0E, PacketTypesOut.TabComplete },                 // Command Suggestion
            { 0x0F, PacketTypesOut.AcknowledgeConfiguration },    // Configuration Acknowledged
            { 0x10, PacketTypesOut.ClickWindowButton },           // Container Button Click
            { 0x11, PacketTypesOut.ClickWindow },                 // Container Click
            { 0x12, PacketTypesOut.CloseWindow },                 // Container Close
            { 0x13, PacketTypesOut.ChangeContainerSlotState },    // Container Slot State Changed
            { 0x14, PacketTypesOut.CookieResponse },              // Cookie Response
            { 0x15, PacketTypesOut.PluginMessage },               // Custom Payload
            { 0x16, PacketTypesOut.DebugSampleSubscription },     // Debug Subscription Request
            { 0x17, PacketTypesOut.EditBook },                    // Edit Book
            { 0x18, PacketTypesOut.EntityNBTRequest },            // Entity Tag Query
            { 0x19, PacketTypesOut.InteractEntity },              // Interact
            { 0x1A, PacketTypesOut.GenerateStructure },           // Jigsaw Generate
            { 0x1B, PacketTypesOut.KeepAlive },                   // Keep Alive
            { 0x1C, PacketTypesOut.LockDifficulty },              // Lock Difficulty
            { 0x1D, PacketTypesOut.PlayerPosition },              // Move Player Pos
            { 0x1E, PacketTypesOut.PlayerPositionAndRotation },   // Move Player Pos Rot
            { 0x1F, PacketTypesOut.PlayerRotation },              // Move Player Rot
            { 0x20, PacketTypesOut.PlayerMovement },              // Move Player Status Only
            { 0x21, PacketTypesOut.VehicleMove },                 // Move Vehicle
            { 0x22, PacketTypesOut.SteerBoat },                   // Paddle Boat
            { 0x23, PacketTypesOut.PickItem },                    // Pick Item From Block
            { 0x24, PacketTypesOut.PickItemFromEntity },          // Pick Item From Entity
            { 0x25, PacketTypesOut.PingRequest },                 // Ping Request
            { 0x26, PacketTypesOut.CraftRecipeRequest },          // Place Recipe
            { 0x27, PacketTypesOut.PlayerAbilities },             // Player Abilities
            { 0x28, PacketTypesOut.PlayerDigging },               // Player Action
            { 0x29, PacketTypesOut.EntityAction },                // Player Command
            { 0x2A, PacketTypesOut.SteerVehicle },                // Player Input
            { 0x2B, PacketTypesOut.PlayerLoaded },                // Player Loaded
            { 0x2C, PacketTypesOut.Pong },                        // Pong
            { 0x2D, PacketTypesOut.SetDisplayedRecipe },          // Recipe Book Change Settings
            { 0x2E, PacketTypesOut.SetRecipeBookState },          // Recipe Book Seen Recipe
            { 0x2F, PacketTypesOut.NameItem },                    // Rename Item
            { 0x30, PacketTypesOut.ResourcePackStatus },          // Resource Pack
            { 0x31, PacketTypesOut.AdvancementTab },              // Seen Advancements
            { 0x32, PacketTypesOut.SelectTrade },                 // Select Trade
            { 0x33, PacketTypesOut.SetBeaconEffect },             // Set Beacon
            { 0x34, PacketTypesOut.HeldItemChange },              // Set Carried Item
            { 0x35, PacketTypesOut.UpdateCommandBlock },          // Set Command Block
            { 0x36, PacketTypesOut.UpdateCommandBlockMinecart },  // Set Command Minecart
            { 0x37, PacketTypesOut.CreativeInventoryAction },     // Set Creative Mode Slot
            { 0x38, PacketTypesOut.UpdateJigsawBlock },           // Set Jigsaw Block
            { 0x39, PacketTypesOut.UpdateStructureBlock },        // Set Structure Block
            { 0x3A, PacketTypesOut.SetTestBlock },                // Set Test Block
            { 0x3B, PacketTypesOut.UpdateSign },                  // Sign Update
            { 0x3C, PacketTypesOut.Animation },                   // Swing
            { 0x3D, PacketTypesOut.Spectate },                    // Teleport To Entity
            { 0x3E, PacketTypesOut.TestInstanceBlockAction },     // Test Instance Block Action
            { 0x3F, PacketTypesOut.PlayerBlockPlacement },        // Use Item On
            { 0x40, PacketTypesOut.UseItem },                     // Use Item
            { 0x41, PacketTypesOut.CustomClickAction }            // Custom Click Action
        };

        private readonly Dictionary<int, ConfigurationPacketTypesIn> configurationTypesIn = new()
        {
            { 0x00, ConfigurationPacketTypesIn.CookieRequest },
            { 0x01, ConfigurationPacketTypesIn.PluginMessage },
            { 0x02, ConfigurationPacketTypesIn.Disconnect },
            { 0x03, ConfigurationPacketTypesIn.FinishConfiguration },
            { 0x04, ConfigurationPacketTypesIn.KeepAlive },
            { 0x05, ConfigurationPacketTypesIn.Ping },
            { 0x06, ConfigurationPacketTypesIn.ResetChat },
            { 0x07, ConfigurationPacketTypesIn.RegistryData },
            { 0x08, ConfigurationPacketTypesIn.RemoveResourcePack },
            { 0x09, ConfigurationPacketTypesIn.ResourcePack },
            { 0x0A, ConfigurationPacketTypesIn.StoreCookie },
            { 0x0B, ConfigurationPacketTypesIn.Transfer },
            { 0x0C, ConfigurationPacketTypesIn.FeatureFlags },
            { 0x0D, ConfigurationPacketTypesIn.UpdateTags },
            { 0x0E, ConfigurationPacketTypesIn.KnownDataPacks },
            { 0x0F, ConfigurationPacketTypesIn.CustomReportDetails },
            { 0x10, ConfigurationPacketTypesIn.ServerLinks },
            { 0x11, ConfigurationPacketTypesIn.ClearDialog },
            { 0x12, ConfigurationPacketTypesIn.ShowDialog },
            { 0x13, ConfigurationPacketTypesIn.CodeOfConduct }      // New in 1.21.9
        };

        private readonly Dictionary<int, ConfigurationPacketTypesOut> configurationTypesOut = new()
        {
            { 0x00, ConfigurationPacketTypesOut.ClientInformation },
            { 0x01, ConfigurationPacketTypesOut.CookieResponse },
            { 0x02, ConfigurationPacketTypesOut.PluginMessage },
            { 0x03, ConfigurationPacketTypesOut.FinishConfiguration },
            { 0x04, ConfigurationPacketTypesOut.KeepAlive },
            { 0x05, ConfigurationPacketTypesOut.Pong },
            { 0x06, ConfigurationPacketTypesOut.ResourcePackResponse },
            { 0x07, ConfigurationPacketTypesOut.KnownDataPacks },
            { 0x08, ConfigurationPacketTypesOut.CustomClickAction },
            { 0x09, ConfigurationPacketTypesOut.AcceptCodeOfConduct }  // New in 1.21.9
        };
        
        protected override Dictionary<int, PacketTypesIn> GetListIn() => typeIn;
        protected override Dictionary<int, PacketTypesOut> GetListOut() => typeOut;
        protected override Dictionary<int, ConfigurationPacketTypesIn> GetConfigurationListIn() => configurationTypesIn!;
        protected override Dictionary<int, ConfigurationPacketTypesOut> GetConfigurationListOut() => configurationTypesOut!;
    }
