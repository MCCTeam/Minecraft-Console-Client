namespace MinecraftClient.Protocol.Handlers;

public enum ConfigurationPacketTypesOut
{
    ClientInformation,
    PluginMessage,
    FinishConfiguration,
    KeepAlive,
    Pong,
    ResourcePackResponse,
    CookieResponse,
    KnownDataPacks,
    CustomClickAction,  // Added in 1.21.6
    AcceptCodeOfConduct, // Added in 1.21.9
    
    Unknown
}