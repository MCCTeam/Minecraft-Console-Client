using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MinecraftClient.Mcp;

[McpServerToolType]
public sealed class MccMcpToolSet
{
    private readonly IMccMcpCapabilities capabilities;

    public MccMcpToolSet(IMccMcpCapabilities capabilities)
    {
        this.capabilities = capabilities;
    }

    [McpServerTool(Name = "mcc_session_status"), Description("Get current MCC session and feature status.")]
    public object SessionStatus()
    {
        return capabilities.GetSessionStatus();
    }

    [McpServerTool(Name = "mcc_server_info"), Description("Get active MCC server connection info and current TPS.")]
    public object ServerInfo()
    {
        return capabilities.GetServerInfo();
    }

    [McpServerTool(Name = "mcc_player_state"), Description("Get current controlled player state.")]
    public object PlayerState()
    {
        return capabilities.GetPlayerState();
    }

    [McpServerTool(Name = "mcc_players_list"), Description("List currently known online players.")]
    public object PlayersList()
    {
        return capabilities.GetPlayersList();
    }

    [McpServerTool(Name = "mcc_chat_history"), Description("Get recent chat/system lines seen by MCC.")]
    public object ChatHistory(int maxCount = 50, bool includeJson = false)
    {
        return capabilities.GetChatHistory(maxCount, includeJson);
    }

    [McpServerTool(Name = "mcc_internal_commands_list"), Description("List available MCC internal commands with usage and description.")]
    public object InternalCommandsList()
    {
        return capabilities.GetInternalCommands();
    }

    [McpServerTool(Name = "mcc_materials_list"), Description("List known MCC material names with optional filtering.")]
    public object MaterialsList(string? filter = null, int maxCount = 500)
    {
        return capabilities.GetMaterialsList(filter, maxCount);
    }

    [McpServerTool(Name = "mcc_block_types_list"), Description("List known MCC block type names with optional filtering.")]
    public object BlockTypesList(string? filter = null, int maxCount = 500)
    {
        return capabilities.GetBlockTypesList(filter, maxCount);
    }

    [McpServerTool(Name = "mcc_entity_types_list"), Description("List known MCC entity type names with optional filtering.")]
    public object EntityTypesList(string? filter = null, int maxCount = 500)
    {
        return capabilities.GetEntityTypesList(filter, maxCount);
    }

    [McpServerTool(Name = "mcc_send_chat"), Description("Send chat text or slash-command to the connected Minecraft server.")]
    public object SendChat([Description("Text to send to server chat.")] string text)
    {
        return capabilities.SendChat(text);
    }

    [McpServerTool(Name = "mcc_quit_client"), Description("Quit MCC client process cleanly.")]
    public object QuitClient()
    {
        return capabilities.QuitClient();
    }

    [McpServerTool(Name = "mcc_run_internal_command"), Description("Run an internal MCC command.")]
    public object RunInternalCommand([Description("MCC command line without leading slash.")] string command)
    {
        return capabilities.RunInternalCommand(command);
    }

    [McpServerTool(Name = "mcc_change_hotbar_slot"), Description("Change active hotbar slot (1-9).")]
    public object ChangeHotbarSlot(int slot)
    {
        return capabilities.ChangeHotbarSlot(slot);
    }

    [McpServerTool(Name = "mcc_use_item_on_hand"), Description("Use the currently held item.")]
    public object UseItemOnHand()
    {
        return capabilities.UseItemOnHand();
    }

    [McpServerTool(Name = "mcc_use_item_on_block"), Description("Use currently held item on a target block location.")]
    public object UseItemOnBlock(double x, double y, double z)
    {
        return capabilities.UseItemOnBlock(x, y, z);
    }

    [McpServerTool(Name = "mcc_dig_block"), Description("Dig a block at target location.")]
    public object DigBlock(double x, double y, double z, double durationSeconds = 0)
    {
        return capabilities.DigBlock(x, y, z, durationSeconds);
    }

    [McpServerTool(Name = "mcc_place_block"), Description("Place the currently held block/item at a target block location.")]
    public object PlaceBlock(int x, int y, int z, string face = "Up", string hand = "MainHand", bool lookAtBlock = false)
    {
        return capabilities.PlaceBlock(x, y, z, face, hand, lookAtBlock);
    }

