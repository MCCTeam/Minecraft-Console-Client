using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Protocol.Message;

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
        /// <param name="offset">Amount of bytes to read</param>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte[] ReadData(int offset, Queue<byte> cache)
        {
            byte[] result = new byte[offset];
            for (int i = 0; i < offset; i++)
                result[i] = cache.Dequeue();
            return result;
        }

        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <param name="dest">Storage results</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void ReadDataReverse(Queue<byte> cache, Span<byte> dest)
        {
            for (int i = (dest.Length - 1); i >= 0; --i)
                dest[i] = cache.Dequeue();
        }

        /// <summary>
        /// Remove some data from the cache
        /// </summary>
        /// <param name="offset">Amount of bytes to drop</param>
        /// <param name="cache">Cache of bytes to drop</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void DropData(int offset, Queue<byte> cache)
        {
            while (offset-- > 0)
                cache.Dequeue();
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
        /// Skip a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        public void SkipNextString(Queue<byte> cache)
        {
            int length = ReadNextVarInt(cache);
            DropData(length, cache);
        }

        /// <summary>
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool ReadNextBool(Queue<byte> cache)
        {
            return ReadNextByte(cache) != 0x00;
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public short ReadNextShort(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[2];
            for (int i = (2 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToInt16(rawValue);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The integer value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int ReadNextInt(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[4];
            for (int i = (4 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToInt32(rawValue);
        }

        /// <summary>
        /// Read a long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public long ReadNextLong(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[8];
            for (int i = (8 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToInt64(rawValue);
        }

        /// <summary>
        /// Read an unsigned short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ushort ReadNextUShort(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[2];
            for (int i = (2 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToUInt16(rawValue);
        }

        /// <summary>
        /// Read an unsigned long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ulong ReadNextULong(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[8];
            for (int i = (8 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToUInt64(rawValue);
        }

        /// <summary>
        /// Read a Location encoded as an ulong field and remove it from the cache
        /// </summary>
        /// <returns>The Location value</returns>
        public Location ReadNextLocation(Queue<byte> cache)
        {
            ulong locEncoded = ReadNextULong(cache);
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
        /// Read several little endian unsigned short integers at once from a cache of bytes and remove them from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public ushort[] ReadNextUShortsLittleEndian(int amount, Queue<byte> cache)
        {
            byte[] rawValues = ReadData(2 * amount, cache);
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
        /// Read a byte array from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The byte array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte[] ReadNextByteArray(Queue<byte> cache)
        {
            int len = protocolversion >= Protocol18Handler.MC_1_8_Version
                ? ReadNextVarInt(cache)
                : ReadNextShort(cache);
            return ReadData(len, cache);
        }

        /// <summary>
        /// Read a byte array with given length from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <param name="length">Length of the bytes array</param>
        /// <returns>The byte array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte[] ReadNextByteArray(Queue<byte> cache, int length)
        {
            return ReadData(length, cache);
        }

        /// <summary>
        /// Reads a length-prefixed array of unsigned long integers and removes it from the cache
        /// </summary>
        /// <returns>The unsigned long integer values</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ulong[] ReadNextULongArray(Queue<byte> cache)
        {
            int len = ReadNextVarInt(cache);
            ulong[] result = new ulong[len];
            for (int i = 0; i < len; i++)
                result[i] = ReadNextULong(cache);
            return result;
        }

        /// <summary>
        /// Read a double from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The double value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public double ReadNextDouble(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[8];
            for (int i = (8 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToDouble(rawValue);
        }

        /// <summary>
        /// Read a float from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The float value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public float ReadNextFloat(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[4];
            for (int i = (4 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToSingle(rawValue);
        }

        /// <summary>
        /// Read an integer from the network
        /// </summary>
        /// <returns>The integer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadNextVarIntRAW(SocketWrapper socket)
        {
            int i = 0;
            int j = 0;
            byte b;
            while (true)
            {
                b = socket.ReadDataRAW(1)[0];
                i |= (b & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((b & 0x80) != 128) break;
            }

            return i;
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
        /// <param name="cache">Cache of bytes to read from</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SkipNextVarInt(Queue<byte> cache)
        {
            while (true)
                if ((ReadNextByte(cache) & 0x80) != 128)
                    break;
        }

        /// <summary>
        /// Read an "extended short", which is actually an int of some kind, from the cache of bytes.
        /// This is only done with forge.  It looks like it's a normal short, except that if the high
        /// bit is set, it has an extra byte.
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The int</returns>
        public int ReadNextVarShort(Queue<byte> cache)
        {
            ushort low = ReadNextUShort(cache);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = ReadNextByte(cache);
            }

            return ((high & 0xFF) << 15) | low;
        }

        /// <summary>
        /// Read a long from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The long value</returns>
        public long ReadNextVarLong(Queue<byte> cache)
        {
            int numRead = 0;
            long result = 0;
            byte read;
            do
            {
                read = ReadNextByte(cache);
                long value = (read & 0x7F);
                result |= (value << (7 * numRead));

                numRead++;
                if (numRead > 10)
                {
                    throw new OverflowException("VarLong is too big");
                }
            } while ((read & 0x80) != 0);

            return result;
        }

        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte ReadNextByte(Queue<byte> cache)
        {
            byte result = cache.Dequeue();
            return result;
        }

        /// <summary>
        /// Read an uncompressed Named Binary Tag blob and remove it from the cache
        /// </summary>
        public Dictionary<string, object> ReadNextNbt(Queue<byte> cache)
        {
            return ReadNextNbt(cache, true);
        }

        /// <summary>
        /// Read a single item slot from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public Item? ReadNextItemSlot(Queue<byte> cache, ItemPalette itemPalette)
        {
            // MC 1.13.2 and greater
            if (protocolversion >= Protocol18Handler.MC_1_13_Version)
            {
                var itemPresent = ReadNextBool(cache);

                if (!itemPresent)
                    return null;

                var itemId = ReadNextVarInt(cache);

                if (itemId == -1)
                    return null;

                var type = itemPalette.FromId(itemId);
                var itemCount = ReadNextByte(cache);
                var nbt = ReadNextNbt(cache);
                return new Item(type, itemCount, nbt);
            }
            else
            {
                var itemId = ReadNextShort(cache);

                if (itemId == -1)
                    return null;

                var itemCount = ReadNextByte(cache);
                var data = ReadNextShort(cache);
                var nbt = ReadNextNbt(cache);

                // For 1.8 - 1.12.2 we combine Item Id and Item Data/Damage to a single value using: (id << 16) | data
                return new Item(itemPalette.FromId((itemId << 16) | (ushort)data), itemCount, data, nbt);
            }
        }

        /// <summary>
        /// Read entity information from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="entityPalette">Mappings for converting entity type Ids to EntityType</param>
        /// <param name="living">TRUE for living entities (layout differs)</param>
        /// <returns>Entity information</returns>
        public Entity ReadNextEntity(Queue<byte> cache, EntityPalette entityPalette, bool living)
        {
            int entityID = ReadNextVarInt(cache);
            Guid entityUUID = Guid.Empty;

            // UUID field added in 1.9 +
            if (protocolversion >= Protocol18Handler.MC_1_9_Version)
                entityUUID = ReadNextUUID(cache);

            // Entity type data type change from byte to varint in 1.13.2
            EntityType entityType;

            if (living)
            {
                // For living entities the Type field was changed to VarInt from Byte in 1.11 +
                if (protocolversion >= Protocol18Handler.MC_1_11_Version)
                    entityType = entityPalette.FromId(ReadNextVarInt(cache), living);
                else entityType = entityPalette.FromId(ReadNextByte(cache), living);
            }
            else
            {
                // For non-living entities the Type field was changed to VarInt from Byte in 1.13.2 +
                if (protocolversion >= Protocol18Handler.MC_1_13_2_Version)
                    entityType = entityPalette.FromId(ReadNextVarInt(cache), living);
                else entityType = entityPalette.FromId(ReadNextByte(cache), living);
            }

            Double entityX, entityY, entityZ;

            if (protocolversion < Protocol18Handler.MC_1_9_Version)
            {
                entityX = ReadNextInt(cache) / 32.0D; // X
                entityY = ReadNextInt(cache) / 32.0D; // Y
                entityZ = ReadNextInt(cache) / 32.0D; // Z
            }
            else
            {
                entityX = ReadNextDouble(cache); // X
                entityY = ReadNextDouble(cache); // Y
                entityZ = ReadNextDouble(cache); // Z
            }

            int data = -1;
            byte entityPitch, entityYaw;

            if (living)
            {
                entityYaw = ReadNextByte(cache); // Yaw
                entityPitch = ReadNextByte(cache); // Pitch
                entityPitch = ReadNextByte(cache); // Head Pitch
            }
            else
            {
                entityPitch = ReadNextByte(cache); // Pitch
                entityYaw = ReadNextByte(cache); // Yaw

                if (protocolversion >= Protocol18Handler.MC_1_19_Version)
                    entityYaw = ReadNextByte(cache); // Head Yaw

                // Data
                data = protocolversion >= Protocol18Handler.MC_1_19_Version
                    ? ReadNextVarInt(cache)
                    : ReadNextInt(cache);
            }

            // In 1.8 those 3 fields for Velocity are optional
            if (protocolversion < Protocol18Handler.MC_1_9_Version)
            {
                if (data != 0)
                {
                    ReadNextShort(cache);
                    ReadNextShort(cache);
                    ReadNextShort(cache);
                }
            }
            else
            {
                ReadNextShort(cache);
                ReadNextShort(cache);
                ReadNextShort(cache);
            }

            return new Entity(entityID, entityType, new Location(entityX, entityY, entityZ), entityYaw, entityPitch,
                data);
        }

        /// <summary>
        /// Read an uncompressed Named Binary Tag blob and remove it from the cache (internal)
        /// </summary>
        private Dictionary<string, object> ReadNextNbt(Queue<byte> cache, bool root)
        {
            Dictionary<string, object> nbtData = new();

            if (root)
            {
                if (cache.Peek() == 0) // TAG_End
                {
                    cache.Dequeue();
                    return nbtData;
                }

                var nextId = cache.Dequeue();
                if (protocolversion < Protocol18Handler.MC_1_20_2_Version)
                {
                    if (nextId is not 10) // TAG_Compound
                        throw new System.IO.InvalidDataException(
                            "Failed to decode NBT: Does not start with TAG_Compound");

                    // NBT root name
                    var rootName = Encoding.ASCII.GetString(ReadData(ReadNextUShort(cache), cache));

                    if (!string.IsNullOrEmpty(rootName))
                        nbtData[""] = rootName;
                }
                // In 1.20.2 The root TAG_Compound doesn't have a name
                // In 1.20.3+ The root can be TAG_Compound or TAG_String
                else
                {
                    if (nextId is not (10 or 8)) // TAG_Compound or TAG_String
                        throw new System.IO.InvalidDataException(
                            "Failed to decode NBT: Does not start with TAG_Compound or TAG_String");

                    // Read TAG_String
                    if (nextId is 8)
                    {
                        var byteArrayLength = ReadNextUShort(cache);
                        var result = Encoding.UTF8.GetString(ReadData(byteArrayLength, cache));

                        return new Dictionary<string, object>()
                        {
                            { "", result }
                        };
                    }
                }
            }

            while (true)
            {
                int fieldType = ReadNextByte(cache);

                if (fieldType == 0) // TAG_End
                    return nbtData;

                int fieldNameLength = ReadNextUShort(cache);
                string fieldName = Encoding.ASCII.GetString(ReadData(fieldNameLength, cache));
                object fieldValue = ReadNbtField(cache, fieldType);

                // This will override previous tags with the same name
                nbtData[fieldName] = fieldValue;
            }
        }

        /// <summary>
        /// Read a single Named Binary Tag field of the specified type and remove it from the cache
        /// </summary>
        private object ReadNbtField(Queue<byte> cache, int fieldType)
        {
            switch (fieldType)
            {
                case 1: // TAG_Byte
                    return ReadNextByte(cache);
                case 2: // TAG_Short
                    return ReadNextShort(cache);
                case 3: // TAG_Int
                    return ReadNextInt(cache);
                case 4: // TAG_Long
                    return ReadNextLong(cache);
                case 5: // TAG_Float
                    return ReadNextFloat(cache);
                case 6: // TAG_Double
                    return ReadNextDouble(cache);
                case 7: // TAG_Byte_Array
                    return ReadData(ReadNextInt(cache), cache);
                case 8: // TAG_String
                    return Encoding.UTF8.GetString(ReadData(ReadNextUShort(cache), cache));
                case 9: // TAG_List
                    int listType = ReadNextByte(cache);
                    int listLength = ReadNextInt(cache);
                    object[] listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = ReadNbtField(cache, listType);
                    return listItems;
                case 10: // TAG_Compound
                    return ReadNextNbt(cache, false);
                case 11: // TAG_Int_Array
                    listType = 3;
                    listLength = ReadNextInt(cache);
                    listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = ReadNbtField(cache, listType);
                    return listItems;
                case 12: // TAG_Long_Array
                    listType = 4;
                    listLength = ReadNextInt(cache);
                    listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = ReadNbtField(cache, listType);
                    return listItems;
                default:
                    throw new System.IO.InvalidDataException("Failed to decode NBT: Unknown field type " + fieldType);
            }
        }

        /// <summary>
        /// Read a Entity MetaData and remove it from the cache
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="itemPalette"></param>
        /// <param name="metadataPalette"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="System.IO.InvalidDataException"></exception>
        public Dictionary<int, object?> ReadNextMetadata(Queue<byte> cache, ItemPalette itemPalette,
            EntityMetadataPalette metadataPalette)
        {
            Dictionary<int, object?> data = new();
            byte key = ReadNextByte(cache);
            byte terminteValue = protocolversion <= Protocol18Handler.MC_1_8_Version
                ? (byte)0x7f // 1.8 (https://wiki.vg/index.php?title=Entity_metadata&oldid=6220#Entity_Metadata_Format)
                : (byte)0xff; // 1.9+

            while (key != terminteValue)
            {
                int typeId = protocolversion <= Protocol18Handler.MC_1_8_Version
                    ? key >> 5 // 1.8
                    : ReadNextVarInt(cache); // 1.9+

                EntityMetaDataType type;
                try
                {
                    type = metadataPalette.GetDataType(typeId);
                }
                catch (KeyNotFoundException)
                {
                    throw new System.IO.InvalidDataException("Unknown Metadata Type ID " + typeId +
                                                             ". Is this up to date for new MC Version?");
                }

                if (protocolversion <= Protocol18Handler.MC_1_8_Version)
                    key = (byte)(key & 0x1f);

                // Value's data type is depended on Type
                object? value = null;

                switch (type)
                {
                    case EntityMetaDataType.Short: // 1.8 only
                        value = ReadNextShort(cache);
                        break;
                    case EntityMetaDataType.Int: // 1.8 only
                        value = ReadNextInt(cache);
                        break;
                    case EntityMetaDataType.Vector3Int: // 1.8 only
                        value = new List<int>()
                        {
                            ReadNextInt(cache),
                            ReadNextInt(cache),
                            ReadNextInt(cache),
                        };
                        break;
                    case EntityMetaDataType.Byte: // byte
                        value = ReadNextByte(cache);
                        break;
                    case EntityMetaDataType.VarInt: // VarInt
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.VarLong: // Long
                        value = ReadNextVarLong(cache);
                        break;
                    case EntityMetaDataType.Float: // Float
                        value = ReadNextFloat(cache);
                        break;
                    case EntityMetaDataType.String: // String
                        value = ReadNextString(cache);
                        break;
                    case EntityMetaDataType.Chat: // Chat
                        value = ReadNextChat(cache);
                        break;
                    case EntityMetaDataType.OptionalChat: // Optional Chat
                        if (ReadNextBool(cache))
                            value = ReadNextChat(cache);
                        break;
                    case EntityMetaDataType.Slot: // Slot
                        value = ReadNextItemSlot(cache, itemPalette);
                        break;
                    case EntityMetaDataType.Boolean: // Boolean
                        value = ReadNextBool(cache);
                        break;
                    case EntityMetaDataType.Rotation: // Rotation (3x floats)
                        value = new List<float>
                        {
                            ReadNextFloat(cache),
                            ReadNextFloat(cache),
                            ReadNextFloat(cache)
                        };
                        break;
                    case EntityMetaDataType.Position: // Position
                        value = ReadNextLocation(cache);
                        break;
                    case EntityMetaDataType.OptionalPosition: // Optional Position
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextLocation(cache);
                        }

                        break;
                    case EntityMetaDataType.Direction: // Direction (VarInt)
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.OptionalUuid: // Optional UUID
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextUUID(cache);
                        }

                        break;
                    case EntityMetaDataType.BlockId: // BlockID (VarInt)
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.OptionalBlockId: // Optional BlockID (VarInt)
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.Nbt: // NBT
                        value = ReadNextNbt(cache);
                        break;
                    case EntityMetaDataType.Particle: // Particle
                        // Skip data only, not used
                        ReadParticleData(cache, itemPalette);
                        break;
                    case EntityMetaDataType.VillagerData: // Villager Data (3x VarInt)
                        value = new List<int>
                        {
                            ReadNextVarInt(cache),
                            ReadNextVarInt(cache),
                            ReadNextVarInt(cache)
                        };
                        break;
                    case EntityMetaDataType.OptionalVarInt: // Optional VarInt
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextVarInt(cache);
                        }

                        break;
                    case EntityMetaDataType.Pose: // Pose
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.CatVariant: // Cat Variant
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.FrogVariant: // Frog Varint
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.GlobalPosition: // GlobalPos
                        // Dimension and blockPos, currently not in use
                        value = new Tuple<string, Location>(ReadNextString(cache), ReadNextLocation(cache));
                        break;
                    case EntityMetaDataType.OptionalGlobalPosition:
                        // FIXME: wiki.vg is bool + string + location
                        //        but minecraft-data is bool + string
                        if (ReadNextBool(cache))
                        {
                            // Dimension and blockPos, currently not in use
                            value = new Tuple<string, Location>(ReadNextString(cache), ReadNextLocation(cache));
                        }

                        break;
                    case EntityMetaDataType.PaintingVariant: // Painting Variant
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.SnifferState: // Sniffer state
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.Vector3: // Vector 3f
                        value = new List<float>
                        {
                            ReadNextFloat(cache),
                            ReadNextFloat(cache),
                            ReadNextFloat(cache)
                        };
                        break;
                    case EntityMetaDataType.Quaternion: // Quaternion
                        value = new List<float>
                        {
                            ReadNextFloat(cache),
                            ReadNextFloat(cache),
                            ReadNextFloat(cache),
                            ReadNextFloat(cache)
                        };
                        break;
                }

                data[key] = value;
                key = ReadNextByte(cache);
            }

            return data;
        }

        /// <summary>
        /// Currently not handled. Reading data only
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="itemPalette"></param>
        public void ReadParticleData(Queue<byte> cache, ItemPalette itemPalette)
        {
            if (protocolversion < Protocol18Handler.MC_1_13_Version)
                return;

            var particleId = ReadNextVarInt(cache);

            // Documentation:
            // 1.19.3+ - https://wiki.vg/index.php?title=Data_types&oldid=17986
            // 1.18 - https://wiki.vg/index.php?title=Data_types&oldid=17180
            // 1.17 - https://wiki.vg/index.php?title=Data_types&oldid=16740
            // 1.15 - https://wiki.vg/index.php?title=Data_types&oldid=15338
            // 1.13 - https://wiki.vg/index.php?title=Data_types&oldid=14271

            switch (particleId)
            {
                case 2:
                    // 1.18 +
                    if (protocolversion > Protocol18Handler.MC_1_17_1_Version)
                        ReadNextVarInt(cache); // Block state (minecraft:block)
                    break;
                case 3:
                    if (protocolversion is < Protocol18Handler.MC_1_17_Version or > Protocol18Handler.MC_1_17_1_Version)
                        ReadNextVarInt(
                            cache); // Block State (minecraft:block before 1.18, minecraft:block_marker after 1.18)
                    break;
                case 4:
                    if (protocolversion is Protocol18Handler.MC_1_17_Version or Protocol18Handler.MC_1_17_1_Version)
                        ReadNextVarInt(cache); // Block State (minecraft:block)
                    break;
                case 11:
                    // 1.13 - 1.14.4
                    if (protocolversion < Protocol18Handler.MC_1_15_Version)
                        ReadDustParticle(cache);
                    break;
                case 14:
                    // 1.15 - 1.16.5 and 1.18 - 1.19.4
                    if (protocolversion is >= Protocol18Handler.MC_1_15_Version and < Protocol18Handler.MC_1_17_Version
                        or > Protocol18Handler.MC_1_17_1_Version)
                        ReadDustParticle(cache);
                    break;
                case 15:
                    switch (protocolversion)
                    {
                        case Protocol18Handler.MC_1_17_Version or Protocol18Handler.MC_1_17_1_Version:
                            ReadDustParticle(cache);
                            break;
                        case > Protocol18Handler.MC_1_17_1_Version:
                            ReadDustParticleColorTransition(cache);
                            break;
                    }

                    break;
                case 16:
                    if (protocolversion is Protocol18Handler.MC_1_17_Version or Protocol18Handler.MC_1_17_1_Version)
                        ReadDustParticleColorTransition(cache);
                    break;
                case 23:
                    // 1.15 - 1.16.5
                    if (protocolversion is >= Protocol18Handler.MC_1_15_Version and < Protocol18Handler.MC_1_17_Version)
                        ReadNextVarInt(cache); // Block State (minecraft:falling_dust)
                    break;
                case 24:
                    // 1.18 - 1.19.2 onwards
                    if (protocolversion is > Protocol18Handler.MC_1_17_1_Version
                        and < Protocol18Handler.MC_1_19_3_Version)
                        ReadNextVarInt(cache); // Block State (minecraft:falling_dust)
                    break;
                case 25:
                    // 1.17 - 1.17.1 and 1.19.3 onwards
                    if (protocolversion is Protocol18Handler.MC_1_17_Version or Protocol18Handler.MC_1_17_1_Version
                        or >= Protocol18Handler.MC_1_19_3_Version)
                        ReadNextVarInt(cache); // Block State (minecraft:falling_dust)
                    break;
                case 27:
                    // 1.13 - 1.14.4
                    if (protocolversion < Protocol18Handler.MC_1_15_Version)
                        ReadNextItemSlot(cache, itemPalette); // Item (minecraft:item)
                    break;
                case 30:
                    if (protocolversion >= Protocol18Handler.MC_1_19_3_Version)
                        ReadNextFloat(cache); // Roll (minecraft:sculk_charge)
                    break;
                case 32:
                    // 1.15 - 1.16.5
                    if (protocolversion is >= Protocol18Handler.MC_1_15_Version and < Protocol18Handler.MC_1_17_Version)
                        ReadNextItemSlot(cache, itemPalette); // Item (minecraft:item)
                    break;
                case 36:
                    switch (protocolversion)
                    {
                        // 1.17 - 1.17.1
                        case Protocol18Handler.MC_1_17_Version or Protocol18Handler.MC_1_17_1_Version:
                            ReadNextItemSlot(cache, itemPalette); // Item (minecraft:item)
                            break;
                        case > Protocol18Handler.MC_1_17_1_Version and < Protocol18Handler.MC_1_19_3_Version:
                            // minecraft:vibration
                            ReadNextLocation(cache); // Origin (Starting Position)
                            ReadNextLocation(cache); // Destination (Ending Position)
                            ReadNextVarInt(cache); // Ticks
                            break;
                    }

                    break;
                case 37:
                    // minecraft:vibration
                    if (protocolversion is Protocol18Handler.MC_1_17_Version or Protocol18Handler.MC_1_17_1_Version)
                    {
                        ReadNextDouble(cache); // Origin X
                        ReadNextDouble(cache); // Origin Y
                        ReadNextDouble(cache); // Origin Z
                        ReadNextDouble(cache); // Destination X
                        ReadNextDouble(cache); // Destination Y
                        ReadNextDouble(cache); // Destination Z
                        ReadNextInt(cache); // Ticks
                    }

                    break;
                case 39:
                    if (protocolversion >= Protocol18Handler.MC_1_19_3_Version)
                        ReadNextItemSlot(cache, itemPalette); // Item (minecraft:item)
                    break;
                case 40:
                    if (protocolversion >= Protocol18Handler.MC_1_19_3_Version)
                    {
                        var positionSourceType = ReadNextString(cache);
                        switch (positionSourceType)
                        {
                            case "minecraft:block":
                                ReadNextLocation(cache);
                                break;
                            case "minecraft:entity":
                                ReadNextVarInt(cache);
                                ReadNextFloat(cache);
                                break;
                        }

                        ReadNextVarInt(cache);
                    }

                    break;
            }
        }

        private void ReadDustParticle(Queue<byte> cache)
        {
            ReadNextFloat(cache); // Red
            ReadNextFloat(cache); // Green
            ReadNextFloat(cache); // Blue
            ReadNextFloat(cache); // Scale
        }

        private void ReadDustParticleColorTransition(Queue<byte> cache)
        {
            ReadNextFloat(cache); // From red
            ReadNextFloat(cache); // From green
            ReadNextFloat(cache); // From blue
            ReadNextFloat(cache); // Scale
            ReadNextFloat(cache); // To red
            ReadNextFloat(cache); // To green
            ReadNextFloat(cache); // To Blue
        }

        /// <summary>
        /// Read a single villager trade from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public VillagerTrade ReadNextTrade(Queue<byte> cache, ItemPalette itemPalette)
        {
            Item inputItem1 = ReadNextItemSlot(cache, itemPalette)!;
            Item outputItem = ReadNextItemSlot(cache, itemPalette)!;

            Item? inputItem2 = null;

            if (protocolversion >= Protocol18Handler.MC_1_19_3_Version)
                inputItem2 = ReadNextItemSlot(cache, itemPalette);
            else
            {
                if (ReadNextBool(cache)) //check if villager has second item
                    inputItem2 = ReadNextItemSlot(cache, itemPalette);
            }

            bool tradeDisabled = ReadNextBool(cache);
            int numberOfTradeUses = ReadNextInt(cache);
            int maximumNumberOfTradeUses = ReadNextInt(cache);
            int xp = ReadNextInt(cache);
            int specialPrice = ReadNextInt(cache);
            float priceMultiplier = ReadNextFloat(cache);
            int demand = ReadNextInt(cache);
            return new VillagerTrade(inputItem1, outputItem, inputItem2, tradeDisabled, numberOfTradeUses,
                maximumNumberOfTradeUses, xp, specialPrice, priceMultiplier, demand);
        }

        public string ReadNextChat(Queue<byte> cache)
        {
            if (protocolversion >= Protocol18Handler.MC_1_20_4_Version)
            {
                // Read as NBT
                var r = ReadNextNbt(cache);
                var msg = ChatParser.ParseText(r);
                return msg;
            }
            else
            {
                // Read as String
                var json = ReadNextString(cache);
                return ChatParser.ParseText(json);
            }
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

                if (nbt.ContainsKey(""))
                    rootName = nbt[""] as string;

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
            if (obj is byte)
            {
                fieldType = 1; // TAG_Byte
                return new[] { (byte)obj };
            }
            else if (obj is short)
            {
                fieldType = 2; // TAG_Short
                return GetShort((short)obj);
            }
            else if (obj is int)
            {
                fieldType = 3; // TAG_Int
                return GetInt((int)obj);
            }
            else if (obj is long)
            {
                fieldType = 4; // TAG_Long
                return GetLong((long)obj);
            }
            else if (obj is float)
            {
                fieldType = 5; // TAG_Float
                return GetFloat((float)obj);
            }
            else if (obj is double)
            {
                fieldType = 6; // TAG_Double
                return GetDouble((double)obj);
            }
            else if (obj is byte[])
            {
                fieldType = 7; // TAG_Byte_Array
                return (byte[])obj;
            }
            else if (obj is string)
            {
                fieldType = 8; // TAG_String
                byte[] stringBytes = Encoding.UTF8.GetBytes((string)obj);
                return ConcatBytes(GetUShort((ushort)stringBytes.Length), stringBytes);
            }
            else if (obj is object[])
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
                        throw new System.IO.InvalidDataException(
                            "GetNbt: Cannot encode object[] list with mixed types: " + firstItemTypeString + ", " +
                            item.GetType().Name + " into NBT!");
                }

                // Build NBT list: type, length, item array
                return ConcatBytes(new[] { firstItemType }, GetInt(arrayLengthTotal), firstItemBytes,
                    subsequentItemsBytes.ToArray());
            }
            else if (obj is Dictionary<string, object>)
            {
                fieldType = 10; // TAG_Compound
                return GetNbt((Dictionary<string, object>)obj, false);
            }
            else if (obj is int[])
            {
                fieldType = 11; // TAG_Int_Array

                int[] srcIntList = (int[])obj;
                List<byte> encIntList = new();
                encIntList.AddRange(GetInt(srcIntList.Length));
                foreach (int item in srcIntList)
                    encIntList.AddRange(GetInt(item));
                return encIntList.ToArray();
            }
            else if (obj is long[])
            {
                fieldType = 12; // TAG_Long_Array

                long[] srcLongList = (long[])obj;
                List<byte> encLongList = new();
                encLongList.AddRange(GetInt(srcLongList.Length));
                foreach (long item in srcLongList)
                    encLongList.AddRange(GetLong(item));
                return encLongList.ToArray();
            }
            else
            {
                throw new System.IO.InvalidDataException("GetNbt: Cannot encode data type " + obj.GetType().Name +
                                                         " into NBT!");
            }
        }

        /// <summary>
        /// Build an integer for sending over the network
        /// </summary>
        /// <param name="paramInt">Integer to encode</param>
        /// <returns>Byte array for this integer</returns>
        public static byte[] GetVarInt(int paramInt)
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
        public static byte[] GetLong(long number)
        {
            byte[] theLong = BitConverter.GetBytes(number);
            Array.Reverse(theLong);
            return theLong;
        }

        /// <summary>
        /// Get byte array representing a long integer
        /// </summary>
        /// <param name="number">Long to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetULong(ulong number)
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
        public static byte[] GetInt(int number)
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
                locationBytes = BitConverter.GetBytes(((((ulong)location.X) & 0x3FFFFFF) << 38) |
                                                      ((((ulong)location.Z) & 0x3FFFFFF) << 12) |
                                                      (((ulong)location.Y) & 0xFFF));
            }
            else
                locationBytes = BitConverter.GetBytes(((((ulong)location.X) & 0x3FFFFFF) << 38) |
                                                      ((((ulong)location.Y) & 0xFFF) << 26) |
                                                      (((ulong)location.Z) & 0x3FFFFFF));

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
                    // For 1.8 - 1.12.2 we combine Item Id and Item Data to a single value using: (id << 16) | data
                    // Thus to get an ID we do a right shift by 16 bits
                    slotData.AddRange(GetShort((short)(itemPalette.ToId(item.Type) >> 16)));
                    slotData.Add((byte)item.Count);
                    slotData.Add((byte)item.Data);
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
        public static byte[] GetUUID(Guid UUID)
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
                return GetVarInt(0); // Message list size
            else
            {
                List<byte> fields = new();
                fields.AddRange(GetVarInt(msgList.entries.Length)); // Message list size
                foreach (Message.LastSeenMessageList.AcknowledgedMessage entry in msgList.entries)
                {
                    fields.AddRange(entry.profileId.ToBigEndianBytes()); // UUID
                    fields.AddRange(GetVarInt(entry.signature.Length)); // Signature length
                    fields.AddRange(entry.signature); // Signature data
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
                fields.AddRange(GetBool(false)); // Has last received message
            else
            {
                fields.AddRange(GetBool(true));
                fields.AddRange(ack.lastReceived.profileId.ToBigEndianBytes()); // Has last received message
                fields.AddRange(GetVarInt(ack.lastReceived.signature.Length)); // Last received message signature length
                fields.AddRange(ack.lastReceived.signature); // Last received message signature data
            }

            return fields.ToArray();
        }
    }
}