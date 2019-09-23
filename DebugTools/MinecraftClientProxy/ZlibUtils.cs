using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zlib;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Quick Zlib compression handling for network packet compression.
    /// Note: Underlying compression handling is taken from the DotNetZip Library.
    /// This library is open source and provided under the Microsoft Public License.
    /// More info about DotNetZip at dotnetzip.codeplex.com.
    /// </summary>

    public static class ZlibUtils
    {
        /// <summary>
        /// Decompress a byte array into another byte array of the specified size
        /// </summary>
        /// <param name="to_decompress">Data to decompress</param>
        /// <param name="size_uncompressed">Size of the data once decompressed</param>
        /// <returns>Decompressed data as a byte array</returns>

        public static byte[] Decompress(byte[] to_decompress, int size_uncompressed)
        {
            ZlibStream stream = new ZlibStream(new System.IO.MemoryStream(to_decompress, false), CompressionMode.Decompress);
            byte[] packetData_decompressed = new byte[size_uncompressed];
            stream.Read(packetData_decompressed, 0, size_uncompressed);
            stream.Close();
            return packetData_decompressed;
        }
    }
}
