using System.IO;
using System.IO.Compression;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Quick Zlib compression handling for network packet compression.
    /// </summary>
    public static class ZlibUtils
    {
        /// <summary>
        /// Compress a byte array into another bytes array using Zlib compression
        /// </summary>
        /// <param name="to_compress">Data to compress</param>
        /// <returns>Compressed data as a byte array</returns>
        public static byte[] Compress(byte[] to_compress)
        {
            using MemoryStream memstream = new();
            using (ZLibStream stream = new(memstream, CompressionMode.Compress, leaveOpen: true))
            {
                stream.Write(to_compress, 0, to_compress.Length);
            }

            return memstream.ToArray();
        }

        /// <summary>
        /// Decompress a byte array into another byte array of the specified size
        /// </summary>
        /// <param name="to_decompress">Data to decompress</param>
        /// <param name="size_uncompressed">Size of the data once decompressed</param>
        /// <returns>Decompressed data as a byte array</returns>
        public static byte[] Decompress(byte[] to_decompress, int size_uncompressed)
        {
            using MemoryStream compressedStream = new(to_decompress, writable: false);
            using ZLibStream stream = new(compressedStream, CompressionMode.Decompress);

            byte[] packetData_decompressed = new byte[size_uncompressed];
            int totalRead = 0;
            while (totalRead < size_uncompressed)
            {
                int read = stream.Read(packetData_decompressed, totalRead, size_uncompressed - totalRead);
                if (read <= 0)
                    break;

                totalRead += read;
            }

            return packetData_decompressed;
        }

        /// <summary>
        /// Decompress a byte array into another byte array of a potentially unlimited size (!)
        /// </summary>
        /// <param name="to_decompress">Data to decompress</param>
        /// <returns>Decompressed data as byte array</returns>
        public static byte[] Decompress(byte[] to_decompress)
        {
            using MemoryStream compressedStream = new(to_decompress, writable: false);
            using ZLibStream stream = new(compressedStream, CompressionMode.Decompress);
            byte[] buffer = new byte[16 * 1024];
            using MemoryStream decompressedBuffer = new();
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                decompressedBuffer.Write(buffer, 0, read);

            return decompressedBuffer.ToArray();
        }
    }
}
