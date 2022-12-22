using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MinecraftClient.EntityHandler;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Protocol.PacketPipeline;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Handle data types encoding / decoding
    /// </summary>
    class DataTypes
    {
        /// <summary>
        /// Protocol version for adjusting data types
        /// </summary>
        private readonly int protocolversion;

        private const int BufferLength = 32;
        private static readonly Memory<byte> Buffer = new byte[BufferLength];

        /// <summary>
        /// Initialize a new DataTypes instance
        /// </summary>
        /// <param name="protocol">Protocol version</param>
        public DataTypes(int protocol)
        {
            protocolversion = protocol;
        }

        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="length">Amount of bytes to read</param>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>
        public byte[] ReadData(int length, Queue<byte> cache)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
                result[i] = cache.Dequeue();
            return result;
        }

        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="length">Amount of bytes to read</param>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>
        public async Task<byte[]> ReadDataAsync(int length, PacketStream stream)
        {
            byte[] result = new byte[length];
            await stream.ReadExactlyAsync(result);
            return result;
        }

        /// <summary>
        /// Remove some data from the cache
        /// </summary>
        /// <param name="length">Amount of bytes to drop</param>
        /// <param name="stream">Cache of bytes to drop</param>
        public async Task DropDataAsync(int length, PacketStream stream)
        {
            await stream.Skip(length);
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        public string ReadNextString(Queue<byte> cache)
        {
            int length = ReadNextVarInt(cache);
            if (length > 0)
            {
                return Encoding.UTF8.GetString(ReadData(length, cache));
            }
            else return "";
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        public async Task<string> ReadNextStringAsync(PacketStream stream)
        {
            return await ReadNextUtf8StringAsync(stream, ReadNextVarInt(stream));
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        public async Task<string> ReadNextUtf8StringAsync(PacketStream stream, int length)
        {
            Memory<byte> strByte = length > BufferLength ? new byte[length] : Buffer[..length];
            await stream.ReadExactlyAsync(strByte);
            return Encoding.UTF8.GetString(strByte.Span);
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        public async Task<string> ReadNextAsciiStringAsync(PacketStream stream, int length)
        {
            Memory<byte> strByte = length > BufferLength ? new byte[length] : Buffer[..length];
            await stream.ReadExactlyAsync(strByte);
            return Encoding.ASCII.GetString(strByte.Span);
        }

        /// <summary>
        /// Skip a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        public async Task SkipNextStringAsync(PacketStream stream)
        {
            int length = ReadNextVarInt(stream);
            await DropDataAsync(length, stream);
        }

        /// <summary>
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>
        public bool ReadNextBool(Queue<byte> cache)
        {
            return ReadNextByte(cache) != 0x00;
        }

        /// <summary>
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>
        public bool ReadNextBool(PacketStream stream)
        {
            return ReadNextByte(stream) != 0x00;
        }

        /// <summary>
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>
        public async Task<bool> ReadNextBoolAsync(PacketStream stream)
        {
            return (await ReadNextByteAsync(stream)) != 0x00;
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>
        public short ReadNextShort(PacketStream stream)
        {
            Span<byte> buf = Buffer[..2].Span;
            buf[1] = ReadNextByte(stream);
            buf[0] = ReadNextByte(stream);
            return BinaryPrimitives.ReadInt16LittleEndian(buf);
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>
        public async Task<short> ReadNextShortAsync(PacketStream stream)
        {
            Memory<byte> buf = Buffer[..2];
            await stream.ReadExactlyAsync(buf);
            return BinaryPrimitives.ReadInt16BigEndian(buf.Span);
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>
        public async Task SkipNextShortAsync(PacketStream stream)
        {
            await DropDataAsync(2, stream);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The integer value</returns>
        public int ReadNextInt(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[4];
            for (int i = (4 - 1); i >= 0; --i) // Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToInt32(rawValue);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The integer value</returns>
        public int ReadNextInt(PacketStream stream)
        {
            Span<byte> buf = Buffer[..4].Span;
            buf[3] = ReadNextByte(stream);
            buf[2] = ReadNextByte(stream);
            buf[1] = ReadNextByte(stream);
            buf[0] = ReadNextByte(stream);
            return BinaryPrimitives.ReadInt32LittleEndian(buf);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The integer value</returns>
        public async Task<int> ReadNextIntAsync(PacketStream stream)
        {
            Memory<byte> buf = Buffer[..4];
            await stream.ReadExactlyAsync(buf);
            return BinaryPrimitives.ReadInt32BigEndian(buf.Span);
        }

        /// <summary>
        /// Read a long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public long ReadNextLong(PacketStream stream)
        {
            Span<byte> buf = Buffer[..8].Span;
            buf[7] = ReadNextByte(stream);
            buf[6] = ReadNextByte(stream);
            buf[5] = ReadNextByte(stream);
            buf[4] = ReadNextByte(stream);
            buf[3] = ReadNextByte(stream);
            buf[2] = ReadNextByte(stream);
            buf[1] = ReadNextByte(stream);
            buf[0] = ReadNextByte(stream);
            return BinaryPrimitives.ReadInt64LittleEndian(buf);
        }

        /// <summary>
        /// Read a long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public async Task<long> ReadNextLongAsync(PacketStream stream)
        {
            Memory<byte> buf = Buffer[..8];
            await stream.ReadExactlyAsync(buf);
            return BinaryPrimitives.ReadInt64BigEndian(buf.Span);
        }

        /// <summary>
        /// Read a long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public async Task SkipNextLongAsync(PacketStream stream)
        {
            await DropDataAsync(8, stream);
        }

        /// <summary>
        /// Read an unsigned short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public ushort ReadNextUShort(PacketStream stream)
        {
            Span<byte> buf = Buffer[..2].Span;
            buf[1] = ReadNextByte(stream);
            buf[0] = ReadNextByte(stream);
            return BinaryPrimitives.ReadUInt16LittleEndian(buf);
        }

        /// <summary>
        /// Read an unsigned short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public async Task<ushort> ReadNextUShortAsync(PacketStream stream)
        {
            Memory<byte> buf = Buffer[..2];
            await stream.ReadExactlyAsync(buf);
            return BinaryPrimitives.ReadUInt16BigEndian(buf.Span);
        }

        /// <summary>
        /// Read an unsigned long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public ulong ReadNextULong(PacketStream stream)
        {
            Span<byte> buf = Buffer[..8].Span;
            buf[7] = ReadNextByte(stream);
            buf[6] = ReadNextByte(stream);
            buf[5] = ReadNextByte(stream);
            buf[4] = ReadNextByte(stream);
            buf[3] = ReadNextByte(stream);
            buf[2] = ReadNextByte(stream);
            buf[1] = ReadNextByte(stream);
            buf[0] = ReadNextByte(stream);
            return BinaryPrimitives.ReadUInt64LittleEndian(buf);
        }

        /// <summary>
        /// Read a long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public async Task<ulong> ReadNextULongAsync(PacketStream stream)
        {
            Memory<byte> buf = Buffer[..8];
            await stream.ReadExactlyAsync(buf);
            return BinaryPrimitives.ReadUInt64BigEndian(buf.Span);
        }

        /// <summary>
        /// Read a Location encoded as an ulong field and remove it from the cache
        /// </summary>
        /// <returns>The Location value</returns>
        public Location ReadNextLocation(PacketStream stream)
        {
            ulong locEncoded = ReadNextULong(stream);
            int x, y, z;
            if (protocolversion >= Protocol18Handler.MC_1_14_Version)
            {
                x = (int)(locEncoded >> 38);
                y = (int)(locEncoded & 0xFFF);
                z = (int)(locEncoded << 26 >> 38);
            }
            else
            {
                x = (int)(locEncoded >> 38);
                y = (int)((locEncoded >> 26) & 0xFFF);
                z = (int)(locEncoded << 38 >> 38);
            }
            if (x >= 0x02000000) // 33,554,432
                x -= 0x04000000; // 67,108,864
            if (y >= 0x00000800) //      2,048
                y -= 0x00001000; //      4,096
            if (z >= 0x02000000) // 33,554,432
                z -= 0x04000000; // 67,108,864
            return new Location(x, y, z);
        }

        /// <summary>
        /// Read a Location encoded as an ulong field and remove it from the cache
        /// </summary>
        /// <returns>The Location value</returns>
        public async Task<Location> ReadNextLocationAsync(PacketStream stream)
        {
            ulong locEncoded = await ReadNextULongAsync(stream);
            int x, y, z;
            if (protocolversion >= Protocol18Handler.MC_1_14_Version)
            {
                x = (int)(locEncoded >> 38);
                y = (int)(locEncoded & 0xFFF);
                z = (int)(locEncoded << 26 >> 38);
            }
            else
            {
                x = (int)(locEncoded >> 38);
                y = (int)((locEncoded >> 26) & 0xFFF);
                z = (int)(locEncoded << 38 >> 38);
            }
            if (x >= 0x02000000) // 33,554,432
                x -= 0x04000000; // 67,108,864
            if (y >= 0x00000800) //      2,048
                y -= 0x00001000; //      4,096
            if (z >= 0x02000000) // 33,554,432
                z -= 0x04000000; // 67,108,864
            return new Location(x, y, z);
        }

        /// <summary>
        /// Read a Location encoded as an ulong field and remove it from the cache
        /// </summary>
        /// <returns>The Location value</returns>
        public async Task SkipNextLocationAsync(PacketStream stream)
        {
            await SkipNextLongAsync(stream);
        }

        /// <summary>
        /// Read a uuid from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The uuid</returns>
        public Guid ReadNextUUID(Queue<byte> cache)
        {
            Span<byte> javaUUID = stackalloc byte[16];
            for (int i = 0; i < 16; ++i)
                javaUUID[i] = cache.Dequeue();
            Guid guid = new(javaUUID);
            if (BitConverter.IsLittleEndian)
                guid = guid.ToLittleEndian();
            return guid;
        }

        /// <summary>
        /// Read a uuid from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The uuid</returns>
        public async Task<Guid> ReadNextUUIDAsync(PacketStream stream)
        {
            Memory<byte> buf = Buffer[..16];
            await stream.ReadExactlyAsync(buf);
            return new Guid(buf.Span).ToLittleEndian();
        }

        /// <summary>
        /// Read a uuid from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The uuid</returns>
        public async Task SkipNextUUIDAsync(PacketStream stream)
        {
            await DropDataAsync(16, stream);
        }

        /// <summary>
        /// Read a byte array from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The byte array</returns>
        public async Task<byte[]> ReadNextByteArrayAsync(PacketStream stream)
        {
            int len = protocolversion >= Protocol18Handler.MC_1_8_Version
                ? await ReadNextVarIntAsync(stream)
                : await ReadNextShortAsync(stream);
            return await ReadDataAsync(len, stream);
        }

        /// <summary>
        /// Reads a length-prefixed array of unsigned long integers and removes it from the cache
        /// </summary>
        /// <returns>The unsigned long integer values</returns>
        public ulong[] ReadNextULongArray(PacketStream stream)
        {
            int len = ReadNextVarInt(stream);
            ulong[] result = new ulong[len];
            for (int i = 0; i < len; i++)
                result[i] = ReadNextULong(stream);
            return result;
        }

        /// <summary>
        /// Reads a length-prefixed array of unsigned long integers and removes it from the cache
        /// </summary>
        /// <returns>The unsigned long integer values</returns>
        public async Task<ulong[]> ReadNextULongArrayAsync(PacketStream stream)
        {
            int len = await ReadNextVarIntAsync(stream);
            ulong[] result = new ulong[len];
            for (int i = 0; i < len; i++)
                result[i] = await ReadNextULongAsync(stream);
            return result;
        }

        /// <summary>
        /// Reads a length-prefixed array of unsigned long integers and removes it from the cache
        /// </summary>
        /// <returns>The unsigned long integer values</returns>
        public async Task SkipNextULongArray(PacketStream stream)
        {
            int len = await ReadNextVarIntAsync(stream);
            await DropDataAsync(len * 8, stream);
        }

        /// <summary>
        /// Read a double from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The double value</returns>
        public double ReadNextDouble(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[8];
            for (int i = (8 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToDouble(rawValue);
        }

        /// <summary>
        /// Read a double from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The double value</returns>
        public double ReadNextDouble(PacketStream stream)
        {
            Span<byte> buf = Buffer[..8].Span;
            buf[7] = ReadNextByte(stream);
            buf[6] = ReadNextByte(stream);
            buf[5] = ReadNextByte(stream);
            buf[4] = ReadNextByte(stream);
            buf[3] = ReadNextByte(stream);
            buf[2] = ReadNextByte(stream);
            buf[1] = ReadNextByte(stream);
            buf[0] = ReadNextByte(stream);
            return BinaryPrimitives.ReadDoubleLittleEndian(buf);
        }

        /// <summary>
        /// Read a double from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The double value</returns>
        public async Task<double> ReadNextDoubleAsync(PacketStream stream)
        {
            Memory<byte> buf = Buffer[..8];
            await stream.ReadExactlyAsync(buf);
            return BinaryPrimitives.ReadDoubleBigEndian(buf.Span);
        }

        /// <summary>
        /// Read a float from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The float value</returns>
        public float ReadNextFloat(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[4];
            for (int i = (4 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToSingle(rawValue);
        }

        /// <summary>
        /// Read a float from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The float value</returns>
        public float ReadNextFloat(PacketStream stream)
        {
            Span<byte> buf = Buffer[..4].Span;
            buf[3] = ReadNextByte(stream);
            buf[2] = ReadNextByte(stream);
            buf[1] = ReadNextByte(stream);
            buf[0] = ReadNextByte(stream);
            return BinaryPrimitives.ReadSingleLittleEndian(buf);
        }

        /// <summary>
        /// Read a float from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The float value</returns>
        public async Task<float> ReadNextFloatAsync(PacketStream stream)
        {
            Memory<byte> buf = Buffer[..4];
            await stream.ReadExactlyAsync(buf);
            return BinaryPrimitives.ReadSingleBigEndian(buf.Span);
        }

        /// <summary>
        /// Read a float from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The float value</returns>
        public async Task SkipNextFloatAsync(PacketStream stream)
        {
            await DropDataAsync(4, stream);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public int ReadNextVarInt(PacketStream stream)
        {
            int i = 0;
            int j = 0;
            byte b;
            do
            {
                b = ReadNextByte(stream);
                i |= (b & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
            } while ((b & 0x80) == 128);
            return i;
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public async Task<int> ReadNextVarIntAsync(PacketStream stream)
        {
            int i = 0;
            int j = 0;
            byte b;
            do
            {
                b = await ReadNextByteAsync(stream);
                i |= (b & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
            } while ((b & 0x80) == 128);
            return i;
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public int ReadNextVarInt(Queue<byte> cache)
        {
            int i = 0;
            int j = 0;
            byte b;
            do
            {
                b = cache.Dequeue();
                i |= (b & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
            } while ((b & 0x80) == 128);
            return i;
        }

        /// <summary>
        /// Skip a VarInt from a cache of bytes with better performance
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        public void SkipNextVarInt(PacketStream stream)
        {
            while (true)
                if ((((byte)ReadNextByte(stream)) & 0x80) != 128)
                    break;
        }

        /// <summary>
        /// Skip a VarInt from a cache of bytes with better performance
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        public async Task SkipNextVarIntAsync(PacketStream stream)
        {
            while (true)
                if (((await ReadNextByteAsync(stream)) & 0x80) != 128)
                    break;
        }

        /// <summary>
        /// Read an "extended short", which is actually an int of some kind, from the cache of bytes.
        /// This is only done with forge.  It looks like it's a normal short, except that if the high
        /// bit is set, it has an extra byte.
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The int</returns>
        public int ReadNextVarShort(PacketStream stream)
        {
            ushort low = ReadNextUShort(stream);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = ReadNextByte(stream);
            }
            return ((high & 0xFF) << 15) | low;
        }

        /// <summary>
        /// Read an "extended short", which is actually an int of some kind, from the cache of bytes.
        /// This is only done with forge.  It looks like it's a normal short, except that if the high
        /// bit is set, it has an extra byte.
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The int</returns>
        public async Task<int> ReadNextVarShortAsync(PacketStream stream)
        {
            ushort low = await ReadNextUShortAsync(stream);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = await ReadNextByteAsync(stream);
            }
            return ((high & 0xFF) << 15) | low;
        }

        /// <summary>
        /// Read a long from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The long value</returns>
        public long ReadNextVarLong(PacketStream stream)
        {
            int numRead = 0;
            long result = 0;
            byte read;
            do
            {
                read = ReadNextByte(stream);
                long value = (read & 0x7F);
                result |= (value << (7 * numRead));

                numRead++;
                if (numRead > 10)
                    throw new OverflowException("VarLong is too big");
            } while ((read & 0x80) != 0);
            return result;
        }

        /// <summary>
        /// Read a long from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="stream">Cache of bytes to read from</param>
        /// <returns>The long value</returns>
        public async Task<long> ReadNextVarLongAsync(PacketStream stream)
        {
            int numRead = 0;
            long result = 0;
            byte read;
            do
            {
                read = await ReadNextByteAsync(stream);
                long value = (read & 0x7F);
                result |= (value << (7 * numRead));

                numRead++;
                if (numRead > 10)
                    throw new OverflowException("VarLong is too big");
            } while ((read & 0x80) != 0);
            return result;
        }

        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        public byte ReadNextByte(Queue<byte> cache)
        {
            byte result = cache.Dequeue();
            return result;
        }

        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        public byte ReadNextByte(PacketStream stream)
        {
            return stream.ReadByte();
        }

        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        public async Task<byte> ReadNextByteAsync(PacketStream stream)
        {
            return await stream.ReadByteAsync();
        }

        /// <summary>
        /// Read an uncompressed Named Binary Tag blob and remove it from the cache
        /// </summary>
        public async Task<Dictionary<string, object>> ReadNextNbtAsync(PacketStream stream)
        {
            return await ReadNextNbtAsync(stream, true);
        }

        /// <summary>
        /// Read a single item slot from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public async Task<Item?> ReadNextItemSlotAsync(PacketStream stream, ItemPalette itemPalette)
        {
            if (protocolversion > Protocol18Handler.MC_1_13_Version)
            {
                // MC 1.13 and greater
                bool itemPresent = await ReadNextBoolAsync(stream);
                if (itemPresent)
                {
                    ItemType type = itemPalette.FromId(await ReadNextVarIntAsync(stream));
                    byte itemCount = await ReadNextByteAsync(stream);
                    Dictionary<string, object> nbt = await ReadNextNbtAsync(stream);
                    return new Item(type, itemCount, nbt);
                }
                else return null;
            }
            else
            {
                // MC 1.12.2 and lower
                short itemID = await ReadNextShortAsync(stream);
                if (itemID == -1)
                    return null;
                byte itemCount = await ReadNextByteAsync(stream);
                short itemDamage = await ReadNextShortAsync(stream);
                Dictionary<string, object> nbt = await ReadNextNbtAsync(stream);
                return new Item(itemPalette.FromId(itemID), itemCount, nbt);
            }
        }

        /// <summary>
        /// Read entity information from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="entityPalette">Mappings for converting entity type Ids to EntityType</param>
        /// <param name="living">TRUE for living entities (layout differs)</param>
        /// <returns>Entity information</returns>
        public async Task<Entity> ReadNextEntity(PacketStream stream, EntityPalette entityPalette, bool living)
        {
            int entityID = await ReadNextVarIntAsync(stream);
            if (protocolversion > Protocol18Handler.MC_1_8_Version)
                await SkipNextUUIDAsync(stream);

            EntityType entityType;
            // Entity type data type change from byte to varint after 1.14
            if (protocolversion > Protocol18Handler.MC_1_13_Version)
                entityType = entityPalette.FromId(await ReadNextVarIntAsync(stream), living);
            else
                entityType = entityPalette.FromId(await ReadNextByteAsync(stream), living);

            double entityX = await ReadNextDoubleAsync(stream);
            double entityY = await ReadNextDoubleAsync(stream);
            double entityZ = await ReadNextDoubleAsync(stream);
            byte entityPitch = await ReadNextByteAsync(stream);
            byte entityYaw = await ReadNextByteAsync(stream);

            int metadata = -1;
            if (living)
            {
                if (protocolversion == Protocol18Handler.MC_1_18_2_Version)
                    entityYaw = await ReadNextByteAsync(stream);
                else
                    entityPitch = await ReadNextByteAsync(stream);
            }
            else
            {
                if (protocolversion >= Protocol18Handler.MC_1_19_Version)
                {
                    entityYaw = await ReadNextByteAsync(stream);
                    metadata = await ReadNextVarIntAsync(stream);
                }
                else
                    metadata = await ReadNextIntAsync(stream);
            }

            short velocityX = await ReadNextShortAsync(stream);
            short velocityY = await ReadNextShortAsync(stream);
            short velocityZ = await ReadNextShortAsync(stream);

            return new Entity(entityID, entityType, new Location(entityX, entityY, entityZ), entityYaw, entityPitch, metadata);
        }

        /// <summary>
        /// Read an uncompressed Named Binary Tag blob and remove it from the cache (internal)
        /// </summary>
        private async Task<Dictionary<string, object>> ReadNextNbtAsync(PacketStream stream, bool root)
        {
            Dictionary<string, object> nbtData = new();

            if (root)
            {
                byte head = await ReadNextByteAsync(stream); // Tag type (TAG_Compound)

                if (head == 0) // TAG_End
                    return nbtData;

                if (head != 10) // TAG_Compound
                    throw new InvalidDataException("Failed to decode NBT: Does not start with TAG_Compound");

                // NBT root name
                string rootName = await ReadNextAsciiStringAsync(stream, await ReadNextUShortAsync(stream));

                if (!string.IsNullOrEmpty(rootName))
                    nbtData[string.Empty] = rootName;
            }

            while (true)
            {
                byte fieldType = await ReadNextByteAsync(stream);

                if (fieldType == 0x00) // TAG_End
                    return nbtData;

                string fieldName = await ReadNextAsciiStringAsync(stream, await ReadNextUShortAsync(stream));
                object fieldValue = await ReadNbtFieldAsync(stream, fieldType);

                // This will override previous tags with the same name
                nbtData[fieldName] = fieldValue;
            }
        }

        /// <summary>
        /// Read a single Named Binary Tag field of the specified type and remove it from the cache
        /// </summary>
        private async Task<object> ReadNbtFieldAsync(PacketStream stream, byte fieldType)
        {
            switch (fieldType)
            {
                case 1: // TAG_Byte
                    return await ReadNextByteAsync(stream);
                case 2: // TAG_Short
                    return await ReadNextShortAsync(stream);
                case 3: // TAG_Int
                    return await ReadNextIntAsync(stream);
                case 4: // TAG_Long
                    return await ReadNextLongAsync(stream);
                case 5: // TAG_Float
                    return await ReadNextFloatAsync(stream);
                case 6: // TAG_Double
                    return await ReadNextDoubleAsync(stream);
                case 7: // TAG_Byte_Array
                    return await ReadDataAsync(await ReadNextIntAsync(stream), stream);
                case 8: // TAG_String
                    return await ReadNextUtf8StringAsync(stream, await ReadNextUShortAsync(stream));
                case 9: // TAG_List
                    byte listType = await ReadNextByteAsync(stream);
                    int listLength = await ReadNextIntAsync(stream);
                    object[] listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = await ReadNbtFieldAsync(stream, listType);
                    return listItems;
                case 10: // TAG_Compound
                    return await ReadNextNbtAsync(stream, false);
                case 11: // TAG_Int_Array
                    listType = 3;
                    listLength = await ReadNextIntAsync(stream);
                    listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = await ReadNbtFieldAsync(stream, listType);
                    return listItems;
                case 12: // TAG_Long_Array
                    listType = 4;
                    listLength = await ReadNextIntAsync(stream);
                    listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = await ReadNbtFieldAsync(stream, listType);
                    return listItems;
                default:
                    throw new InvalidDataException("Failed to decode NBT: Unknown field type " + fieldType);
            }
        }

        public async Task<Dictionary<int, object?>> ReadNextMetadataAsync(PacketStream stream, ItemPalette itemPalette)
        {
            Dictionary<int, object?> data = new();
            byte key = await ReadNextByteAsync(stream);

            while (key != 0xff)
            {
                int type = await ReadNextVarIntAsync(stream);

                // starting from 1.13, Optional Chat is inserted as number 5 in 1.13 and IDs after 5 got shifted.
                // Increase type ID by 1 if
                // - below 1.13
                // - type ID larger than 4
                if (protocolversion < Protocol18Handler.MC_1_13_Version)
                {
                    if (type > 4)
                    {
                        type += 1;
                    }
                }

                // Value's data type is depended on Type
                object? value = null;

                // This is backward compatible since new type is appended to the end
                // Version upgrade note
                // - Check type ID got shifted or not
                // - Add new type if any
                switch (type)
                {
                    case 0: // byte
                        value = await ReadNextByteAsync(stream);
                        break;
                    case 1: // VarInt
                        value = await ReadNextVarIntAsync(stream);
                        break;
                    case 2: // Float
                        value = await ReadNextFloatAsync(stream);
                        break;
                    case 3: // String
                        value = await ReadNextStringAsync(stream);
                        break;
                    case 4: // Chat
                        value = await ReadNextStringAsync(stream);
                        break;
                    case 5: // Optional Chat
                        if (await ReadNextBoolAsync(stream))
                            value = await ReadNextStringAsync(stream);
                        break;
                    case 6: // Slot
                        value = await ReadNextItemSlotAsync(stream, itemPalette);
                        break;
                    case 7: // Boolean
                        value = await ReadNextBoolAsync(stream);
                        break;
                    case 8: // Rotation (3x floats)
                        value = new List<float>
                        {
                            await ReadNextFloatAsync(stream),
                            await ReadNextFloatAsync(stream),
                            await ReadNextFloatAsync(stream)
                        };
                        break;
                    case 9: // Position
                        value = await ReadNextLocationAsync(stream);
                        break;
                    case 10: // Optional Position
                        if (await ReadNextBoolAsync(stream))
                            value = await ReadNextLocationAsync(stream);
                        break;
                    case 11: // Direction (VarInt)
                        value = await ReadNextVarIntAsync(stream);
                        break;
                    case 12: // Optional UUID
                        if (await ReadNextBoolAsync(stream))
                            value = await ReadNextUUIDAsync(stream);
                        break;
                    case 13: // Optional BlockID (VarInt)
                        value = await ReadNextVarIntAsync(stream);
                        break;
                    case 14: // NBT
                        value = await ReadNextNbtAsync(stream);
                        break;
                    case 15: // Particle
                             // Currecutly not handled. Reading data only
                        int ParticleID = await ReadNextVarIntAsync(stream);
                        switch (ParticleID)
                        {
                            case 3:
                                await SkipNextVarIntAsync(stream);
                                break;
                            case 14:
                                await SkipNextFloatAsync(stream);
                                await SkipNextFloatAsync(stream);
                                await SkipNextFloatAsync(stream);
                                await SkipNextFloatAsync(stream);
                                break;
                            case 23:
                                await SkipNextVarIntAsync(stream);
                                break;
                            case 32:
                                await ReadNextItemSlotAsync(stream, itemPalette);
                                break;
                        }
                        break;
                    case 16: // Villager Data (3x VarInt)
                        value = new List<int>
                        {
                            await ReadNextVarIntAsync(stream),
                            await ReadNextVarIntAsync(stream),
                            await ReadNextVarIntAsync(stream)
                        };
                        break;
                    case 17: // Optional VarInt
                        if (await ReadNextBoolAsync(stream))
                        {
                            value = await ReadNextVarIntAsync(stream);
                        }
                        break;
                    case 18: // Pose
                        value = await ReadNextVarIntAsync(stream);
                        break;
                    case 19: // Cat Variant
                        value = await ReadNextVarIntAsync(stream);
                        break;
                    case 20: // Frog Varint
                        value = await ReadNextVarIntAsync(stream);
                        break;
                    case 21: // GlobalPos at 1.19.2+; Painting Variant at 1.19-
                        if (protocolversion <= Protocol18Handler.MC_1_19_Version)
                            value = await ReadNextVarIntAsync(stream);
                        else
                            value = null; // Dimension and blockPos, currently not in use
                        break;
                    case 22: // Painting Variant
                        value = await ReadNextVarIntAsync(stream);
                        break;
                    default:
                        throw new InvalidDataException("Unknown Metadata Type ID " + type + ". Is this up to date for new MC Version?");
                }
                data[key] = value;
                key = await ReadNextByteAsync(stream);
            }
            return data;
        }

        /// <summary>
        /// Read a single villager trade from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public async Task<VillagerTrade> ReadNextTradeAsync(PacketStream stream, ItemPalette itemPalette)
        {
            Item inputItem1 = (await ReadNextItemSlotAsync(stream, itemPalette))!;
            Item outputItem = (await ReadNextItemSlotAsync(stream, itemPalette))!;
            Item? inputItem2 = null;
            if (await ReadNextBoolAsync(stream)) //check if villager has second item
                inputItem2 = await ReadNextItemSlotAsync(stream, itemPalette);
            bool tradeDisabled = await ReadNextBoolAsync(stream);
            int numberOfTradeUses = await ReadNextIntAsync(stream);
            int maximumNumberOfTradeUses = await ReadNextIntAsync(stream);
            int xp = await ReadNextIntAsync(stream);
            int specialPrice = await ReadNextIntAsync(stream);
            float priceMultiplier = await ReadNextFloatAsync(stream);
            int demand = await ReadNextIntAsync(stream);
            return new VillagerTrade(inputItem1, outputItem, inputItem2, tradeDisabled, numberOfTradeUses, maximumNumberOfTradeUses, xp, specialPrice, priceMultiplier, demand);
        }

        /// <summary>
        /// Build an uncompressed Named Binary Tag blob for sending over the network
        /// </summary>
        /// <param name="nbt">Dictionary to encode as Nbt</param>
        /// <returns>Byte array for this NBT tag</returns>
        public byte[] GetNbt(Dictionary<string, object>? nbt)
        {
            return GetNbt(nbt, true);
        }

        /// <summary>
        /// Build an uncompressed Named Binary Tag blob for sending over the network (internal)
        /// </summary>
        /// <param name="nbt">Dictionary to encode as Nbt</param>
        /// <param name="root">TRUE if starting a new NBT tag, FALSE if processing a nested NBT tag</param>
        /// <returns>Byte array for this NBT tag</returns>
        private byte[] GetNbt(Dictionary<string, object>? nbt, bool root)
        {
            if (nbt == null || nbt.Count == 0)
                return new byte[] { 0 }; // TAG_End

            List<byte> bytes = new();

            if (root)
            {
                bytes.Add(10); // TAG_Compound

                // NBT root name
                string? rootName = null;

                if (nbt.TryGetValue("", out object? rootNameObj))
                    rootName = rootNameObj as string;

                rootName ??= "";

                bytes.AddRange(GetUShort((ushort)rootName.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(rootName));
            }

            foreach (var item in nbt)
            {
                // Skip NBT root name
                if (item.Key == "" && root)
                    continue;

                byte[] fieldNameLength = GetUShort((ushort)item.Key.Length);
                byte[] fieldName = Encoding.ASCII.GetBytes(item.Key);
                byte[] fieldData = GetNbtField(item.Value, out byte fieldType);
                bytes.Add(fieldType);
                bytes.AddRange(fieldNameLength);
                bytes.AddRange(fieldName);
                bytes.AddRange(fieldData);
            }

            bytes.Add(0); // TAG_End
            return bytes.ToArray();
        }

        /// <summary>
        /// Convert a single object into its NBT representation (internal)
        /// </summary>
        /// <param name="obj">Object to convert</param>
        /// <param name="fieldType">Field type for the passed object</param>
        /// <returns>Binary data for the passed object</returns>
        private byte[] GetNbtField(object obj, out byte fieldType)
        {
            switch (obj)
            {
                case byte:
                    fieldType = 1; // TAG_Byte
                    return new[] { (byte)obj };
                case short:
                    fieldType = 2; // TAG_Short
                    return GetShort((short)obj);
                case int:
                    fieldType = 3; // TAG_Int
                    return GetInt((int)obj);
                case long:
                    fieldType = 4; // TAG_Long
                    return GetLong((long)obj);
                case float:
                    fieldType = 5; // TAG_Float
                    return GetFloat((float)obj);
                case double:
                    fieldType = 6; // TAG_Double
                    return GetDouble((double)obj);
                case byte[]:
                    fieldType = 7; // TAG_Byte_Array
                    return (byte[])obj;
                case string:
                    {
                        fieldType = 8; // TAG_String
                        byte[] stringBytes = Encoding.UTF8.GetBytes((string)obj);
                        return ConcatBytes(GetUShort((ushort)stringBytes.Length), stringBytes);
                    }
                case object[]:
                    {
                        fieldType = 9; // TAG_List

                        List<object> list = new((object[])obj);
                        int arrayLengthTotal = list.Count;

                        // Treat empty list as TAG_Byte, length 0
                        if (arrayLengthTotal == 0)
                            return ConcatBytes(new[] { (byte)1 }, GetInt(0));

                        // Encode first list item, retain its type
                        string firstItemTypeString = list[0].GetType().Name;
                        byte[] firstItemBytes = GetNbtField(list[0], out byte firstItemType);
                        list.RemoveAt(0);

                        // Encode further list items, check they have the same type
                        List<byte> subsequentItemsBytes = new();
                        foreach (object item in list)
                        {
                            subsequentItemsBytes.AddRange(GetNbtField(item, out byte subsequentItemType));
                            if (subsequentItemType != firstItemType)
                                throw new InvalidDataException(
                                    "GetNbt: Cannot encode object[] list with mixed types: " + firstItemTypeString + ", " + item.GetType().Name + " into NBT!");
                        }

                        // Build NBT list: type, length, item array
                        return ConcatBytes(new[] { firstItemType }, GetInt(arrayLengthTotal), firstItemBytes, subsequentItemsBytes.ToArray());
                    }
                case Dictionary<string, object>:
                    fieldType = 10; // TAG_Compound
                    return GetNbt((Dictionary<string, object>)obj, false);
                case int[]:
                    {
                        fieldType = 11; // TAG_Int_Array

                        int[] srcIntList = (int[])obj;
                        List<byte> encIntList = new();
                        encIntList.AddRange(GetInt(srcIntList.Length));
                        foreach (int item in srcIntList)
                            encIntList.AddRange(GetInt(item));
                        return encIntList.ToArray();
                    }
                case long[]:
                    {
                        fieldType = 12; // TAG_Long_Array

                        long[] srcLongList = (long[])obj;
                        List<byte> encLongList = new();
                        encLongList.AddRange(GetInt(srcLongList.Length));
                        foreach (long item in srcLongList)
                            encLongList.AddRange(GetLong(item));
                        return encLongList.ToArray();
                    }
                default:
                    throw new InvalidDataException("GetNbt: Cannot encode data type " + obj.GetType().Name + " into NBT!");
            }
        }

        /// <summary>
        /// Build an integer for sending over the network
        /// </summary>
        /// <param name="paramInt">Integer to encode</param>
        /// <returns>Byte array for this integer</returns>
        public byte[] GetVarInt(int paramInt)
        {
            List<byte> bytes = new();
            while ((paramInt & -128) != 0)
            {
                bytes.Add((byte)(paramInt & 127 | 128));
                paramInt = (int)(((uint)paramInt) >> 7);
            }
            bytes.Add((byte)paramInt);
            return bytes.ToArray();
        }

        /// <summary>
        /// Build an boolean for sending over the network
        /// </summary>
        /// <param name="paramBool">Boolean to encode</param>
        /// <returns>Byte array for this boolean</returns>
        public byte[] GetBool(bool paramBool)
        {
            List<byte> bytes = new()
            {
                Convert.ToByte(paramBool)
            };
            return bytes.ToArray();
        }

        /// <summary>
        /// Get byte array representing a long integer
        /// </summary>
        /// <param name="number">Long to process</param>
        /// <returns>Array ready to send</returns>
        public byte[] GetLong(long number)
        {
            byte[] theLong = BitConverter.GetBytes(number);
            Array.Reverse(theLong);
            return theLong;
        }

        /// <summary>
        /// Get byte array representing an integer
        /// </summary>
        /// <param name="number">Integer to process</param>
        /// <returns>Array ready to send</returns>
        public byte[] GetInt(int number)
        {
            byte[] theInt = BitConverter.GetBytes(number);
            Array.Reverse(theInt);
            return theInt;
        }

        /// <summary>
        /// Get byte array representing a short
        /// </summary>
        /// <param name="number">Short to process</param>
        /// <returns>Array ready to send</returns>
        public byte[] GetShort(short number)
        {
            byte[] theShort = BitConverter.GetBytes(number);
            Array.Reverse(theShort);
            return theShort;
        }

        /// <summary>
        /// Get byte array representing an unsigned short
        /// </summary>
        /// <param name="number">Short to process</param>
        /// <returns>Array ready to send</returns>
        public byte[] GetUShort(ushort number)
        {
            byte[] theShort = BitConverter.GetBytes(number);
            Array.Reverse(theShort);
            return theShort;
        }

        /// <summary>
        /// Get byte array representing a double
        /// </summary>
        /// <param name="number">Double to process</param>
        /// <returns>Array ready to send</returns>
        public byte[] GetDouble(double number)
        {
            byte[] theDouble = BitConverter.GetBytes(number);
            Array.Reverse(theDouble); //Endianness
            return theDouble;
        }

        /// <summary>
        /// Get byte array representing a float
        /// </summary>
        /// <param name="number">Floalt to process</param>
        /// <returns>Array ready to send</returns>
        public byte[] GetFloat(float number)
        {
            byte[] theFloat = BitConverter.GetBytes(number);
            Array.Reverse(theFloat); //Endianness
            return theFloat;
        }

        /// <summary>
        /// Get byte array with length information prepended to it
        /// </summary>
        /// <param name="array">Array to process</param>
        /// <returns>Array ready to send</returns>
        public byte[] GetArray(byte[] array)
        {
            if (protocolversion < Protocol18Handler.MC_1_8_Version)
            {
                byte[] length = BitConverter.GetBytes((short)array.Length);
                Array.Reverse(length); //Endianness
                return ConcatBytes(length, array);
            }
            else return ConcatBytes(GetVarInt(array.Length), array);
        }

        /// <summary>
        /// Get a byte array from the given string for sending over the network, with length information prepended.
        /// </summary>
        /// <param name="text">String to process</param>
        /// <returns>Array ready to send</returns>
        public byte[] GetString(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return ConcatBytes(GetVarInt(bytes.Length), bytes);
        }

        /// <summary>
        /// Get a byte array representing the given location encoded as an unsigned long
        /// </summary>
        /// <remarks>
        /// A modulo will be applied if the location is outside the following ranges:
        /// X: -33,554,432 to +33,554,431
        /// Y: -2,048 to +2,047
        /// Z: -33,554,432 to +33,554,431
        /// </remarks>
        /// <returns>Location representation as ulong</returns>
        public byte[] GetLocation(Location location)
        {
            byte[] locationBytes;
            if (protocolversion >= Protocol18Handler.MC_1_14_Version)
            {
                locationBytes = BitConverter.GetBytes(((((ulong)location.X) & 0x3FFFFFF) << 38) | ((((ulong)location.Z) & 0x3FFFFFF) << 12) | (((ulong)location.Y) & 0xFFF));
            }
            else locationBytes = BitConverter.GetBytes(((((ulong)location.X) & 0x3FFFFFF) << 38) | ((((ulong)location.Y) & 0xFFF) << 26) | (((ulong)location.Z) & 0x3FFFFFF));
            Array.Reverse(locationBytes); //Endianness
            return locationBytes;
        }

        /// <summary>
        /// Get a byte array representing the given item as an item slot
        /// </summary>
        /// <param name="item">Item</param>
        /// <param name="itemPalette">Item Palette</param>
        /// <returns>Item slot representation</returns>
        public byte[] GetItemSlot(Item? item, ItemPalette itemPalette)
        {
            List<byte> slotData = new();
            if (protocolversion > Protocol18Handler.MC_1_13_Version)
            {
                // MC 1.13 and greater
                if (item == null || item.IsEmpty)
                    slotData.AddRange(GetBool(false)); // No item
                else
                {
                    slotData.AddRange(GetBool(true)); // Item is present
                    slotData.AddRange(GetVarInt(itemPalette.ToId(item.Type)));
                    slotData.Add((byte)item.Count);
                    slotData.AddRange(GetNbt(item.NBT));
                }
            }
            else
            {
                // MC 1.12.2 and lower
                if (item == null || item.IsEmpty)
                    slotData.AddRange(GetShort(-1));
                else
                {
                    slotData.AddRange(GetShort((short)itemPalette.ToId(item.Type)));
                    slotData.Add((byte)item.Count);
                    slotData.AddRange(GetNbt(item.NBT));
                }
            }
            return slotData.ToArray();
        }

        /// <summary>
        /// Get a byte array representing an array of item slots
        /// </summary>
        /// <param name="items">Items</param>
        /// <param name="itemPalette">Item Palette</param>
        /// <returns>Array of Item slot representations</returns>
        public byte[] GetSlotsArray(Dictionary<int, Item> items, ItemPalette itemPalette)
        {
            byte[] slotsArray = new byte[items.Count];

            foreach (KeyValuePair<int, Item> item in items)
            {
                slotsArray = ConcatBytes(slotsArray, GetShort((short)item.Key), GetItemSlot(item.Value, itemPalette));
            }

            return slotsArray;
        }

        /// <summary>
        /// Get protocol block face from Direction
        /// </summary>
        /// <param name="direction">Direction</param>
        /// <returns>Block face byte enum</returns>
        public byte GetBlockFace(Direction direction)
        {
            return direction switch
            {
                Direction.Down => 0,
                Direction.Up => 1,
                Direction.North => 2,
                Direction.South => 3,
                Direction.West => 4,
                Direction.East => 5,
                _ => throw new NotImplementedException("Unknown direction: " + direction.ToString()),
            };
        }

        /// <summary>
        /// Get a byte array from the given uuid
        /// </summary>
        /// <param name="uuid">UUID of Player/Entity</param>
        /// <returns>UUID representation</returns>
        public byte[] GetUUID(Guid UUID)
        {
            return UUID.ToBigEndianBytes();
        }

        /// <summary>
        /// Easily append several byte arrays
        /// </summary>
        /// <param name="bytes">Bytes to append</param>
        /// <returns>Array containing all the data</returns>
        public byte[] ConcatBytes(params byte[][] bytes)
        {
            List<byte> result = new();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        /// <summary>
        /// Convert a byte array to an hexadecimal string representation (for debugging purposes)
        /// </summary>
        /// <param name="bytes">Byte array</param>
        /// <returns>String representation</returns>
        public string ByteArrayToString(byte[]? bytes)
        {
            if (bytes == null)
                return "null";
            else
                return BitConverter.ToString(bytes).Replace("-", " ");
        }

        /// <summary>
        /// Write LastSeenMessageList
        /// </summary>
        /// <param name="msgList">Message.LastSeenMessageList</param>
        /// <param name="isOnlineMode">Whether the server is in online mode</param>
        /// <returns>Message.LastSeenMessageList Packet Data</returns>
        public byte[] GetLastSeenMessageList(Message.LastSeenMessageList msgList, bool isOnlineMode)
        {
            if (!isOnlineMode)
                return GetVarInt(0);                                                   // Message list size
            else
            {
                List<byte> fields = new();
                fields.AddRange(GetVarInt(msgList.entries.Length));                    // Message list size
                foreach (Message.LastSeenMessageList.Entry entry in msgList.entries)
                {
                    fields.AddRange(entry.profileId.ToBigEndianBytes());               // UUID
                    fields.AddRange(GetVarInt(entry.lastSignature.Length));            // Signature length
                    fields.AddRange(entry.lastSignature);                              // Signature data
                }
                return fields.ToArray();
            }
        }

        /// <summary>
        /// Write LastSeenMessageList.Acknowledgment
        /// </summary>
        /// <param name="ack">Acknowledgment</param>
        /// <param name="isOnlineMode">Whether the server is in online mode</param>
        /// <returns>Acknowledgment Packet Data</returns>
        public byte[] GetAcknowledgment(Message.LastSeenMessageList.Acknowledgment ack, bool isOnlineMode)
        {
            List<byte> fields = new();
            fields.AddRange(GetLastSeenMessageList(ack.lastSeen, isOnlineMode));
            if (!isOnlineMode || ack.lastReceived == null)
                fields.AddRange(GetBool(false));                                        // Has last received message
            else
            {
                fields.AddRange(GetBool(true));
                fields.AddRange(ack.lastReceived.profileId.ToBigEndianBytes());         // Has last received message
                fields.AddRange(GetVarInt(ack.lastReceived.lastSignature.Length));      // Last received message signature length
                fields.AddRange(ack.lastReceived.lastSignature);                        // Last received message signature data
            }
            return fields.ToArray();
        }
    }
}
