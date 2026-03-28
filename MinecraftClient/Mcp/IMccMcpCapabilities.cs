namespace MinecraftClient.Mcp;

public interface IMccMcpCapabilities
{
    MccMcpResult GetSessionStatus();
    MccMcpResult GetServerInfo();
    MccMcpResult GetPlayerState();
    MccMcpResult GetPlayersList();
    MccMcpResult GetChatHistory(int maxCount, bool includeJson);
    MccMcpResult GetInternalCommands();
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
    MccMcpResult MoveTo(double x, double y, double z, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs);
    MccMcpResult MoveToPlayer(string playerName, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs);
    MccMcpResult LookAt(double x, double y, double z);
    MccMcpResult GetInventorySnapshot(int inventoryId);
    MccMcpResult InventoryWindowAction(int inventoryId, int slotId, string actionType);
    MccMcpResult DropInventoryItem(string itemType, int count, int inventoryId, bool preferStack);
    MccMcpResult QueryEntities(int maxCount);
    MccMcpResult ListEntities(int maxCount, string? typeFilter, double radius);
    MccMcpResult GetEntityInfo(int entityId, bool includeMetadata, bool includeEquipment, bool includeEffects);
    MccMcpResult GetWorldBlockAt(int x, int y, int z);
}
