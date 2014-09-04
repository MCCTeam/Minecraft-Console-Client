using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Crypto
{
    /// <summary>
    /// Interface for padding provider
    /// Allow to get a padding plugin message from the current network protocol implementation.
    /// </summary>

    public interface IPaddingProvider
    {
        byte[] getPaddingPacket();
    }
}
