using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.PacketTypes.Palettes
{
    class PacketPalette1132 : PacketTypePalette
    {
        private List<PacketTypesIn> typeIn = new List<PacketTypesIn>()
        {
            PacketTypesIn.SpawnEntity,
            PacketTypesIn.SpawnExperienceOrb,
            PacketTypesIn.SpawnWeatherEntity,
            PacketTypesIn.SpawnLivingEntity,
            PacketTypesIn.SpawnPainting,
            PacketTypesIn.SpawnPlayer,
            PacketTypesIn.EntityAnimation,
            PacketTypesIn.Statistics,
            PacketTypesIn.BlockBreakAnimation,
            PacketTypesIn.BlockEntityData,
            PacketTypesIn.BlockAction,
            PacketTypesIn.BlockChange,
            PacketTypesIn.BossBar,
            PacketTypesIn.ServerDifficulty,
            PacketTypesIn.ChatMessage,
            PacketTypesIn.MultiBlockChange,
            PacketTypesIn.TabComplete,
            PacketTypesIn.DeclareCommands,
            PacketTypesIn.WindowConfirmation,
            PacketTypesIn.CloseWindow,
            PacketTypesIn.OpenWindow,
            PacketTypesIn.WindowItems,
            PacketTypesIn.WindowProperty,
            PacketTypesIn.SetSlot,
            PacketTypesIn.SetCooldown,
            PacketTypesIn.PluginMessage,
            PacketTypesIn.NamedSoundEffect,
            PacketTypesIn.Disconnect,
            PacketTypesIn.EntityStatus,
            PacketTypesIn.NBTQueryResponse,
            PacketTypesIn.Explosion,
            PacketTypesIn.UnloadChunk,
            PacketTypesIn.ChangeGameState,
            PacketTypesIn.KeepAlive,
            PacketTypesIn.ChunkData,
            PacketTypesIn.Effect,
            PacketTypesIn.Particle,
            PacketTypesIn.JoinGame,
            PacketTypesIn.MapData,
            PacketTypesIn.EntityMovement,
            PacketTypesIn.EntityPosition,
            PacketTypesIn.EntityPositionandRotation,
            PacketTypesIn.EntityRotation,
            PacketTypesIn.VehicleMove,
            PacketTypesIn.OpenSignEditor,
            PacketTypesIn.CraftRecipeResponse,
            PacketTypesIn.PlayerAbilities,
            PacketTypesIn.CombatEvent,
            PacketTypesIn.PlayerListUpdate,
            PacketTypesIn.FacePlayer,
            PacketTypesIn.PlayerPositionAndLook,
            PacketTypesIn.Unknown, // UseBed
            PacketTypesIn.UnlockRecipes,
            PacketTypesIn.DestroyEntities,
            PacketTypesIn.RemoveEntityEffect,
            PacketTypesIn.ResourcePackSend,
            PacketTypesIn.Respawn,
            PacketTypesIn.EntityHeadLook,
            PacketTypesIn.SelectAdvancementTab,
            PacketTypesIn.WorldBorder,
            PacketTypesIn.Camera,
            PacketTypesIn.HeldItemChange,
            PacketTypesIn.DisplayScoreboard,
            PacketTypesIn.EntityMetadata,
            PacketTypesIn.AttachEntity,
            PacketTypesIn.EntityVelocity,
            PacketTypesIn.EntityEquipment,
            PacketTypesIn.SetExperience,
            PacketTypesIn.UpdateHealth,
            PacketTypesIn.ScoreboardObjective,
            PacketTypesIn.SetPassengers,
            PacketTypesIn.Teams,
            PacketTypesIn.UpdateScore,
            PacketTypesIn.SpawnPosition,
            PacketTypesIn.TimeUpdate,
            PacketTypesIn.Title,
            PacketTypesIn.StopSound,
            PacketTypesIn.SoundEffect,
            PacketTypesIn.PlayerListHeaderAndFooter,
            PacketTypesIn.CollectItem,
            PacketTypesIn.EntityTeleport,
            PacketTypesIn.Advancements,
            PacketTypesIn.EntityProperties,
            PacketTypesIn.EntityEffect,
            PacketTypesIn.DeclareRecipes,
            PacketTypesIn.Tags
        };

        private List<PacketTypesOut> typeOut = new List<PacketTypesOut>()
        {
            PacketTypesOut.TeleportConfirm,
            PacketTypesOut.QueryBlockNBT,
            PacketTypesOut.ChatMessage,
            PacketTypesOut.ClientStatus,
            PacketTypesOut.ClientSettings,
            PacketTypesOut.TabComplete,
            PacketTypesOut.WindowConfirmation,
            PacketTypesOut.Unknown, // EnchantItem
            PacketTypesOut.ClickWindow,
            PacketTypesOut.CloseWindow,
            PacketTypesOut.PluginMessage,
            PacketTypesOut.EditBook,
            PacketTypesOut.EntityNBTRequest,
            PacketTypesOut.InteractEntity,
            PacketTypesOut.KeepAlive,
            PacketTypesOut.PlayerMovement,
            PacketTypesOut.PlayerPosition,
            PacketTypesOut.PlayerPositionAndRotation,
            PacketTypesOut.PlayerPosition,
            PacketTypesOut.VehicleMove,
            PacketTypesOut.SteerBoat,
            PacketTypesOut.PickItem,
            PacketTypesOut.CraftRecipeRequest,
            PacketTypesOut.PlayerAbilities,
            PacketTypesOut.PlayerDigging,
            PacketTypesOut.EntityAction,
            PacketTypesOut.SteerVehicle,
            PacketTypesOut.RecipeBookData,
            PacketTypesOut.NameItem,
            PacketTypesOut.ResourcePackStatus,
            PacketTypesOut.AdvancementTab,
            PacketTypesOut.SelectTrade,
            PacketTypesOut.SetBeaconEffect,
            PacketTypesOut.HeldItemChange,
            PacketTypesOut.UpdateCommandBlock,
            PacketTypesOut.UpdateCommandBlockMinecart,
            PacketTypesOut.CreativeInventoryAction,
            PacketTypesOut.UpdateStructureBlock,
            PacketTypesOut.UpdateSign,
            PacketTypesOut.Animation,
            PacketTypesOut.Spectate,
            PacketTypesOut.PlayerBlockPlacement,
            PacketTypesOut.UseItem
        };

        protected override List<PacketTypesIn> GetListIn()
        {
            return typeIn;
        }

        protected override List<PacketTypesOut> GetListOut()
        {
            return typeOut;
        }
    }
}
