﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.PacketTypes.Palettes
{
    class PacketPalette1162 : PacketTypePalette
    {
        private List<PacketTypesIn> typeIn = new List<PacketTypesIn>()
        {
            PacketTypesIn.SpawnEntity,
            PacketTypesIn.SpawnExperienceOrb,
            PacketTypesIn.SpawnLivingEntity,
            PacketTypesIn.SpawnPainting,
            PacketTypesIn.SpawnPlayer,
            PacketTypesIn.EntityAnimation,
            PacketTypesIn.Statistics,
            PacketTypesIn.AcknowledgePlayerDigging,
            PacketTypesIn.BlockBreakAnimation,
            PacketTypesIn.BlockEntityData,
            PacketTypesIn.BlockAction,
            PacketTypesIn.BlockChange,
            PacketTypesIn.BossBar,
            PacketTypesIn.ServerDifficulty,
            PacketTypesIn.ChatMessage,
            
            PacketTypesIn.TabComplete,
            PacketTypesIn.DeclareCommands,
            PacketTypesIn.WindowConfirmation,
            PacketTypesIn.CloseWindow,
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
            PacketTypesIn.OpenHorseWindow,
            PacketTypesIn.KeepAlive,
            PacketTypesIn.ChunkData,
            PacketTypesIn.Effect,
            PacketTypesIn.Particle,
            PacketTypesIn.UpdateLight,
            PacketTypesIn.JoinGame,
            PacketTypesIn.MapData,
            PacketTypesIn.TradeList,
            PacketTypesIn.EntityPosition,
            PacketTypesIn.EntityPositionAndRotation,
            PacketTypesIn.EntityRotation,
            PacketTypesIn.EntityMovement,
            PacketTypesIn.VehicleMove,
            PacketTypesIn.OpenBook,
            PacketTypesIn.OpenWindow,
            PacketTypesIn.OpenSignEditor,
            PacketTypesIn.CraftRecipeResponse,
            PacketTypesIn.PlayerAbilities,
            PacketTypesIn.CombatEvent,
            PacketTypesIn.PlayerListUpdate,
            PacketTypesIn.FacePlayer,
            PacketTypesIn.PlayerPositionAndLook,
            PacketTypesIn.UnlockRecipes,
            PacketTypesIn.DestroyEntities,
            PacketTypesIn.RemoveEntityEffect,
            PacketTypesIn.ResourcePackSend,
            PacketTypesIn.Respawn,
            PacketTypesIn.EntityHeadLook,
            PacketTypesIn.MultiBlockChange,
            PacketTypesIn.SelectAdvancementTab,
            PacketTypesIn.WorldBorder,
            PacketTypesIn.Camera,
            PacketTypesIn.HeldItemChange,
            PacketTypesIn.UpdateViewPosition,
            PacketTypesIn.UpdateViewDistance,
            PacketTypesIn.SpawnPosition,
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
            PacketTypesIn.TimeUpdate,
            PacketTypesIn.Title,
            PacketTypesIn.EntitySoundEffect,
            PacketTypesIn.SoundEffect,
            PacketTypesIn.StopSound,
            PacketTypesIn.PlayerListHeaderAndFooter,
            PacketTypesIn.NBTQueryResponse,
            PacketTypesIn.CollectItem,
            PacketTypesIn.EntityTeleport,
            PacketTypesIn.Advancements,
            PacketTypesIn.EntityProperties,
            PacketTypesIn.EntityEffect,
            PacketTypesIn.DeclareRecipes,
            PacketTypesIn.Tags,
        };

        private List<PacketTypesOut> typeOut = new List<PacketTypesOut>()
        {
            PacketTypesOut.TeleportConfirm,
            PacketTypesOut.QueryBlockNBT,
            PacketTypesOut.SetDifficulty,
            PacketTypesOut.ChatMessage,
            PacketTypesOut.ClientStatus,
            PacketTypesOut.ClientSettings,
            PacketTypesOut.TabComplete,
            PacketTypesOut.WindowConfirmation,
            PacketTypesOut.ClickWindowButton,
            PacketTypesOut.ClickWindow,
            PacketTypesOut.CloseWindow,
            PacketTypesOut.PluginMessage,
            PacketTypesOut.EditBook,
            PacketTypesOut.EntityNBTRequest,
            PacketTypesOut.InteractEntity,
            PacketTypesOut.GenerateStructure,
            PacketTypesOut.KeepAlive,
            PacketTypesOut.LockDifficulty,
            PacketTypesOut.PlayerPosition,
            PacketTypesOut.PlayerPositionAndRotation,
            PacketTypesOut.PlayerRotation,
            PacketTypesOut.PlayerMovement,
            PacketTypesOut.VehicleMove,
            PacketTypesOut.SteerBoat,
            PacketTypesOut.PickItem,
            PacketTypesOut.CraftRecipeRequest,
            PacketTypesOut.PlayerAbilities,
            PacketTypesOut.PlayerDigging,
            PacketTypesOut.EntityAction,
            PacketTypesOut.SteerVehicle,
            PacketTypesOut.SetDisplayedRecipe,
            PacketTypesOut.SetRecipeBookState,
            PacketTypesOut.NameItem,
            PacketTypesOut.ResourcePackStatus,
            PacketTypesOut.AdvancementTab,
            PacketTypesOut.SelectTrade,
            PacketTypesOut.SetBeaconEffect,
            PacketTypesOut.HeldItemChange,
            PacketTypesOut.UpdateCommandBlock,
            PacketTypesOut.UpdateCommandBlockMinecart,
            PacketTypesOut.CreativeInventoryAction,
            PacketTypesOut.UpdateJigsawBlock,
            PacketTypesOut.UpdateStructureBlock,
            PacketTypesOut.UpdateSign,
            PacketTypesOut.Animation,
            PacketTypesOut.Spectate,
            PacketTypesOut.PlayerBlockPlacement,
            PacketTypesOut.UseItem,
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
