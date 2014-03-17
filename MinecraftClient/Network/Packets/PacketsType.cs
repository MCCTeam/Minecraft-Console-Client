namespace MinecraftClient.Network.Packets
{
    public enum Packet : byte
    {
        Handshake                   = 0x00,
        EncryptionRequest           = 0x01,
        LoginSuccess                = 0x02,
        JoinGame                    = 0x01,
        Particle                    = 0x2A,
        DisplayScoreboard           = 0x3D,

        LoginStart                  = 0x00,
        EncryptionResponse          = 0x01,

        KeepAlive                   = 0x00,
        ChatMessage                 = 0x01,
        UseEntity                   = 0x02,
        Player                      = 0x03,
        PlayerPosition              = 0x04,
        PlayerLook                  = 0x05,
        PlayerPositionAndLook       = 0x06,
        PlayerDigging               = 0x07,
        PlayerBlockPlacement        = 0x08,
        HeldItemChange              = 0x09,
        Animation                   = 0x0A,
        EntityAction                = 0x0B,
        SteerVehicle                = 0x0C,
        CloseWindow                 = 0x0D,
        ClickWindow                 = 0x0E,
        ConfirmTransaction          = 0x0F,
        CreativeInventoryAction     = 0x10,
        EnchantItem                 = 0x11,
        UpdateSign                  = 0x12,
        PlayerAbilities             = 0x13,
        TabComplete                 = 0x14,
        ClientSettings              = 0x15,
        ClientStatus                = 0x16,
        PluginMessage               = 0x17,
        Disconnect                  = 0x40
    }
}

