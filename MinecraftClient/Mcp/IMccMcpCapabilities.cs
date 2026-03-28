namespace MinecraftClient.Mcp;

public interface IMccMcpCapabilities
{
    MccMcpResult GetSessionStatus();
    MccMcpResult GetServerInfo();
    MccMcpResult GetPlayerState();
    MccMcpResult GetPlayersList();
    MccMcpResult GetChatHistory(int maxCount, bool includeJson);
    MccMcpResult GetInternalCommands();
    MccMcpResult GetMaterialsList(string? filter, int maxCount);
    MccMcpResult GetBlockTypesList(string? filter, int maxCount);
    MccMcpResult GetEntityTypesList(string? filter, int maxCount);
    MccMcpResult SendChat(string text);
    MccMcpResult QuitClient();
    MccMcpResult RunInternalCommand(string command);
    MccMcpResult UseItemOnHand();
    MccMcpResult ChangeHotbarSlot(int slot);
    MccMcpResult UseItemOnBlock(double x, double y, double z);
    MccMcpResult DigBlock(double x, double y, double z, double durationSeconds);
    MccMcpResult PlaceBlock(int x, int y, int z, string face, string hand, bool lookAtBlock);
    MccMcpResult InteractEntity(int entityId, string interaction, string hand);
    MccMcpResult ScanNearbyBlocks(int radius, int maxCount, string? materialFilter);
    MccMcpResult FindBlocks(string? query, int radius, int maxCount, bool exactMatch);
    MccMcpResult IsPlayerNearby(string? playerName, double radius, bool includeSelf);
    MccMcpResult LocatePlayer(string playerName, bool includeSelf);
    MccMcpResult CanReachPosition(double x, double y, double z, bool allowUnsafe, int maxOffset, int minOffset, int timeoutMs);
    MccMcpResult MoveTo(double x, double y, double z, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs);
    MccMcpResult MoveToPlayer(string playerName, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs);
    MccMcpResult LookAt(double x, double y, double z);
    MccMcpResult ListInventories();
    MccMcpResult GetInventorySnapshot(int inventoryId);
    MccMcpResult OpenContainerAt(int x, int y, int z, int timeoutMs, bool closeCurrent);
    MccMcpResult CloseContainer(int inventoryId, int timeoutMs);
    MccMcpResult InventoryWindowAction(int inventoryId, int slotId, string actionType);
    MccMcpResult DropInventoryItem(string itemType, int count, int inventoryId, bool preferStack);
    MccMcpResult DepositContainerItem(string itemType, int count, int inventoryId, bool preferLargestStack);
    MccMcpResult WithdrawContainerItem(string itemType, int count, int inventoryId, bool preferLargestStack);
    MccMcpResult QueryEntities(int maxCount);
    MccMcpResult ListEntities(int maxCount, string? typeFilter, double radius);
    MccMcpResult GetEntityInfo(int entityId, bool includeMetadata, bool includeEquipment, bool includeEffects);
    MccMcpResult FindSigns(string text, bool exactMatch, int radius, int maxCount, bool includeBackText);
    MccMcpResult ListItemEntities(string? itemType, double radius, int maxCount);
    MccMcpResult PickupItems(string itemType, double radius, int maxItems, bool allowUnsafe, int timeoutMs);
    MccMcpResult GetWorldBlockAt(int x, int y, int z);
}