    [McpServerTool(Name = "mcc_entity_interact"), Description("Interact with a tracked entity.")]
    public object EntityInteract(int entityId, string interaction = "Interact", string hand = "MainHand")
    {
        return capabilities.InteractEntity(entityId, interaction, hand);
    }

    [McpServerTool(Name = "mcc_block_scan"), Description("Scan nearby blocks around player location.")]
    public object BlockScan(int radius = 3, int maxCount = 200, string? materialFilter = null)
    {
        return capabilities.ScanNearbyBlocks(radius, maxCount, materialFilter);
    }

    [McpServerTool(Name = "mcc_blocks_find"), Description("Find nearby blocks by block name/type query or block ID.")]
    public object BlocksFind(string? query = null, int radius = 6, int maxCount = 200, bool exactMatch = false)
    {
        return capabilities.FindBlocks(query, radius, maxCount, exactMatch);
    }

    [McpServerTool(Name = "mcc_player_nearby"), Description("Check if any player, or a specific player, is nearby.")]
    public object PlayerNearby(string? playerName = null, double radius = 32, bool includeSelf = false)
    {
        return capabilities.IsPlayerNearby(playerName, radius, includeSelf);
    }

    [McpServerTool(Name = "mcc_player_locate"), Description("Locate a tracked player entity by name and return exact coordinates when available.")]
    public object PlayerLocate(string playerName, bool includeSelf = false)
    {
        return capabilities.LocatePlayer(playerName, includeSelf);
    }

    [McpServerTool(Name = "mcc_can_reach_position"), Description("Check whether MCC can currently path to a world coordinate without moving there.")]
    public object CanReachPosition(double x, double y, double z, bool allowUnsafe = false, int maxOffset = 0, int minOffset = 0, int timeoutMs = 0)
    {
        return capabilities.CanReachPosition(x, y, z, allowUnsafe, maxOffset, minOffset, timeoutMs);
    }

    [McpServerTool(Name = "mcc_move_to"), Description("Request movement/pathing to a world coordinate and verify arrival.")]
    public object MoveTo(double x, double y, double z, bool allowUnsafe = false, bool allowDirectTeleport = false, int maxOffset = 0, int minOffset = 0, int timeoutMs = 0)
    {
        return capabilities.MoveTo(x, y, z, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeoutMs);
    }

    [McpServerTool(Name = "mcc_move_to_player"), Description("Locate a tracked player entity, request movement/pathing, and verify arrival.")]
    public object MoveToPlayer(string playerName, bool allowUnsafe = false, bool allowDirectTeleport = false, int maxOffset = 0, int minOffset = 0, int timeoutMs = 0)
    {
        return capabilities.MoveToPlayer(playerName, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeoutMs);
    }

    [McpServerTool(Name = "mcc_look_at"), Description("Rotate player view toward world coordinates.")]
    public object LookAt(double x, double y, double z)
    {
        return capabilities.LookAt(x, y, z);
    }

    [McpServerTool(Name = "mcc_inventory_snapshot"), Description("Get a snapshot of one inventory.")]
    public object InventorySnapshot([Description("Inventory ID. 0 is the player inventory.")] int inventoryId = 0)
    {
        return capabilities.GetInventorySnapshot(inventoryId);
    }

    [McpServerTool(Name = "mcc_inventories_list"), Description("List currently open inventories and containers known to MCC.")]
    public object InventoriesList()
    {
        return capabilities.ListInventories();
    }

    [McpServerTool(Name = "mcc_container_open_at"), Description("Open an interactable container block at world coordinates and wait for the container inventory to appear.")]
    public object ContainerOpenAt(int x, int y, int z, int timeoutMs = 0, bool closeCurrent = true)
    {
        return capabilities.OpenContainerAt(x, y, z, timeoutMs, closeCurrent);
    }

    [McpServerTool(Name = "mcc_container_close"), Description("Close an open non-player container. Use inventoryId=-1 to close the active container.")]
    public object ContainerClose([Description("Container inventory ID, or -1 for the active non-player container.")] int inventoryId = -1, int timeoutMs = 0)
    {
        return capabilities.CloseContainer(inventoryId, timeoutMs);
    }

