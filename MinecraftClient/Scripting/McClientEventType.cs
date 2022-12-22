using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Scripting
{
    public enum McClientEventType
    {
        /* Internal Events */
        Initialize,
        ClientDisconnect, /* Tuple<ChatBot.DisconnectReason, string>(reason, message) */
        ClientTick,
        InternalCommand,
        NetworkPacket, /* Tuple<int, byte[], bool, bool>(packetID, packetData, isLogin, isInbound) */
        ServerTpsUpdate, /* double(tps) */

        /* General in-game events */
        BlockBreakAnimation,
        BlockChange,
        Enchantments, /* EnchantmentData(lastEnchantment) */
        Explosion, /* Tuple<Location, float, int>(location, strength, affectedBlocks) */
        GameJoin,
        MapDataReceive,
        PluginMessage,
        RainLevelChange,
        ScoreboardUpdate,
        ScoreUpdate,
        TextReceive, /* Tuple<string, string>(messageText, message.content) */
        ThunderLevelChange,
        TimeUpdate, /* Tuple<long, long>(WorldAge, TimeOfDay) */
        TitleReceive, /* TitlePacket(title) */
        TradeListReceive,

        /* Player Events */
        PlayerLatencyUpdate, /* Tuple<PlayerInfo, int>(player, latency) */
        PlayerJoin, /* PlayerInfo(player) */
        PlayerKilled, /* Tuple<Entity, string>(killer, chatMessage) */
        PlayerLeave, /* Tuple<Guid, PlayerInfo?>(uuid, playerInfo) */
        PlayerPropertyReceive, /* Dictionary<string, double>(prop) */
        PlayerStatusUpdate, /* byte(status) */

        /* Player's own events */
        Death, /* null */
        ExperienceChange, /* Tuple<float, int, int>(Experiencebar, Level, TotalExperience) */
        GamemodeUpdate, /* Tuple<PlayerInfo, int>(player, gamemode) */
        HealthUpdate, /* Tuple<float, int>(health, food) */
        HeldItemChange, /* byte(slot) */
        Respawn,

        /* Inventory-related events */
        InventoryClose, /* int(inventoryID) */
        InventoryOpen, /* int(inventoryID) */
        InventoryProperties, /* Tuple<int, int, int>(inventoryID, propertyId, propertyValue) */
        InventoryUpdate, /* int(inventoryID) */

        /* Entity-related events */
        EntityAnimation,
        EntityDespawn, /* Entity(entity) */
        EntityEffect, /* Tuple<Entity, Effect>(entity, effect) */
        EntityEquipment, /* Tuple<Entity, int, Item?>(entity, slot, item) */
        EntityHealth,
        EntityMetadata,
        EntityMove, /* Entity(entity) */
        EntitySpawn, /* Entity(entity) */
    };
}
