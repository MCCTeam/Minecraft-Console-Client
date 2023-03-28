using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes
{
    public class PacketPalette18 : PacketTypePalette
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
            { 0x15, PacketTypesIn.EntityMovement },
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
            // Missing Chunk Bulk Data
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
            // Missing Update Sign
            { 0x34, PacketTypesIn.MapData },
            { 0x35, PacketTypesIn.BlockEntityData },
            { 0x36, PacketTypesIn.OpenSignEditor },
            { 0x37, PacketTypesIn.Statistics },
            // Missing Player List Item
            { 0x39, PacketTypesIn.PlayerAbilities },
            { 0x3A, PacketTypesIn.TabComplete },
            { 0x3B, PacketTypesIn.ScoreboardObjective },
            { 0x3C, PacketTypesIn.UpdateScore },
            { 0x3D, PacketTypesIn.DisplayScoreboard },
            { 0x3E, PacketTypesIn.Teams },
            { 0x3D, PacketTypesIn.PluginMessage },
            { 0x40, PacketTypesIn.Disconnect },
            { 0x41, PacketTypesIn.ServerDifficulty },
            { 0x42, PacketTypesIn.CombatEvent },
            { 0x43, PacketTypesIn.Camera },
            { 0x44, PacketTypesIn.WorldBorder },
            { 0x45, PacketTypesIn.Title },
            { 0x47, PacketTypesIn.PlayerListHeaderAndFooter },
            { 0x48, PacketTypesIn.ResourcePackSend },
            { 0x49, PacketTypesIn.UpdateEntityNBT }
        };

        private Dictionary<int, PacketTypesOut> typeOut = new Dictionary<int, PacketTypesOut>()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },
            { 0x01, PacketTypesOut.Unknown },
            { 0x02, PacketTypesOut.TabComplete },
            { 0x03, PacketTypesOut.ChatMessage },
            { 0x04, PacketTypesOut.ClientStatus },
            { 0x05, PacketTypesOut.ClientSettings },
            { 0x06, PacketTypesOut.WindowConfirmation },
            { 0x07, PacketTypesOut.EnchantItem },
            { 0x08, PacketTypesOut.ClickWindow },
            { 0x09, PacketTypesOut.CloseWindow },
            { 0x0A, PacketTypesOut.PluginMessage },
            { 0x0B, PacketTypesOut.InteractEntity },
            { 0x0C, PacketTypesOut.KeepAlive },
            { 0x0D, PacketTypesOut.PlayerMovement },
            { 0x0E, PacketTypesOut.PlayerPosition },
            { 0x0F, PacketTypesOut.PlayerPositionAndRotation },
            { 0x10, PacketTypesOut.PlayerRotation },
            { 0x11, PacketTypesOut.VehicleMove },
            { 0x12, PacketTypesOut.SteerBoat },
            { 0x13, PacketTypesOut.PlayerAbilities },
            { 0x14, PacketTypesOut.PlayerDigging },
            { 0x15, PacketTypesOut.EntityAction },
            { 0x16, PacketTypesOut.SteerVehicle },
            { 0x17, PacketTypesOut.RecipeBookData },
            { 0x18, PacketTypesOut.ResourcePackStatus },
            { 0x19, PacketTypesOut.AdvancementTab },
            { 0x1A, PacketTypesOut.HeldItemChange },
            { 0x1B, PacketTypesOut.CreativeInventoryAction },
            { 0x1C, PacketTypesOut.UpdateSign },
            { 0x1D, PacketTypesOut.Animation },
            { 0x1E, PacketTypesOut.Spectate },
            { 0x1F, PacketTypesOut.PlayerBlockPlacement },
            { 0x20, PacketTypesOut.UseItem },
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
