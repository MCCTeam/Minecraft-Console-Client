using System;

namespace MinecraftClient.Protocol
{
    public static class GuidExtensions
    {
        /// <summary>
        /// A CLSCompliant method to convert a Java big-endian Guid to a .NET 
        /// little-endian Guid.
        /// The Guid Constructor (UInt32, UInt16, UInt16, Byte, Byte, Byte, Byte,
        ///  Byte, Byte, Byte, Byte) is not CLSCompliant.
        /// </summary>
        public static Guid ToLittleEndian(this Guid javaGuid)
        {
            byte[] net = new byte[16];
            byte[] java = javaGuid.ToByteArray();
            for (int i = 8; i < 16; i++)
            {
                net[i] = java[i];
            }
            net[3] = java[0];
            net[2] = java[1];
            net[1] = java[2];
            net[0] = java[3];
            net[5] = java[4];
            net[4] = java[5];
            net[6] = java[7];
            net[7] = java[6];
            return new Guid(net);
        }

        /// <summary>
        /// Converts little-endian .NET guids to big-endian Java guids:
        /// </summary>
        public static Guid ToBigEndian(this Guid netGuid)
        {
            byte[] java = new byte[16];
            byte[] net = netGuid.ToByteArray();
            for (int i = 8; i < 16; i++)
            {
                java[i] = net[i];
            }
            java[0] = net[3];
            java[1] = net[2];
            java[2] = net[1];
            java[3] = net[0];
            java[4] = net[5];
            java[5] = net[4];
            java[6] = net[7];
            java[7] = net[6];
            return new Guid(java);
        }

        /// <summary>
        /// Converts little-endian .NET guids to big-endian Java guids:
        /// </summary>
        public static byte[] ToBigEndianBytes(this Guid netGuid)
        {
            byte[] java = new byte[16];
            byte[] net = netGuid.ToByteArray();
            for (int i = 8; i < 16; i++)
            {
                java[i] = net[i];
            }
            java[0] = net[3];
            java[1] = net[2];
            java[2] = net[1];
            java[3] = net[0];
            java[4] = net[5];
            java[5] = net[4];
            java[6] = net[7];
            java[7] = net[6];
            return java;
        }
    }
}
