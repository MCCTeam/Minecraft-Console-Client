namespace MinecraftClient.Protocol.Handlers;

public enum ConfigurationPacketTypesOut
{
    ClientInformation,
    PluginMessage,
    FinishConfiguration,
    KeepAlive,
    Pong,
    ResourcePackResponse,
    
    Unknown
}