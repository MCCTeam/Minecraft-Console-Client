namespace MinecraftClient.Network.Packets
{
    public static class Response
    {
        public delegate IPacket CreatePacketInstance();

        #region Server Login Response
        public static readonly CreatePacketInstance[] ServerLoginResponse =
        {
            () => new Disconnect(), // 0x00
            () => new EncryptionRequest(), // 0x01
            () => new LoginSuccess(), // 0x02
        };
        #endregion

        #region Server Status Response
        public static readonly CreatePacketInstance[] ServerStatusResponse =
        {
            //() => new Response(), // 0x00
            //() => new Ping(), // 0x01
        };
        #endregion

        #region Server Play Response
        public static readonly CreatePacketInstance[] ServerPlayResponse =
        {
            () => new KeepAlive(), // 0x00
            () => new LoginStart(), // 0x01
            () => new LoginSuccess(), // 0x02
            () => new Handshake(), // 0x03
            () => new ChatMessage(), // 0x04
            null, // 0x05
            null, // 0x06
            null, // 0x07
            null, // 0x08
            null, // 0x09
            null, // 0x0A
            null, // 0x0B
            null, // 0x0C
            null, // 0x0D
            null, // 0x0E
            null, // 0x0F
            null, // 0x10
            null, // 0x11
            null, // 0x12
            null, // 0x13
            null, // 0x14
            null, // 0x15
            null, // 0x16
            null, // 0x17
            null, // 0x18
            null, // 0x19
            null, // 0x1A
            null, // 0x1B
            null, // 0x1C
            null, // 0x1D
            null, // 0x1E
            null, // 0x1F
            null, // 0x20
            null, // 0x21
            null, // 0x22
            null, // 0x23
            null, // 0x24
            null, // 0x25
            null, // 0x26
            null, // 0x27
            null, // 0x28
            null, // 0x29
            () => new Particle(), // 0x2A
            null, // 0x2B
            null, // 0x2C
            null, // 0x2D
            null, // 0x2E
            null, // 0x2F
            null, // 0x30
            null, // 0x31
            null, // 0x32
            null, // 0x33
            null, // 0x34
            null, // 0x35
            null, // 0x36
            null, // 0x37
            null, // 0x38
            null, // 0x39
            null, // 0x3A
            null, // 0x3B
            null, // 0x3C
            () => new DisplayScoreboard(), // 0x3D
            null, // 0x3E
            null, // 0x3F
            null, // 0x40
            null, // 0x41
            null, // 0x42
            null, // 0x43
            null, // 0x44
            null, // 0x45
            null, // 0x46
            null, // 0x47
            null, // 0x48
            null, // 0x49
            null, // 0x4A
            null, // 0x4B
            null, // 0x4C
            null, // 0x4D
            null, // 0x4E
            null, // 0x4F
            null, // 0x50
            null, // 0x51
            null, // 0x52
            null, // 0x53
            null, // 0x54
            null, // 0x55
            null, // 0x56
            null, // 0x57
            null, // 0x58
            null, // 0x59
            null, // 0x5A
            null, // 0x5B
            null, // 0x5C
            null, // 0x5D
            null, // 0x5E
            null, // 0x5F
            null, // 0x60
            null, // 0x61
            null, // 0x62
            null, // 0x63
            null, // 0x64
            null, // 0x65
            null, // 0x66
            null, // 0x67
            null, // 0x68
            null, // 0x69
            null, // 0x6A
            null, // 0x6B
            null, // 0x6C
            null, // 0x6D
            null, // 0x6E
            null, // 0x6F
            null, // 0x70
            null, // 0x71
            null, // 0x72
            null, // 0x73
            null, // 0x74
            null, // 0x75
            null, // 0x76
            null, // 0x77
            null, // 0x78
            null, // 0x79
            null, // 0x7A
            null, // 0x7B
            null, // 0x7C
            null, // 0x7D
            null, // 0x7E
            null, // 0x7F
            null, // 0x80
            null, // 0x81
            null, // 0x82
            null, // 0x83
            null, // 0x84
            null, // 0x85
            null, // 0x86
            null, // 0x87
            null, // 0x88
            null, // 0x89
            null, // 0x8A
            null, // 0x8B
            null, // 0x8C
            null, // 0x8D
            null, // 0x8E
            null, // 0x8F
            null, // 0x90
            null, // 0x91
            null, // 0x92
            null, // 0x93
            null, // 0x94
            null, // 0x95
            null, // 0x96
            null, // 0x97
            null, // 0x98
            null, // 0x99
            null, // 0x9A
            null, // 0x9B
            null, // 0x9C
            null, // 0x9D
            null, // 0x9E
            null, // 0x9F
            null, // 0xA0
            null, // 0xA1
            null, // 0xA2
            null, // 0xA3
            null, // 0xA4
            null, // 0xA5
            null, // 0xA6
            null, // 0xA7
            null, // 0xA8
            null, // 0xA9
            null, // 0xAA
            null, // 0xAB
            null, // 0xAC
            null, // 0xAD
            null, // 0xAE
            null, // 0xAF
            null, // 0xB0
            null, // 0xB1
            null, // 0xB2
            null, // 0xB3
            null, // 0xB4
            null, // 0xB5
            null, // 0xB6
            null, // 0xB7
            null, // 0xB8
            null, // 0xB9
            null, // 0xBA
            null, // 0xBB
            null, // 0xBC
            null, // 0xBD
            null, // 0xBE
            null, // 0xBF
            null, // 0xC0
            null, // 0xC1
            null, // 0xC2
            null, // 0xC3
            null, // 0xC4
            null, // 0xC5
            null, // 0xC6
            null, // 0xC7
            null, // 0xC8
            null, // 0xC9
            null, // 0xCA
            null, // 0xCB
            null, // 0xCC
            null, // 0xCD
            null, // 0xCE
            null, // 0xCF
            null, // 0xD0
            null, // 0xD1
            null, // 0xD2
            null, // 0xD3
            null, // 0xD4
            null, // 0xD5
            null, // 0xD6
            null, // 0xD7
            null, // 0xD8
            null, // 0xD9
            null, // 0xDA
            null, // 0xDB
            null, // 0xDC
            null, // 0xDD
            null, // 0xDE
            null, // 0xDF
            null, // 0xE0
            null, // 0xE1
            null, // 0xE2
            null, // 0xE3
            null, // 0xE4
            null, // 0xE5
            null, // 0xE6
            null, // 0xE7
            null, // 0xE8
            null, // 0xE9
            null, // 0xEA
            null, // 0xEB
            null, // 0xEC
            null, // 0xED
            null, // 0xEE
            null, // 0xEF
            null, // 0xF0
            null, // 0xF1
            null, // 0xF2
            null, // 0xF3
            null, // 0xF4
            null, // 0xF5
            null, // 0xF6
            null, // 0xF7
            null, // 0xF8
            null, // 0xF9
            null, // 0xFA
            null, // 0xFB
            null, // 0xFC
            null, // 0xFD
            null, // 0xFE
            null, // 0xFF
        };
        #endregion
    }
}
