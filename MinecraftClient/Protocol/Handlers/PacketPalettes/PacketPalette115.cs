﻿using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes
{
    public class PacketPalette115 : PacketTypePalette
    {
        private readonly Dictionary<int, PacketTypesIn> typeIn = new()
        {
            { 0x00, PacketTypesIn.SpawnEntity },
            { 0x01, PacketTypesIn.SpawnExperienceOrb },
            { 0x02, PacketTypesIn.SpawnWeatherEntity },
            { 0x03, PacketTypesIn.SpawnLivingEntity },
            { 0x04, PacketTypesIn.SpawnPainting },
            { 0x05, PacketTypesIn.SpawnPlayer },
            { 0x06, PacketTypesIn.EntityAnimation },
            { 0x07, PacketTypesIn.Statistics },
            { 0x08, PacketTypesIn.AcknowledgePlayerDigging },
            { 0x09, PacketTypesIn.BlockBreakAnimation },
            { 0x0A, PacketTypesIn.BlockEntityData },
            { 0x0B, PacketTypesIn.BlockAction },
            { 0x0C, PacketTypesIn.BlockChange },
            { 0x0D, PacketTypesIn.BossBar },
            { 0x0E, PacketTypesIn.ServerDifficulty },
            { 0x0F, PacketTypesIn.ChatMessage },
            { 0x10, PacketTypesIn.MultiBlockChange },
            { 0x11, PacketTypesIn.TabComplete },
            { 0x12, PacketTypesIn.DeclareCommands },
            { 0x13, PacketTypesIn.WindowConfirmation },
            { 0x14, PacketTypesIn.CloseWindow },
            { 0x15, PacketTypesIn.WindowItems },
            { 0x16, PacketTypesIn.WindowProperty },
            { 0x17, PacketTypesIn.SetSlot },
            { 0x18, PacketTypesIn.SetCooldown },
            { 0x19, PacketTypesIn.PluginMessage },
            { 0x1A, PacketTypesIn.NamedSoundEffect },
            { 0x1B, PacketTypesIn.Disconnect },
            { 0x1C, PacketTypesIn.EntityStatus },
            { 0x1D, PacketTypesIn.Explosion },
            { 0x1E, PacketTypesIn.UnloadChunk },
            { 0x1F, PacketTypesIn.ChangeGameState },
            { 0x20, PacketTypesIn.OpenHorseWindow },
            { 0x21, PacketTypesIn.KeepAlive },
            { 0x22, PacketTypesIn.ChunkData },
            { 0x23, PacketTypesIn.Effect },
            { 0x24, PacketTypesIn.Particle },
            { 0x25, PacketTypesIn.UpdateLight },
            { 0x26, PacketTypesIn.JoinGame },
            { 0x27, PacketTypesIn.MapData },
            { 0x28, PacketTypesIn.TradeList },
            { 0x29, PacketTypesIn.EntityPosition },
            { 0x2A, PacketTypesIn.EntityPositionAndRotation },
            { 0x2B, PacketTypesIn.EntityRotation },
            { 0x2C, PacketTypesIn.EntityMovement },
            { 0x2D, PacketTypesIn.VehicleMove },
            { 0x2E, PacketTypesIn.OpenBook },
            { 0x2F, PacketTypesIn.OpenWindow },
            { 0x30, PacketTypesIn.OpenSignEditor },
            { 0x31, PacketTypesIn.CraftRecipeResponse },
            { 0x32, PacketTypesIn.PlayerAbilities },
            { 0x33, PacketTypesIn.CombatEvent },
            { 0x34, PacketTypesIn.PlayerInfo },
            { 0x35, PacketTypesIn.FacePlayer },
            { 0x36, PacketTypesIn.PlayerPositionAndLook },
            { 0x37, PacketTypesIn.UnlockRecipes },
            { 0x38, PacketTypesIn.DestroyEntities },
            { 0x39, PacketTypesIn.RemoveEntityEffect },
            { 0x3A, PacketTypesIn.ResourcePackSend },
            { 0x3B, PacketTypesIn.Respawn },
            { 0x3C, PacketTypesIn.EntityHeadLook },
            { 0x3D, PacketTypesIn.SelectAdvancementTab },
            { 0x3E, PacketTypesIn.WorldBorder },
            { 0x3F, PacketTypesIn.Camera },
            { 0x40, PacketTypesIn.HeldItemChange },
            { 0x41, PacketTypesIn.UpdateViewPosition },
            { 0x42, PacketTypesIn.UpdateViewDistance },
            { 0x43, PacketTypesIn.DisplayScoreboard },
            { 0x44, PacketTypesIn.EntityMetadata },
            { 0x45, PacketTypesIn.AttachEntity },
            { 0x46, PacketTypesIn.EntityVelocity },
            { 0x47, PacketTypesIn.EntityEquipment },
            { 0x48, PacketTypesIn.SetExperience },
            { 0x49, PacketTypesIn.UpdateHealth },
            { 0x4A, PacketTypesIn.ScoreboardObjective },
            { 0x4B, PacketTypesIn.SetPassengers },
            { 0x4C, PacketTypesIn.Teams },
            { 0x4D, PacketTypesIn.UpdateScore },
            { 0x4E, PacketTypesIn.SpawnPosition },
            { 0x4F, PacketTypesIn.TimeUpdate },
            { 0x50, PacketTypesIn.Title },
            { 0x51, PacketTypesIn.EntitySoundEffect },
            { 0x52, PacketTypesIn.SoundEffect },
            { 0x53, PacketTypesIn.StopSound },
            { 0x54, PacketTypesIn.PlayerListHeaderAndFooter },
            { 0x55, PacketTypesIn.NBTQueryResponse },
            { 0x56, PacketTypesIn.CollectItem },
            { 0x57, PacketTypesIn.EntityTeleport },
            { 0x58, PacketTypesIn.Advancements },
            { 0x59, PacketTypesIn.EntityProperties },
            { 0x5A, PacketTypesIn.EntityEffect },
            { 0x5B, PacketTypesIn.DeclareRecipes },
            { 0x5C, PacketTypesIn.Tags },
        };

        private readonly Dictionary<int, PacketTypesOut> typeOut = new()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },
            { 0x01, PacketTypesOut.QueryBlockNBT },
            { 0x02, PacketTypesOut.SetDifficulty },
            { 0x03, PacketTypesOut.ChatMessage },
            { 0x04, PacketTypesOut.ClientStatus },
            { 0x05, PacketTypesOut.ClientSettings },
            { 0x06, PacketTypesOut.TabComplete },
            { 0x07, PacketTypesOut.WindowConfirmation },
            { 0x08, PacketTypesOut.ClickWindowButton },
            { 0x09, PacketTypesOut.ClickWindow },
            { 0x0A, PacketTypesOut.CloseWindow },
            { 0x0B, PacketTypesOut.PluginMessage },
            { 0x0C, PacketTypesOut.EditBook },
            { 0x0D, PacketTypesOut.EntityNBTRequest },
            { 0x0E, PacketTypesOut.InteractEntity },
            { 0x0F, PacketTypesOut.KeepAlive },
            { 0x10, PacketTypesOut.LockDifficulty },
            { 0x11, PacketTypesOut.PlayerPosition },
            { 0x12, PacketTypesOut.PlayerPositionAndRotation },
            { 0x13, PacketTypesOut.PlayerRotation },
            { 0x14, PacketTypesOut.PlayerMovement },
            { 0x15, PacketTypesOut.VehicleMove },
            { 0x16, PacketTypesOut.SteerBoat },
            { 0x17, PacketTypesOut.PickItem },
            { 0x18, PacketTypesOut.CraftRecipeRequest },
            { 0x19, PacketTypesOut.PlayerAbilities },
            { 0x1A, PacketTypesOut.PlayerDigging },
            { 0x1B, PacketTypesOut.EntityAction },
            { 0x1C, PacketTypesOut.SteerVehicle },
            { 0x1D, PacketTypesOut.RecipeBookData },
            { 0x1E, PacketTypesOut.NameItem },
            { 0x1F, PacketTypesOut.ResourcePackStatus },
            { 0x20, PacketTypesOut.AdvancementTab },
            { 0x21, PacketTypesOut.SelectTrade },
            { 0x22, PacketTypesOut.SetBeaconEffect },
            { 0x23, PacketTypesOut.HeldItemChange },
            { 0x24, PacketTypesOut.UpdateCommandBlock },
            { 0x25, PacketTypesOut.UpdateCommandBlockMinecart },
            { 0x26, PacketTypesOut.CreativeInventoryAction },
            { 0x27, PacketTypesOut.UpdateJigsawBlock },
            { 0x28, PacketTypesOut.UpdateStructureBlock },
            { 0x29, PacketTypesOut.UpdateSign },
            { 0x2A, PacketTypesOut.Animation },
            { 0x2B, PacketTypesOut.Spectate },
            { 0x2C, PacketTypesOut.PlayerBlockPlacement },
            { 0x2D, PacketTypesOut.UseItem },
        };

        protected override Dictionary<int, PacketTypesIn> GetListIn() => typeIn;
        protected override Dictionary<int, PacketTypesOut> GetListOut() => typeOut;
        protected override Dictionary<int, ConfigurationPacketTypesIn> GetConfigurationListIn() => null!;
        protected override Dictionary<int, ConfigurationPacketTypesOut> GetConfigurationListOut() => null!;
    }
}
