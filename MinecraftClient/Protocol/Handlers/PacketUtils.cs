using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Proxy;
using System.Security.Cryptography;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Mapping;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Method to read packet data
    /// </summary>
    public class PacketUtils
    {
        public const int MC18Version = 47;
        public const int MC19Version = 107;
        public const int MC191Version = 108;
        public const int MC110Version = 210;
        public const int MC111Version = 315;
        public const int MC17w13aVersion = 318;
        public const int MC112pre5Version = 332;
        public const int MC17w31aVersion = 336;
        public const int MC17w45aVersion = 343;
        public const int MC17w46aVersion = 345;
        public const int MC17w47aVersion = 346;
        public const int MC18w01aVersion = 352;
        public const int MC18w06aVersion = 357;
        public const int MC113pre4Version = 386;
        public const int MC113pre7Version = 389;
        public const int MC113Version = 393;

        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>
        public static byte[] readData(int offset, List<byte> cache)
        {
            byte[] result = cache.Take(offset).ToArray();
            cache.RemoveRange(0, offset);
            return result;
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        public static string readNextString(List<byte> cache)
        {
            int length = readNextVarInt(cache);
            if (length > 0)
            {
                return Encoding.UTF8.GetString(readData(length, cache));
            }
            else return "";
        }

        /// <summary>
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>
        public static bool readNextBool(List<byte> cache)
        {
            return readNextByte(cache) != 0x00;
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>
        public static short readNextShort(List<byte> cache)
        {
            byte[] rawValue = readData(2, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt16(rawValue, 0);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The integer value</returns>
        public static int readNextInt(List<byte> cache)
        {
            byte[] rawValue = readData(4, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt32(rawValue, 0);
        }

        /// <summary>
        /// Read an unsigned short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public static ushort readNextUShort(List<byte> cache)
        {
            byte[] rawValue = readData(2, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToUInt16(rawValue, 0);
        }

        /// <summary>
        /// Read an unsigned long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public static ulong readNextULong(List<byte> cache)
        {
            byte[] rawValue = readData(8, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToUInt64(rawValue, 0);
        }

        /// <summary>
        /// Read several little endian unsigned short integers at once from a cache of bytes and remove them from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public static ushort[] readNextUShortsLittleEndian(int amount, List<byte> cache)
        {
            byte[] rawValues = readData(2 * amount, cache);
            ushort[] result = new ushort[amount];
            for (int i = 0; i < amount; i++)
                result[i] = BitConverter.ToUInt16(rawValues, i * 2);
            return result;
        }

        /// <summary>
        /// Read a uuid from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The uuid</returns>
        public static Guid readNextUUID(List<byte> cache)
        {
            byte[] javaUUID = readData(16, cache);
            Guid guid;
            if (BitConverter.IsLittleEndian)
            {
                guid = ToLittleEndian(javaUUID);
            }
            else
            {
                guid = new Guid(javaUUID);
            }
            return guid;
        }

        /// <summary>
        /// convert a Java big-endian Guid to a .NET little-endian Guid.
        /// </summary>
        /// <returns>GUID in little endian.</returns>
        public static Guid ToLittleEndian(byte[] java)
        {
            byte[] net = new byte[16];
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
        /// Read a byte array from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The byte array</returns>
        public static byte[] readNextByteArray(int protocolversion, List<byte> cache)
        {
            int len = protocolversion >= MC18Version
                ? readNextVarInt(cache)
                : readNextShort(cache);
            return readData(len, cache);
        }

        /// <summary>
        /// Reads a length-prefixed array of unsigned long integers and removes it from the cache
        /// </summary>
        /// <returns>The unsigned long integer values</returns>
        public static ulong[] readNextULongArray(List<byte> cache)
        {
            int len = readNextVarInt(cache);
            ulong[] result = new ulong[len];
            for (int i = 0; i < len; i++)
                result[i] = readNextULong(cache);
            return result;
        }

        /// <summary>
        /// Read a double from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The double value</returns>
        public static double readNextDouble(List<byte> cache)
        {
            byte[] rawValue = readData(8, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToDouble(rawValue, 0);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public static int readNextVarInt(List<byte> cache)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            while (true)
            {
                k = readNextByte(cache);
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }

        /// <summary>
        /// Read an "extended short", which is actually an int of some kind, from the cache of bytes.
        /// This is only done with forge.  It looks like it's a normal short, except that if the high
        /// bit is set, it has an extra byte.
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The int</returns>
        public static int readNextVarShort(List<byte> cache)
        {
            ushort low = readNextUShort(cache);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = readNextByte(cache);
            }
            return ((high & 0xFF) << 15) | low;
        }

        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        public static byte readNextByte(List<byte> cache)
        {
            byte result = cache[0];
            cache.RemoveAt(0);
            return result;
        }

        /// <summary>
        /// Read a float from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The float value</returns>
        public static float readNextFloat(List<byte> cache)
        {
            byte[] rawValue = PacketUtils.readData(4, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToSingle(rawValue, 0);
        }

        /// <summary>
        /// Build an integer for sending over the network
        /// </summary>
        /// <param name="paramInt">Integer to encode</param>
        /// <returns>Byte array for this integer</returns>
        public static byte[] getVarInt(int paramInt)
        {
            List<byte> bytes = new List<byte>();
            while ((paramInt & -128) != 0)
            {
                bytes.Add((byte)(paramInt & 127 | 128));
                paramInt = (int)(((uint)paramInt) >> 7);
            }
            bytes.Add((byte)paramInt);
            return bytes.ToArray();
        }

        /// <summary>
        /// Get byte array representing a double
        /// </summary>
        /// <param name="number">Double to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] getDouble(double number)
        {
            byte[] theDouble = BitConverter.GetBytes(number);
            Array.Reverse(theDouble); //Endianness
            return theDouble;
        }

        /// <summary>
        /// Get byte array with length information prepended to it
        /// </summary>
        /// <param name="array">Array to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] getArray(int protocolversion, byte[] array)
        {
            if (protocolversion < MC18Version)
            {
                byte[] length = BitConverter.GetBytes((short)array.Length);
                Array.Reverse(length);
                return concatBytes(length, array);
            }
            else return concatBytes(getVarInt(array.Length), array);
        }

        /// <summary>
        /// Get a byte array from the given string for sending over the network, with length information prepended.
        /// </summary>
        /// <param name="text">String to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] getString(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            return concatBytes(getVarInt(bytes.Length), bytes);
        }

        /// <summary>
        /// Get byte array representing a float
        /// </summary>
        /// <param name="number">Floalt to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] getFloat(float number)
        {
            byte[] theFloat = BitConverter.GetBytes(number);
            Array.Reverse(theFloat); //Endianness
            return theFloat;
        }

        /// <summary>
        /// Easily append several byte arrays
        /// </summary>
        /// <param name="bytes">Bytes to append</param>
        /// <returns>Array containing all the data</returns>
        public static byte[] concatBytes(params byte[][] bytes)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }
    }
}
