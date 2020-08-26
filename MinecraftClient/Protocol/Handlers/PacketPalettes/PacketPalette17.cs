using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes
{
    public class PacketPalette17 : PacketTypePalette
    {
        private Dictionary<int, PacketTypesIn> typeIn = new Dictionary<int, PacketTypesIn>()
        {
            { 0x00, PacketTypesIn.KeepAlive },
            { 0x01, PacketTypesIn.JoinGame },
            { 0x02, PacketTypesIn.ChatMessage },
            { 0x03, PacketTypesIn.TimeUpdate },
            { 0x04, PacketTypesIn.EntityEquipment },
            { 0x05, PacketTypesIn.SpawnPosition },
            { 0x06, PacketTypesIn.UpdateHealth },
            { 0x07, PacketTypesIn.Respawn },
            { 0x08, PacketTypesIn.PlayerPositionAndLook },
            { 0x09, PacketTypesIn.HeldItemChange },
            { 0x0A, PacketTypesIn.UseBed },
            { 0x0B, PacketTypesIn.EntityAnimation },
            { 0x0C, PacketTypesIn.SpawnPlayer },
            { 0x0D, PacketTypesIn.CollectItem },
            { 0x0E, PacketTypesIn.SpawnEntity },
            { 0x0F, PacketTypesIn.SpawnLivingEntity },
            { 0x10, PacketTypesIn.SpawnPainting },
            { 0x11, PacketTypesIn.SpawnExperienceOrb },
            { 0x12, PacketTypesIn.EntityVelocity },
            { 0x13, PacketTypesIn.DestroyEntities },
            { 0x14, PacketTypesIn.EntityMovement },
            { 0x15, PacketTypesIn.EntityPosition },
            { 0x16, PacketTypesIn.EntityRotation },
            { 0x17, PacketTypesIn.EntityPositionAndRotation },
            { 0x18, PacketTypesIn.EntityTeleport },
            { 0x19, PacketTypesIn.EntityHeadLook },
            { 0x1A, PacketTypesIn.EntityStatus },
            { 0x1B, PacketTypesIn.AttachEntity },
            { 0x1C, PacketTypesIn.EntityMetadata },
            { 0x1D, PacketTypesIn.EntityEffect },
            { 0x1E, PacketTypesIn.RemoveEntityEffect },
            { 0x1F, PacketTypesIn.SetExperience },
            { 0x20, PacketTypesIn.EntityProperties },
            { 0x21, PacketTypesIn.ChunkData },
            { 0x22, PacketTypesIn.MultiBlockChange },
            { 0x23, PacketTypesIn.BlockChange },
            { 0x24, PacketTypesIn.BlockAction },
            { 0x25, PacketTypesIn.BlockBreakAnimation },
            { 0x26, PacketTypesIn.MapChunkBulk },
            { 0x27, PacketTypesIn.Explosion },
            { 0x28, PacketTypesIn.Effect },
            { 0x29, PacketTypesIn.SoundEffect },
            { 0x2A, PacketTypesIn.Particle },
            { 0x2B, PacketTypesIn.ChangeGameState },
            { 0x2C, PacketTypesIn.SpawnWeatherEntity },
            { 0x2D, PacketTypesIn.OpenWindow },
            { 0x2E, PacketTypesIn.CloseWindow },
            { 0x2F, PacketTypesIn.SetSlot },
            { 0x30, PacketTypesIn.WindowItems },
            { 0x31, PacketTypesIn.WindowProperty },
            { 0x32, PacketTypesIn.WindowConfirmation },
            { 0x33, PacketTypesIn.UpdateSign },
            { 0x34, PacketTypesIn.MapData },
            { 0x35, PacketTypesIn.BlockEntityData },
            { 0x36, PacketTypesIn.OpenSignEditor },
            { 0x37, PacketTypesIn.Statistics },
            { 0x38, PacketTypesIn.PlayerInfo },
            { 0x39, PacketTypesIn.PlayerAbilities },
            { 0x3A, PacketTypesIn.TabComplete },
            { 0x3B, PacketTypesIn.ScoreboardObjective },
            { 0x3C, PacketTypesIn.UpdateScore },
            { 0x3D, PacketTypesIn.DisplayScoreboard },
            { 0x3E, PacketTypesIn.Teams },
            { 0x3F, PacketTypesIn.PluginMessage },
            { 0x40, PacketTypesIn.Disconnect },
            { 0x41, PacketTypesIn.ServerDifficulty },
            { 0x42, PacketTypesIn.CombatEvent },
            { 0x43, PacketTypesIn.Camera },
            { 0x44, PacketTypesIn.WorldBorder },
            { 0x45, PacketTypesIn.Title },
            { 0x46, PacketTypesIn.SetCompression },
            { 0x47, PacketTypesIn.PlayerListHeaderAndFooter },
            { 0x48, PacketTypesIn.ResourcePackSend },
            { 0x49, PacketTypesIn.UpdateEntityNBT },
        };

        private Dictionary<int, PacketTypesOut> typeOut = new Dictionary<int, PacketTypesOut>()
        {
            { 0x00, PacketTypesOut.KeepAlive },
            { 0x01, PacketTypesOut.ChatMessage },
            { 0x02, PacketTypesOut.InteractEntity },
            { 0x03, PacketTypesOut.PlayerMovement },
            { 0x04, PacketTypesOut.PlayerPosition },
            { 0x05, PacketTypesOut.PlayerRotation },
            { 0x06, PacketTypesOut.PlayerPositionAndRotation },
            { 0x07, PacketTypesOut.PlayerDigging },
            { 0x08, PacketTypesOut.PlayerBlockPlacement },
            { 0x09, PacketTypesOut.HeldItemChange },
            { 0x0A, PacketTypesOut.Animation },
            { 0x0B, PacketTypesOut.EntityAction },
            { 0x0C, PacketTypesOut.SteerVehicle },
            { 0x0D, PacketTypesOut.CloseWindow },
            { 0x0E, PacketTypesOut.ClickWindow },
            { 0x0F, PacketTypesOut.WindowConfirmation },
            { 0x10, PacketTypesOut.CreativeInventoryAction },
            { 0x11, PacketTypesOut.EnchantItem },
            { 0x12, PacketTypesOut.UpdateSign },
            { 0x13, PacketTypesOut.PlayerAbilities },
            { 0x14, PacketTypesOut.TabComplete },
            { 0x15, PacketTypesOut.ClientSettings },
            { 0x16, PacketTypesOut.ClientStatus },
            { 0x17, PacketTypesOut.PluginMessage },
            { 0x18, PacketTypesOut.Spectate },
            { 0x19, PacketTypesOut.ResourcePackStatus },
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
