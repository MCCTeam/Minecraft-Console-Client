using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Crypto
{
    public abstract class IAesHandler
    {
        public abstract void EncryptEcb(Span<byte> plaintext, Span<byte> destination);
    }
}