    [McpServerTool(Name = "mcc_inventory_window_action"), Description("Perform a window action on an inventory slot.")]
    public object InventoryWindowAction(int inventoryId, int slotId, [Description("WindowActionType enum name, e.g. LeftClick or ShiftClick.")] string actionType)
    {
        return capabilities.InventoryWindowAction(inventoryId, slotId, actionType);
    }

    [McpServerTool(Name = "mcc_inventory_drop_item"), Description("Drop an exact item count from an inventory by item type.")]
    public object InventoryDropItem(
        [Description("Item type enum name (e.g. Diamond).")] string itemType,
        [Description("Exact number of items to drop.")] int count,
        [Description("Inventory ID. 0 is the player inventory.")] int inventoryId = 0,
        [Description("Prefer dropping from larger stacks first when true.")] bool preferStack = false)
    {
        return capabilities.DropInventoryItem(itemType, count, inventoryId, preferStack);
    }

    [McpServerTool(Name = "mcc_container_deposit_item"), Description("Move an exact item count from the player inventory into an open container and verify the transfer.")]
    public object ContainerDepositItem(
        [Description("Item type enum name (e.g. Diamond).")] string itemType,
        [Description("Exact number of items to move into the container.")] int count,
        [Description("Container inventory ID, or -1 for the active non-player container.")] int inventoryId = -1,
        [Description("Prefer larger source stacks first when true.")] bool preferLargestStack = true)
    {
        return capabilities.DepositContainerItem(itemType, count, inventoryId, preferLargestStack);
    }

    [McpServerTool(Name = "mcc_container_withdraw_item"), Description("Move an exact item count from an open container into the player inventory and verify the transfer.")]
    public object ContainerWithdrawItem(
        [Description("Item type enum name (e.g. Diamond).")] string itemType,
        [Description("Exact number of items to move into the player inventory.")] int count,
        [Description("Container inventory ID, or -1 for the active non-player container.")] int inventoryId = -1,
        [Description("Prefer larger source stacks first when true.")] bool preferLargestStack = true)
    {
        return capabilities.WithdrawContainerItem(itemType, count, inventoryId, preferLargestStack);
    }

    [McpServerTool(Name = "mcc_entities_query"), Description("Query tracked entities.")]
    public object EntitiesQuery([Description("Maximum entities to return.")] int maxCount = 50)
    {
        return capabilities.QueryEntities(maxCount);
    }

    [McpServerTool(Name = "mcc_entities_list"), Description("List tracked entities with optional type and radius filtering.")]
    public object EntitiesList(int maxCount = 100, string? typeFilter = null, double radius = 0)
    {
        return capabilities.ListEntities(maxCount, typeFilter, radius);
    }

    [McpServerTool(Name = "mcc_entity_info"), Description("Get detailed info for one tracked entity.")]
    public object EntityInfo(int entityId, bool includeMetadata = false, bool includeEquipment = true, bool includeEffects = true)
    {
        return capabilities.GetEntityInfo(entityId, includeMetadata, includeEquipment, includeEffects);
    }

    [McpServerTool(Name = "mcc_signs_find"), Description("Find nearby signs whose text exactly matches or contains the requested text.")]
    public object SignsFind(string text, bool exactMatch = false, int radius = 16, int maxCount = 50, bool includeBackText = true)
    {
        return capabilities.FindSigns(text, exactMatch, radius, maxCount, includeBackText);
    }

    [McpServerTool(Name = "mcc_items_list"), Description("List nearby dropped item entities with optional item type filtering.")]
    public object ItemsList(string? itemType = null, double radius = 32, int maxCount = 100)
    {
        return capabilities.ListItemEntities(itemType, radius, maxCount);
    }

    [McpServerTool(Name = "mcc_items_pickup"), Description("Move to and pick up nearby dropped items of a given item type.")]
    public object ItemsPickup(string itemType, double radius = 32, int maxItems = 20, bool allowUnsafe = false, int timeoutMs = 0)
    {
        return capabilities.PickupItems(itemType, radius, maxItems, allowUnsafe, timeoutMs);
    }

    [McpServerTool(Name = "mcc_world_block_at"), Description("Get block information at world coordinates.")]
    public object WorldBlockAt(int x, int y, int z)
    {
        return capabilities.GetWorldBlockAt(x, y, z);
    }
}
