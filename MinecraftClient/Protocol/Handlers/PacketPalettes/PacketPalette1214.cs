using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes;

public class PacketPalette1214 : PacketTypePalette
    {
        private readonly Dictionary<int, PacketTypesIn> typeIn = new()
        {
            { 0x00, PacketTypesIn.Bundle },                     // Bundle delimiter
            { 0x01, PacketTypesIn.SpawnEntity },                // Add Entity
            { 0x02, PacketTypesIn.SpawnExperienceOrb },         // Add Experience Orb
            { 0x03, PacketTypesIn.EntityAnimation },            // Animate
            { 0x04, PacketTypesIn.Statistics },                 // Award Stats
            { 0x05, PacketTypesIn.BlockChangedAck },            // Block Changed Ack
            { 0x06, PacketTypesIn.BlockBreakAnimation },        // Block Destruction
            { 0x07, PacketTypesIn.BlockEntityData },            // Block Entity Data
            { 0x08, PacketTypesIn.BlockAction },                // Block Event
            { 0x09, PacketTypesIn.BlockChange },                // Block Update
            { 0x0A, PacketTypesIn.BossBar },                    // Boss Event
            { 0x0B, PacketTypesIn.ServerDifficulty },           // Change Difficulty
            { 0x0C, PacketTypesIn.ChunkBatchFinished },         // Chunk Batch Finished
            { 0x0D, PacketTypesIn.ChunkBatchStarted },          // Chunk Batch Start
            { 0x0E, PacketTypesIn.ChunksBiomes },               // Chunks Biomes
            { 0x0F, PacketTypesIn.ClearTiles },                 // Clear Titles
            { 0x10, PacketTypesIn.TabComplete },                // Command Suggestions
            { 0x11, PacketTypesIn.DeclareCommands },            // Commands
            { 0x12, PacketTypesIn.CloseWindow },                // Container Close
            { 0x13, PacketTypesIn.WindowItems },                // Container Set Content
            { 0x14, PacketTypesIn.WindowProperty },             // Container Set Data
            { 0x15, PacketTypesIn.SetSlot },                    // Container Set Slot
            { 0x16, PacketTypesIn.CookieRequest },              // Cookie Request
            { 0x17, PacketTypesIn.SetCooldown },                // Cooldown
            { 0x18, PacketTypesIn.ChatSuggestions },            // Custom Chat Completions
            { 0x19, PacketTypesIn.PluginMessage },              // Custom Payload
            { 0x1A, PacketTypesIn.DamageEvent },                // Damage Event
            { 0x1B, PacketTypesIn.DebugSample },                // Debug Sample
            { 0x1C, PacketTypesIn.HideMessage },                // Delete Chat
            { 0x1D, PacketTypesIn.Disconnect },                 // Disconnect
            { 0x1E, PacketTypesIn.ProfilelessChatMessage },     // Disguised Chat
            { 0x1F, PacketTypesIn.EntityStatus },               // Entity Event
            { 0x20, PacketTypesIn.EntityPositionSync },         // Entity Position Sync (new in 1.21.2)
            { 0x21, PacketTypesIn.Explosion },                  // Explode
            { 0x22, PacketTypesIn.UnloadChunk },                // Forget Level Chunk
            { 0x23, PacketTypesIn.ChangeGameState },            // Game Event
            { 0x24, PacketTypesIn.OpenHorseWindow },            // Horse Screen Open
            { 0x25, PacketTypesIn.HurtAnimation },              // Hurt Animation
            { 0x26, PacketTypesIn.InitializeWorldBorder },      // Initialize Border
            { 0x27, PacketTypesIn.KeepAlive },                  // Keep Alive
            { 0x28, PacketTypesIn.ChunkData },                  // Level Chunk With Light
            { 0x29, PacketTypesIn.Effect },                     // Level Event
            { 0x2A, PacketTypesIn.Particle },                   // Level Particles
            { 0x2B, PacketTypesIn.UpdateLight },                // Light Update
            { 0x2C, PacketTypesIn.JoinGame },                   // Login
            { 0x2D, PacketTypesIn.MapData },                    // Map Item Data
            { 0x2E, PacketTypesIn.TradeList },                  // Merchant Offers
            { 0x2F, PacketTypesIn.EntityPosition },             // Move Entity Pos
            { 0x30, PacketTypesIn.EntityPositionAndRotation },  // Move Entity Pos Rot
            { 0x31, PacketTypesIn.MoveMinecartAlongTrack },     // Move Minecart Along Track (new in 1.21.2)
            { 0x32, PacketTypesIn.EntityRotation },             // Move Entity Rot
            { 0x33, PacketTypesIn.VehicleMove },                // Move Vehicle
            { 0x34, PacketTypesIn.OpenBook },                   // Open Book
            { 0x35, PacketTypesIn.OpenWindow },                 // Open Screen
            { 0x36, PacketTypesIn.OpenSignEditor },             // Open Sign Editor
            { 0x37, PacketTypesIn.Ping },                       // Ping
            { 0x38, PacketTypesIn.PingResponse },               // Pong Response
            { 0x39, PacketTypesIn.CraftRecipeResponse },        // Place Ghost Recipe
            { 0x3A, PacketTypesIn.PlayerAbilities },            // Player Abilities
            { 0x3B, PacketTypesIn.ChatMessage },                // Player Chat
            { 0x3C, PacketTypesIn.EndCombatEvent },             // Player Combat End
            { 0x3D, PacketTypesIn.EnterCombatEvent },           // Player Combat Enter
            { 0x3E, PacketTypesIn.DeathCombatEvent },           // Player Combat Kill
            { 0x3F, PacketTypesIn.PlayerRemove },               // Player Info Remove
            { 0x40, PacketTypesIn.PlayerInfo },                 // Player Info Update
            { 0x41, PacketTypesIn.FacePlayer },                 // Player Look At
            { 0x42, PacketTypesIn.PlayerPositionAndLook },      // Player Position
            { 0x43, PacketTypesIn.PlayerRotation },             // Player Rotation (new in 1.21.2)
            { 0x44, PacketTypesIn.RecipeBookAdd },              // Recipe Book Add (new in 1.21.2, replaces UnlockRecipes)
            { 0x45, PacketTypesIn.RecipeBookRemove },           // Recipe Book Remove (new in 1.21.2)
            { 0x46, PacketTypesIn.RecipeBookSettings },         // Recipe Book Settings (new in 1.21.2)
            { 0x47, PacketTypesIn.DestroyEntities },            // Remove Entities
            { 0x48, PacketTypesIn.RemoveEntityEffect },         // Remove Mob Effect
            { 0x49, PacketTypesIn.ResetScore },                 // Reset Score
            { 0x4A, PacketTypesIn.RemoveResourcePack },         // Resource Pack Pop
            { 0x4B, PacketTypesIn.ResourcePackSend },           // Resource Pack Push
            { 0x4C, PacketTypesIn.Respawn },                    // Respawn
            { 0x4D, PacketTypesIn.EntityHeadLook },             // Rotate Head
            { 0x4E, PacketTypesIn.MultiBlockChange },           // Section Blocks Update
            { 0x4F, PacketTypesIn.SelectAdvancementTab },       // Select Advancements Tab
            { 0x50, PacketTypesIn.ServerData },                 // Server Data
            { 0x51, PacketTypesIn.ActionBar },                  // Set Action Bar Text
            { 0x52, PacketTypesIn.WorldBorderCenter },          // Set Border Center
            { 0x53, PacketTypesIn.WorldBorderLerpSize },        // Set Border Lerp Size
            { 0x54, PacketTypesIn.WorldBorderSize },            // Set Border Size
            { 0x55, PacketTypesIn.WorldBorderWarningDelay },    // Set Border Warning Delay
            { 0x56, PacketTypesIn.WorldBorderWarningReach },    // Set Border Warning Distance
            { 0x57, PacketTypesIn.Camera },                     // Set Camera
            { 0x58, PacketTypesIn.UpdateViewPosition },         // Set Chunk Cache Center
            { 0x59, PacketTypesIn.UpdateViewDistance },          // Set Chunk Cache Radius
            { 0x5A, PacketTypesIn.SetCursorItem },              // Set Cursor Item (new in 1.21.2)
            { 0x5B, PacketTypesIn.SpawnPosition },              // Set Default Spawn Position
            { 0x5C, PacketTypesIn.DisplayScoreboard },          // Set Display Objective
            { 0x5D, PacketTypesIn.EntityMetadata },             // Set Entity Data
            { 0x5E, PacketTypesIn.AttachEntity },               // Set Entity Link
            { 0x5F, PacketTypesIn.EntityVelocity },             // Set Entity Motion
            { 0x60, PacketTypesIn.EntityEquipment },            // Set Equipment
            { 0x61, PacketTypesIn.SetExperience },              // Set Experience
            { 0x62, PacketTypesIn.UpdateHealth },               // Set Health
            { 0x63, PacketTypesIn.SetHeldSlot },                // Set Held Slot (new in 1.21.2, replaces HeldItemChange)
            { 0x64, PacketTypesIn.ScoreboardObjective },        // Set Objective
            { 0x65, PacketTypesIn.SetPassengers },              // Set Passengers
            { 0x66, PacketTypesIn.SetPlayerInventory },         // Set Player Inventory (new in 1.21.2)
            { 0x67, PacketTypesIn.Teams },                      // Set Player Team
            { 0x68, PacketTypesIn.UpdateScore },                // Set Score
            { 0x69, PacketTypesIn.UpdateSimulationDistance },   // Set Simulation Distance
            { 0x6A, PacketTypesIn.SetTitleSubTitle },           // Set Subtitle Text
            { 0x6B, PacketTypesIn.TimeUpdate },                 // Set Time
            { 0x6C, PacketTypesIn.SetTitleText },               // Set Title Text
            { 0x6D, PacketTypesIn.SetTitleTime },               // Set Titles Animation
            { 0x6E, PacketTypesIn.EntitySoundEffect },          // Sound Entity
            { 0x6F, PacketTypesIn.SoundEffect },                // Sound
            { 0x70, PacketTypesIn.StartConfiguration },         // Start Configuration
            { 0x71, PacketTypesIn.StopSound },                  // Stop Sound
            { 0x72, PacketTypesIn.StoreCookie },                // Store Cookie
            { 0x73, PacketTypesIn.SystemChat },                 // System Chat
            { 0x74, PacketTypesIn.PlayerListHeaderAndFooter },  // Tab List
            { 0x75, PacketTypesIn.NBTQueryResponse },           // Tag Query
            { 0x76, PacketTypesIn.CollectItem },                // Take Item Entity
            { 0x77, PacketTypesIn.EntityTeleport },             // Teleport Entity
            { 0x78, PacketTypesIn.SetTickingState },            // Ticking State
            { 0x79, PacketTypesIn.StepTick },                   // Ticking Step
            { 0x7A, PacketTypesIn.Transfer },                   // Transfer
            { 0x7B, PacketTypesIn.Advancements },               // Update Advancements
            { 0x7C, PacketTypesIn.EntityProperties },           // Update Attributes
            { 0x7D, PacketTypesIn.EntityEffect },               // Update Mob Effect
            { 0x7E, PacketTypesIn.DeclareRecipes },             // Update Recipes
            { 0x7F, PacketTypesIn.Tags },                       // Update Tags
            { 0x80, PacketTypesIn.ProjectilePower },            // Projectile Power
            { 0x81, PacketTypesIn.CustomReportDetails },        // Custom Report Details
            { 0x82, PacketTypesIn.ServerLinks }                 // Server Links
        };

        private readonly Dictionary<int, PacketTypesOut> typeOut = new()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },             // Accept Teleportation
            { 0x01, PacketTypesOut.QueryBlockNBT },               // Block Entity Tag Query
            { 0x02, PacketTypesOut.BundleItemSelected },          // Bundle Item Selected
            { 0x03, PacketTypesOut.SetDifficulty },               // Change Difficulty
            { 0x04, PacketTypesOut.MessageAcknowledgment },       // Chat Ack
            { 0x05, PacketTypesOut.ChatCommand },                 // Chat Command
            { 0x06, PacketTypesOut.SignedChatCommand },           // Chat Command Signed
            { 0x07, PacketTypesOut.ChatMessage },                 // Chat
            { 0x08, PacketTypesOut.PlayerSession },               // Chat Session Update
            { 0x09, PacketTypesOut.ChunkBatchReceived },          // Chunk Batch Received
            { 0x0A, PacketTypesOut.ClientStatus },                // Client Command
            { 0x0B, PacketTypesOut.ClientTickEnd },               // Client Tick End
            { 0x0C, PacketTypesOut.ClientSettings },              // Client Information
            { 0x0D, PacketTypesOut.TabComplete },                 // Command Suggestion
            { 0x0E, PacketTypesOut.AcknowledgeConfiguration },    // Configuration Acknowledged
            { 0x0F, PacketTypesOut.ClickWindowButton },           // Container Button Click
            { 0x10, PacketTypesOut.ClickWindow },                 // Container Click
            { 0x11, PacketTypesOut.CloseWindow },                 // Container Close
            { 0x12, PacketTypesOut.ChangeContainerSlotState },    // Container Slot State Changed
            { 0x13, PacketTypesOut.CookieResponse },              // Cookie Response
            { 0x14, PacketTypesOut.PluginMessage },               // Custom Payload
            { 0x15, PacketTypesOut.DebugSampleSubscription },     // Debug Sample Subscription
            { 0x16, PacketTypesOut.EditBook },                    // Edit Book
            { 0x17, PacketTypesOut.EntityNBTRequest },            // Entity Tag Query
            { 0x18, PacketTypesOut.InteractEntity },              // Interact
            { 0x19, PacketTypesOut.GenerateStructure },           // Jigsaw Generate
            { 0x1A, PacketTypesOut.KeepAlive },                   // Keep Alive
            { 0x1B, PacketTypesOut.LockDifficulty },              // Lock Difficulty
            { 0x1C, PacketTypesOut.PlayerPosition },              // Move Player Pos
            { 0x1D, PacketTypesOut.PlayerPositionAndRotation },   // Move Player Pos Rot
            { 0x1E, PacketTypesOut.PlayerRotation },              // Move Player Rot
            { 0x1F, PacketTypesOut.PlayerMovement },              // Move Player Status Only
            { 0x20, PacketTypesOut.VehicleMove },                 // Move Vehicle
            { 0x21, PacketTypesOut.SteerBoat },                   // Paddle Boat
            { 0x22, PacketTypesOut.PickItem },                    // Pick Item From Block (split in 1.21.4)
            { 0x23, PacketTypesOut.PickItemFromEntity },          // Pick Item From Entity (new in 1.21.4)
            { 0x24, PacketTypesOut.PingRequest },                 // Ping Request
            { 0x25, PacketTypesOut.CraftRecipeRequest },          // Place Recipe
            { 0x26, PacketTypesOut.PlayerAbilities },             // Player Abilities
            { 0x27, PacketTypesOut.PlayerDigging },               // Player Action
            { 0x28, PacketTypesOut.EntityAction },                // Player Command
            { 0x29, PacketTypesOut.SteerVehicle },                // Player Input
            { 0x2A, PacketTypesOut.PlayerLoaded },                // Player Loaded (new in 1.21.4)
            { 0x2B, PacketTypesOut.Pong },                        // Pong
            { 0x2C, PacketTypesOut.SetDisplayedRecipe },          // Recipe Book Change Settings
            { 0x2D, PacketTypesOut.SetRecipeBookState },          // Recipe Book Seen Recipe
            { 0x2E, PacketTypesOut.NameItem },                    // Rename Item
            { 0x2F, PacketTypesOut.ResourcePackStatus },          // Resource Pack
            { 0x30, PacketTypesOut.AdvancementTab },              // Seen Advancements
            { 0x31, PacketTypesOut.SelectTrade },                 // Select Trade
            { 0x32, PacketTypesOut.SetBeaconEffect },             // Set Beacon
            { 0x33, PacketTypesOut.HeldItemChange },              // Set Carried Item
            { 0x34, PacketTypesOut.UpdateCommandBlock },          // Set Command Block
            { 0x35, PacketTypesOut.UpdateCommandBlockMinecart },  // Set Command Minecart
            { 0x36, PacketTypesOut.CreativeInventoryAction },     // Set Creative Mode Slot
            { 0x37, PacketTypesOut.UpdateJigsawBlock },           // Set Jigsaw Block
            { 0x38, PacketTypesOut.UpdateStructureBlock },        // Set Structure Block
            { 0x39, PacketTypesOut.UpdateSign },                  // Sign Update
            { 0x3A, PacketTypesOut.Animation },                   // Swing
            { 0x3B, PacketTypesOut.Spectate },                    // Teleport To Entity
            { 0x3C, PacketTypesOut.PlayerBlockPlacement },        // Use Item On
            { 0x3D, PacketTypesOut.UseItem },                     // Use Item
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
            { 0x10, ConfigurationPacketTypesIn.ServerLinks }
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
            { 0x07, ConfigurationPacketTypesOut.KnownDataPacks }
        };
        
        protected override Dictionary<int, PacketTypesIn> GetListIn() => typeIn;
        protected override Dictionary<int, PacketTypesOut> GetListOut() => typeOut;
        protected override Dictionary<int, ConfigurationPacketTypesIn> GetConfigurationListIn() => configurationTypesIn!;
        protected override Dictionary<int, ConfigurationPacketTypesOut> GetConfigurationListOut() => configurationTypesOut!;
    }
