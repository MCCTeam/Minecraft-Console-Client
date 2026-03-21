namespace MinecraftClient.Protocol.Handlers;

public enum ConfigurationPacketTypesIn
{
    CookieRequest,
    CustomReportDetails,
    Disconnect,
    FeatureFlags,
    FinishConfiguration,
    KeepAlive,
    KnownDataPacks,
    Ping,
    PluginMessage,
    RegistryData,
    RemoveResourcePack,
    ResetChat,
    ResourcePack,
    ServerLinks,
    StoreCookie,
    Transfer,
    UpdateTags,
    ClearDialog,        // Added in 1.21.6
    ShowDialog,         // Added in 1.21.6
    
    Unknown
}
