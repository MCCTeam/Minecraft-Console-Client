using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Crypto
{
    /// <summary>
    /// Interface for AES stream
    /// Allows to use a different implementation depending on the framework being used.
    /// </summary>

    public interface IAesStream
    {
        int Read(byte[] buffer, int offset, int count);
        void Write(byte[] buffer, int offset, int count);
    }
}
