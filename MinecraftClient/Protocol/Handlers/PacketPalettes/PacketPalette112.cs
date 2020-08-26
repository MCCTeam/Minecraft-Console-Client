using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.PacketTypes.Palettes
{
    /// <summary>
    /// For Minecraft version 1.12 - 1.12.1
    /// </summary>
    class PacketPalette112 : PacketTypePalette
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
            PacketTypesIn.TabComplete,
            PacketTypesIn.ChatMessage,
            PacketTypesIn.MultiBlockChange,
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
            PacketTypesIn.EntityPositionAndRotation,
            PacketTypesIn.EntityRotation,
            PacketTypesIn.VehicleMove,
            PacketTypesIn.OpenSignEditor,
            PacketTypesIn.PlayerAbilities,
            PacketTypesIn.CombatEvent,
            PacketTypesIn.PlayerInfo,
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
            PacketTypesIn.SoundEffect,
            PacketTypesIn.PlayerListHeaderAndFooter,
            PacketTypesIn.CollectItem,
            PacketTypesIn.EntityTeleport,
            PacketTypesIn.Advancements,
            PacketTypesIn.EntityProperties,
            PacketTypesIn.EntityEffect
        };

        private List<PacketTypesOut> typeOut = new List<PacketTypesOut>()
        {
            PacketTypesOut.TeleportConfirm,
            PacketTypesOut.Unknown, // PrepareCraftingGrid
            PacketTypesOut.TabComplete,
            PacketTypesOut.ChatMessage,
            PacketTypesOut.ClientStatus,
            PacketTypesOut.ClientSettings,
            PacketTypesOut.WindowConfirmation,
            PacketTypesOut.Unknown, // EnchantItem
            PacketTypesOut.ClickWindow,
            PacketTypesOut.CloseWindow,
            PacketTypesOut.PluginMessage,
            PacketTypesOut.InteractEntity,
            PacketTypesOut.KeepAlive,
            PacketTypesOut.PlayerMovement,
            PacketTypesOut.PlayerPosition,
            PacketTypesOut.PlayerPositionAndRotation,
            PacketTypesOut.PlayerRotation,
            PacketTypesOut.VehicleMove,
            PacketTypesOut.SteerBoat,
            PacketTypesOut.PlayerAbilities,
            PacketTypesOut.PlayerDigging,
            PacketTypesOut.EntityAction,
            PacketTypesOut.SteerVehicle,
            PacketTypesOut.RecipeBookData,
            PacketTypesOut.ResourcePackStatus,
            PacketTypesOut.AdvancementTab,
            PacketTypesOut.HeldItemChange,
            PacketTypesOut.CreativeInventoryAction,
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
