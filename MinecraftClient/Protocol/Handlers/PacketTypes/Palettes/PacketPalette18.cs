using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.PacketTypes.Palettes
{
    class PacketPalette18 : PacketTypePalette
    {
        private List<PacketTypesIn> typeIn = new List<PacketTypesIn>()
        {
            PacketTypesIn.KeepAlive,
            PacketTypesIn.JoinGame,
            PacketTypesIn.ChatMessage,
            PacketTypesIn.TimeUpdate,
            PacketTypesIn.EntityEquipment,
            PacketTypesIn.SpawnPosition,
            PacketTypesIn.UpdateHealth,
            PacketTypesIn.Respawn,
            PacketTypesIn.PlayerPositionAndLook,
            PacketTypesIn.HeldItemChange,
            PacketTypesIn.Unknown, // UseBed
            PacketTypesIn.EntityAnimation,
            PacketTypesIn.SpawnPlayer,
            PacketTypesIn.CollectItem,
            PacketTypesIn.SpawnEntity,
            PacketTypesIn.SpawnLivingEntity,
            PacketTypesIn.SpawnPainting,
            PacketTypesIn.SpawnExperienceOrb,
            PacketTypesIn.EntityVelocity,
            PacketTypesIn.DestroyEntities,
            PacketTypesIn.EntityMovement,
            PacketTypesIn.EntityPosition,
            PacketTypesIn.EntityRotation,
            PacketTypesIn.EntityPositionandRotation,
            PacketTypesIn.EntityTeleport,
            PacketTypesIn.EntityHeadLook,
            PacketTypesIn.EntityStatus,
            PacketTypesIn.AttachEntity,
            PacketTypesIn.EntityMetadata,
            PacketTypesIn.EntityEffect,
            PacketTypesIn.RemoveEntityEffect,
            PacketTypesIn.SetExperience,
            PacketTypesIn.EntityProperties,
            PacketTypesIn.ChunkData,
            PacketTypesIn.MultiBlockChange,
            PacketTypesIn.BlockChange,
            PacketTypesIn.BlockAction,
            PacketTypesIn.BlockBreakAnimation,
            PacketTypesIn.MapChunkBulk,
            PacketTypesIn.Explosion,
            PacketTypesIn.Effect,
            PacketTypesIn.SoundEffect,
            PacketTypesIn.Particle,
            PacketTypesIn.ChangeGameState,
            PacketTypesIn.SpawnWeatherEntity,
            PacketTypesIn.OpenWindow,
            PacketTypesIn.CloseWindow,
            PacketTypesIn.SetSlot,
            PacketTypesIn.WindowItems,
            PacketTypesIn.WindowProperty,
            PacketTypesIn.WindowConfirmation,
            PacketTypesIn.Unknown, // UpdateSign
            PacketTypesIn.MapData,
            PacketTypesIn.BlockEntityData,
            PacketTypesIn.OpenSignEditor,
            PacketTypesIn.Statistics,
            PacketTypesIn.PlayerListUpdate,
            PacketTypesIn.PlayerAbilities,
            PacketTypesIn.TabComplete,
            PacketTypesIn.ScoreboardObjective,
            PacketTypesIn.UpdateScore,
            PacketTypesIn.DisplayScoreboard,
            PacketTypesIn.Teams,
            PacketTypesIn.PluginMessage,
            PacketTypesIn.Disconnect,
            PacketTypesIn.ServerDifficulty,
            PacketTypesIn.CombatEvent,
            PacketTypesIn.Camera,
            PacketTypesIn.WorldBorder,
            PacketTypesIn.Title,
            PacketTypesIn.Unknown, // SetCompression
            PacketTypesIn.PlayerListHeaderAndFooter,
            PacketTypesIn.ResourcePackSend,
            PacketTypesIn.Unknown // UpdateEntityNBT
        };

        private List<PacketTypesOut> typeOut = new List<PacketTypesOut>()
        {
            PacketTypesOut.KeepAlive,
            PacketTypesOut.ChatMessage,
            PacketTypesOut.InteractEntity,
            PacketTypesOut.Unknown, // Player
            PacketTypesOut.PlayerPosition,
            PacketTypesOut.PlayerRotation,
            PacketTypesOut.PlayerPositionAndRotation,
            PacketTypesOut.PlayerDigging,
            PacketTypesOut.PlayerBlockPlacement,
            PacketTypesOut.HeldItemChange,
            PacketTypesOut.Animation,
            PacketTypesOut.EntityAction,
            PacketTypesOut.SteerVehicle,
            PacketTypesOut.CloseWindow,
            PacketTypesOut.ClickWindow,
            PacketTypesOut.WindowConfirmation,
            PacketTypesOut.CreativeInventoryAction,
            PacketTypesOut.Unknown, // EnchantItem
            PacketTypesOut.UpdateSign,
            PacketTypesOut.PlayerAbilities,
            PacketTypesOut.TabComplete,
            PacketTypesOut.ClientSettings,
            PacketTypesOut.ClientStatus,
            PacketTypesOut.PluginMessage,
            PacketTypesOut.Spectate,
            PacketTypesOut.ResourcePackStatus
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
