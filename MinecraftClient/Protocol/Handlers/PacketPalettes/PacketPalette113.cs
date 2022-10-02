using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes
{
    public class PacketPalette113 : PacketTypePalette
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
            { 0x08, PacketTypesIn.BlockBreakAnimation },
            { 0x09, PacketTypesIn.BlockEntityData },
            { 0x0A, PacketTypesIn.BlockAction },
            { 0x0B, PacketTypesIn.BlockChange },
            { 0x0C, PacketTypesIn.BossBar },
            { 0x0D, PacketTypesIn.ServerDifficulty },
            { 0x0E, PacketTypesIn.ChatMessage },
            { 0x0F, PacketTypesIn.MultiBlockChange },
            { 0x10, PacketTypesIn.TabComplete },
            { 0x11, PacketTypesIn.DeclareCommands },
            { 0x12, PacketTypesIn.WindowConfirmation },
            { 0x13, PacketTypesIn.CloseWindow },
            { 0x14, PacketTypesIn.OpenWindow },
            { 0x15, PacketTypesIn.WindowItems },
            { 0x16, PacketTypesIn.WindowProperty },
            { 0x17, PacketTypesIn.SetSlot },
            { 0x18, PacketTypesIn.SetCooldown },
            { 0x19, PacketTypesIn.PluginMessage },
            { 0x1A, PacketTypesIn.NamedSoundEffect },
            { 0x1B, PacketTypesIn.Disconnect },
            { 0x1C, PacketTypesIn.EntityStatus },
            { 0x1D, PacketTypesIn.NBTQueryResponse },
            { 0x1E, PacketTypesIn.Explosion },
            { 0x1F, PacketTypesIn.UnloadChunk },
            { 0x20, PacketTypesIn.ChangeGameState },
            { 0x21, PacketTypesIn.KeepAlive },
            { 0x22, PacketTypesIn.ChunkData },
            { 0x23, PacketTypesIn.Effect },
            { 0x24, PacketTypesIn.Particle },
            { 0x25, PacketTypesIn.JoinGame },
            { 0x26, PacketTypesIn.MapData },
            { 0x27, PacketTypesIn.EntityMovement },
            { 0x28, PacketTypesIn.EntityPosition },
            { 0x29, PacketTypesIn.EntityPositionAndRotation },
            { 0x2A, PacketTypesIn.EntityRotation },
            { 0x2B, PacketTypesIn.VehicleMove },
            { 0x2C, PacketTypesIn.OpenSignEditor },
            { 0x2D, PacketTypesIn.CraftRecipeResponse },
            { 0x2E, PacketTypesIn.PlayerAbilities },
            { 0x2F, PacketTypesIn.CombatEvent },
            { 0x30, PacketTypesIn.PlayerInfo },
            { 0x31, PacketTypesIn.FacePlayer },
            { 0x32, PacketTypesIn.PlayerPositionAndLook },
            { 0x33, PacketTypesIn.UseBed },
            { 0x34, PacketTypesIn.UnlockRecipes },
            { 0x35, PacketTypesIn.DestroyEntities },
            { 0x36, PacketTypesIn.RemoveEntityEffect },
            { 0x37, PacketTypesIn.ResourcePackSend },
            { 0x38, PacketTypesIn.Respawn },
            { 0x39, PacketTypesIn.EntityHeadLook },
            { 0x3A, PacketTypesIn.SelectAdvancementTab },
            { 0x3B, PacketTypesIn.WorldBorder },
            { 0x3C, PacketTypesIn.Camera },
            { 0x3D, PacketTypesIn.HeldItemChange },
            { 0x3E, PacketTypesIn.DisplayScoreboard },
            { 0x3F, PacketTypesIn.EntityMetadata },
            { 0x40, PacketTypesIn.AttachEntity },
            { 0x41, PacketTypesIn.EntityVelocity },
            { 0x42, PacketTypesIn.EntityEquipment },
            { 0x43, PacketTypesIn.SetExperience },
            { 0x44, PacketTypesIn.UpdateHealth },
            { 0x45, PacketTypesIn.ScoreboardObjective },
            { 0x46, PacketTypesIn.SetPassengers },
            { 0x47, PacketTypesIn.Teams },
            { 0x48, PacketTypesIn.UpdateScore },
            { 0x49, PacketTypesIn.SpawnPosition },
            { 0x4A, PacketTypesIn.TimeUpdate },
            { 0x4B, PacketTypesIn.Title },
            { 0x4C, PacketTypesIn.StopSound },
            { 0x4D, PacketTypesIn.SoundEffect },
            { 0x4E, PacketTypesIn.PlayerListHeaderAndFooter },
            { 0x4F, PacketTypesIn.CollectItem },
            { 0x50, PacketTypesIn.EntityTeleport },
            { 0x51, PacketTypesIn.Advancements },
            { 0x52, PacketTypesIn.EntityProperties },
            { 0x53, PacketTypesIn.EntityEffect },
            { 0x54, PacketTypesIn.DeclareRecipes },
            { 0x55, PacketTypesIn.Tags },
        };

        private readonly Dictionary<int, PacketTypesOut> typeOut = new()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },
            { 0x01, PacketTypesOut.QueryBlockNBT },
            { 0x02, PacketTypesOut.ChatMessage },
            { 0x03, PacketTypesOut.ClientStatus },
            { 0x04, PacketTypesOut.ClientSettings },
            { 0x05, PacketTypesOut.TabComplete },
            { 0x06, PacketTypesOut.WindowConfirmation },
            { 0x07, PacketTypesOut.EnchantItem },
            { 0x08, PacketTypesOut.ClickWindow },
            { 0x09, PacketTypesOut.CloseWindow },
            { 0x0A, PacketTypesOut.PluginMessage },
            { 0x0B, PacketTypesOut.EditBook },
            { 0x0C, PacketTypesOut.EntityNBTRequest },
            { 0x0D, PacketTypesOut.InteractEntity },
            { 0x0E, PacketTypesOut.KeepAlive },
            { 0x0F, PacketTypesOut.PlayerMovement },
            { 0x10, PacketTypesOut.PlayerPosition },
            { 0x11, PacketTypesOut.PlayerPositionAndRotation },
            { 0x12, PacketTypesOut.PlayerRotation },
            { 0x13, PacketTypesOut.VehicleMove },
            { 0x14, PacketTypesOut.SteerBoat },
            { 0x15, PacketTypesOut.PickItem },
            { 0x16, PacketTypesOut.CraftRecipeRequest },
            { 0x17, PacketTypesOut.PlayerAbilities },
            { 0x18, PacketTypesOut.PlayerDigging },
            { 0x19, PacketTypesOut.EntityAction },
            { 0x1A, PacketTypesOut.SteerVehicle },
            { 0x1B, PacketTypesOut.RecipeBookData },
            { 0x1C, PacketTypesOut.NameItem },
            { 0x1D, PacketTypesOut.ResourcePackStatus },
            { 0x1E, PacketTypesOut.AdvancementTab },
            { 0x1F, PacketTypesOut.SelectTrade },
            { 0x20, PacketTypesOut.SetBeaconEffect },
            { 0x21, PacketTypesOut.HeldItemChange },
            { 0x22, PacketTypesOut.UpdateCommandBlock },
            { 0x23, PacketTypesOut.UpdateCommandBlockMinecart },
            { 0x24, PacketTypesOut.CreativeInventoryAction },
            { 0x25, PacketTypesOut.UpdateStructureBlock },
            { 0x26, PacketTypesOut.UpdateSign },
            { 0x27, PacketTypesOut.Animation },
            { 0x28, PacketTypesOut.Spectate },
            { 0x29, PacketTypesOut.PlayerBlockPlacement },
            { 0x2A, PacketTypesOut.UseItem },
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
