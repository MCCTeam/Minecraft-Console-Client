using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MinecraftClient.Mapping;
using MinecraftClient.Crypto;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Inventory.ItemPalettes;

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
        private int protocolversion;

        /// <summary>
        /// Initialize a new DataTypes instance
        /// </summary>
        /// <param name="protocol">Protocol version</param>
        public DataTypes(int protocol)
        {
            this.protocolversion = protocol;
        }

        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>
        public byte[] ReadData(int offset, Queue<byte> cache)
        {
            byte[] result = new byte[offset];
            for (int i = 0; i < offset; i++)
                result[i] = cache.Dequeue();
            return result;
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
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>
        public bool ReadNextBool(Queue<byte> cache)
        {
            return ReadNextByte(cache) != 0x00;
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>
        public short ReadNextShort(Queue<byte> cache)
        {
            byte[] rawValue = ReadData(2, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt16(rawValue, 0);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The integer value</returns>
        public int ReadNextInt(Queue<byte> cache)
        {
            byte[] rawValue = ReadData(4, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt32(rawValue, 0);
        }

        /// <summary>
        /// Read a long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public long ReadNextLong(Queue<byte> cache)
        {
            byte[] rawValue = ReadData(8, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt64(rawValue, 0);
        }

        /// <summary>
        /// Read an unsigned short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public ushort ReadNextUShort(Queue<byte> cache)
        {
            byte[] rawValue = ReadData(2, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToUInt16(rawValue, 0);
        }

        /// <summary>
        /// Read an unsigned long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public ulong ReadNextULong(Queue<byte> cache)
        {
            byte[] rawValue = ReadData(8, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToUInt64(rawValue, 0);
        }

        /// <summary>
        /// Read a Location encoded as an ulong field and remove it from the cache
        /// </summary>
        /// <returns>The Location value</returns>
        public Location ReadNextLocation(Queue<byte> cache)
        {
            ulong locEncoded = ReadNextULong(cache);
            int x, y, z;
            if (protocolversion >= Protocol18Handler.MC114Version)
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
            if (x >= 33554432)
                x -= 67108864;
            if (y >= 2048)
                y -= 4096;
            if (z >= 33554432)
                z -= 67108864;
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
            byte[] javaUUID = ReadData(16, cache);
            Guid guid = new Guid(javaUUID);
            if (BitConverter.IsLittleEndian)
                guid = guid.ToLittleEndian();
            return guid;
        }

        /// <summary>
        /// Read a byte array from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The byte array</returns>
        public byte[] ReadNextByteArray(Queue<byte> cache)
        {
            int len = protocolversion >= Protocol18Handler.MC18Version
                ? ReadNextVarInt(cache)
                : ReadNextShort(cache);
            return ReadData(len, cache);
        }

        /// <summary>
        /// Reads a length-prefixed array of unsigned long integers and removes it from the cache
        /// </summary>
        /// <returns>The unsigned long integer values</returns>
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
        public double ReadNextDouble(Queue<byte> cache)
        {
            byte[] rawValue = ReadData(8, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToDouble(rawValue, 0);
        }

        /// <summary>
        /// Read a float from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The float value</returns>
        public float ReadNextFloat(Queue<byte> cache)
        {
            byte[] rawValue = ReadData(4, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToSingle(rawValue, 0);
        }

        /// <summary>
        /// Read an integer from the network
        /// </summary>
        /// <returns>The integer</returns>
        public int ReadNextVarIntRAW(SocketWrapper socket)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            while (true)
            {
                k = socket.ReadDataRAW(1)[0];
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public int ReadNextVarInt(Queue<byte> cache)
        {
            string rawData = BitConverter.ToString(cache.ToArray());
            int i = 0;
            int j = 0;
            int k = 0;
            while (true)
            {
                k = ReadNextByte(cache);
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big " + rawData);
                if ((k & 0x80) != 128) break;
            }
            return i;
        }

        /// <summary>
        /// Skip a VarInt from a cache of bytes with better performance
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        public void SkipNextVarInt(Queue<byte> cache)
        {
            while (true)
            {
                if ((ReadNextByte(cache) & 0x80) != 128)
                    break;
            }
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
        public Item ReadNextItemSlot(Queue<byte> cache, ItemPalette itemPalette)
        {
            List<byte> slotData = new List<byte>();
            if (protocolversion > Protocol18Handler.MC113Version)
            {
                // MC 1.13 and greater
                bool itemPresent = ReadNextBool(cache);
                if (itemPresent)
                {
                    ItemType type = itemPalette.FromId(ReadNextVarInt(cache));
                    byte itemCount = ReadNextByte(cache);
                    Dictionary<string, object> nbt = ReadNextNbt(cache);
                    return new Item(type, itemCount, nbt);
                }
                else return null;
            }
            else
            {
                // MC 1.12.2 and lower
                short itemID = ReadNextShort(cache);
                if (itemID == -1)
                    return null;
                byte itemCount = ReadNextByte(cache);
                short itemDamage = ReadNextShort(cache);
                Dictionary<string, object> nbt = ReadNextNbt(cache);
                return new Item(itemPalette.FromId(itemID), itemCount, nbt);
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

            if (protocolversion > Protocol18Handler.MC18Version)
            {
                entityUUID = ReadNextUUID(cache);
            }

            EntityType entityType;
            // Entity type data type change from byte to varint after 1.14
            if (protocolversion > Protocol18Handler.MC113Version)
            {
                entityType = entityPalette.FromId(ReadNextVarInt(cache), living);
            }
            else
            {
                entityType = entityPalette.FromId(ReadNextByte(cache), living);
            }
            
            Double entityX = ReadNextDouble(cache);
            Double entityY = ReadNextDouble(cache);
            Double entityZ = ReadNextDouble(cache);
            byte entityYaw = ReadNextByte(cache);
            byte entityPitch = ReadNextByte(cache);

            if (living)
            {
                byte entityHeadPitch = ReadNextByte(cache);
            }
            else
            {
                int metadata = ReadNextInt(cache);
            }

            short velocityX = ReadNextShort(cache);
            short velocityY = ReadNextShort(cache);
            short velocityZ = ReadNextShort(cache);

            return new Entity(entityID, entityType, new Location(entityX, entityY, entityZ));
        }

        /// <summary>
        /// Read an uncompressed Named Binary Tag blob and remove it from the cache (internal)
        /// </summary>
        private Dictionary<string, object> ReadNextNbt(Queue<byte> cache, bool root)
        {
            Dictionary<string, object> nbtData = new Dictionary<string, object>();

            if (root)
            {
                if (cache.Peek() == 0) // TAG_End
                {
                    cache.Dequeue();
                    return nbtData;
                }
                if (cache.Peek() != 10) // TAG_Compound
                    throw new System.IO.InvalidDataException("Failed to decode NBT: Does not start with TAG_Compound");
                ReadNextByte(cache); // Tag type (TAG_Compound)

                // NBT root name
                string rootName = Encoding.ASCII.GetString(ReadData(ReadNextUShort(cache), cache));
                if (!String.IsNullOrEmpty(rootName))
                    nbtData[""] = rootName;
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

        public Dictionary<int, object> ReadNextMetadata(Queue<byte> cache, ItemPalette itemPalette)
        {
            Dictionary<int, object> data = new Dictionary<int, object>();
            byte key = ReadNextByte(cache);
            while (key != 0xff)
            {
                int type = ReadNextVarInt(cache);

                // starting from 1.13, Optional Chat is inserted as number 5 in 1.13 and IDs after 5 got shifted.
                // Increase type ID by 1 if
                // - below 1.13
                // - type ID larger than 4
                if (protocolversion < Protocol18Handler.MC113Version)
                {
                    if (type > 4)
                    {
                        type += 1;
                    }
                }
                // Value's data type is depended on Type
                object value = null;

                // This is backward compatible since new type is appended to the end
                // Version upgrade note
                // - Check type ID got shifted or not
                // - Add new type if any
                switch (type)
                {
                    case 0: // byte
                        value = ReadNextByte(cache);
                        break;
                    case 1: // VarInt
                        value = ReadNextVarInt(cache);
                        break;
                    case 2: // Float
                        value = ReadNextFloat(cache);
                        break;
                    case 3: // String
                        value = ReadNextString(cache);
                        break;
                    case 4: // Chat
                        value = ReadNextString(cache);
                        break;
                    case 5: // Optional Chat
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextString(cache);
                        }
                        break;
                    case 6: // Slot
                        value = ReadNextItemSlot(cache, itemPalette);
                        break;
                    case 7: // Boolean
                        value = ReadNextBool(cache);
                        break;
                    case 8: // Rotation (3x floats)
                        List<float> t = new List<float>();
                        t.Add(ReadNextFloat(cache));
                        t.Add(ReadNextFloat(cache));
                        t.Add(ReadNextFloat(cache));
                        value = t;
                        break;
                    case 9: // Position
                        value = ReadNextLocation(cache);
                        break;
                    case 10: // Optional Position
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextLocation(cache);
                        }
                        break;
                    case 11: // Direction (VarInt)
                        value = ReadNextVarInt(cache);
                        break;
                    case 12: // Optional UUID
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextUUID(cache);
                        }
                        break;
                    case 13: // Optional BlockID (VarInt)
                        value = ReadNextVarInt(cache);
                        break;
                    case 14: // NBT
                        value = ReadNextNbt(cache);
                        break;
                    case 15: // Particle
                             // Currecutly not handled. Reading data only
                        int ParticleID = ReadNextVarInt(cache);
                        switch (ParticleID)
                        {
                            case 3:
                                ReadNextVarInt(cache);
                                break;
                            case 14:
                                ReadNextFloat(cache);
                                ReadNextFloat(cache);
                                ReadNextFloat(cache);
                                ReadNextFloat(cache);
                                break;
                            case 23:
                                ReadNextVarInt(cache);
                                break;
                            case 32:
                                ReadNextItemSlot(cache, itemPalette);
                                break;
                        }
                        break;
                    case 16: // Villager Data (3x VarInt)
                        List<int> d = new List<int>();
                        d.Add(ReadNextVarInt(cache));
                        d.Add(ReadNextVarInt(cache));
                        d.Add(ReadNextVarInt(cache));
                        value = d;
                        break;
                    case 17: // Optional VarInt
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextVarInt(cache);
                        }
                        break;
                    case 18: // Pose
                        value = ReadNextVarInt(cache);
                        break;
                    default:
                        throw new System.IO.InvalidDataException("Unknown Metadata Type ID " + type + ". Is this up to date for new MC Version?");
                }
                data[key] = value;
                key = ReadNextByte(cache);
            }
            return data;
        }

        /// <summary>
        /// Read a single villager trade from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public VillagerTrade ReadNextTrade(Queue<byte> cache, ItemPalette itemPalette)
        {
            Item inputItem1 = ReadNextItemSlot(cache, itemPalette);
            Item outputItem = ReadNextItemSlot(cache, itemPalette);
            Item inputItem2 = null;
            if (ReadNextBool(cache)) //check if villager has second item
            {
                inputItem2 = ReadNextItemSlot(cache, itemPalette);
            }
            bool tradeDisabled = ReadNextBool(cache);
            int numberOfTradeUses = ReadNextInt(cache);
            int maximumNumberOfTradeUses = ReadNextInt(cache);
            int xp = ReadNextInt(cache);
            int specialPrice = ReadNextInt(cache);
            float priceMultiplier = ReadNextFloat(cache);
            int demand = ReadNextInt(cache);
            return new VillagerTrade(inputItem1, outputItem, inputItem2, tradeDisabled, numberOfTradeUses, maximumNumberOfTradeUses, xp, specialPrice, priceMultiplier, demand);
        }

        /// <summary>
        /// Build an uncompressed Named Binary Tag blob for sending over the network
        /// </summary>
        /// <param name="nbt">Dictionary to encode as Nbt</param>
        /// <returns>Byte array for this NBT tag</returns>
        public byte[] GetNbt(Dictionary<string, object> nbt)
        {
            return GetNbt(nbt, true);
        }

        /// <summary>
        /// Build an uncompressed Named Binary Tag blob for sending over the network (internal)
        /// </summary>
        /// <param name="nbt">Dictionary to encode as Nbt</param>
        /// <param name="root">TRUE if starting a new NBT tag, FALSE if processing a nested NBT tag</param>
        /// <returns>Byte array for this NBT tag</returns>
        private byte[] GetNbt(Dictionary<string, object> nbt, bool root)
        {
            if (nbt == null || nbt.Count == 0)
                return new byte[] { 0 }; // TAG_End

            List<byte> bytes = new List<byte>();

            if (root)
            {
                bytes.Add(10); // TAG_Compound

                // NBT root name
                string rootName = null;
                if (nbt.ContainsKey(""))
                    rootName = nbt[""] as string;
                if (rootName == null)
                    rootName = "";
                bytes.AddRange(GetUShort((ushort)rootName.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(rootName));
            }

            foreach (var item in nbt)
            {
                // Skip NBT root name
                if (item.Key == "" && root)
                    continue;

                byte fieldType;
                byte[] fieldNameLength = GetUShort((ushort)item.Key.Length);
                byte[] fieldName = Encoding.ASCII.GetBytes(item.Key);
                byte[] fieldData = GetNbtField(item.Value, out fieldType);
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

                List<object> list = new List<object>((object[])obj);
                int arrayLengthTotal = list.Count;

                // Treat empty list as TAG_Byte, length 0
                if (arrayLengthTotal == 0)
                    return ConcatBytes(new[] { (byte)1 }, GetInt(0));

                // Encode first list item, retain its type
                byte firstItemType;
                string firstItemTypeString = list[0].GetType().Name;
                byte[] firstItemBytes = GetNbtField(list[0], out firstItemType);
                list.RemoveAt(0);

                // Encode further list items, check they have the same type
                byte subsequentItemType;
                List<byte> subsequentItemsBytes = new List<byte>();
                foreach (object item in list)
                {
                    subsequentItemsBytes.AddRange(GetNbtField(item, out subsequentItemType));
                    if (subsequentItemType != firstItemType)
                        throw new System.IO.InvalidDataException(
                            "GetNbt: Cannot encode object[] list with mixed types: " + firstItemTypeString + ", " + item.GetType().Name + " into NBT!");
                }

                // Build NBT list: type, length, item array
                return ConcatBytes(new[] { firstItemType }, GetInt(arrayLengthTotal), firstItemBytes, subsequentItemsBytes.ToArray());
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
                List<byte> encIntList = new List<byte>();
                encIntList.AddRange(GetInt(srcIntList.Length));
                foreach (int item in srcIntList)
                    encIntList.AddRange(GetInt(item));
                return encIntList.ToArray();
            }
            else if (obj is long[])
            {
                fieldType = 12; // TAG_Long_Array

                long[] srcLongList = (long[])obj;
                List<byte> encLongList = new List<byte>();
                encLongList.AddRange(GetInt(srcLongList.Length));
                foreach (long item in srcLongList)
                    encLongList.AddRange(GetLong(item));
                return encLongList.ToArray();
            }
            else
            {
                throw new System.IO.InvalidDataException("GetNbt: Cannot encode data type " + obj.GetType().Name + " into NBT!");
            }
        }

        /// <summary>
        /// Build an integer for sending over the network
        /// </summary>
        /// <param name="paramInt">Integer to encode</param>
        /// <returns>Byte array for this integer</returns>
        public byte[] GetVarInt(int paramInt)
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
        /// Build an boolean for sending over the network
        /// </summary>
        /// <param name="paramBool">Boolean to encode</param>
        /// <returns>Byte array for this boolean</returns>
        public byte[] GetBool(bool paramBool)
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)Convert.ToByte(paramBool));
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
            if (protocolversion < Protocol18Handler.MC18Version)
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
            if (protocolversion >= Protocol18Handler.MC114Version)
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
        public byte[] GetItemSlot(Item item, ItemPalette itemPalette)
        {
            List<byte> slotData = new List<byte>();
            if (protocolversion > Protocol18Handler.MC113Version)
            {
                // MC 1.13 and greater
                if (item == null || item.IsEmpty)
                    slotData.Add(0); // No item
                else
                {
                    slotData.Add(1); // Item is present
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
        /// Get protocol block face from Direction
        /// </summary>
        /// <param name="direction">Direction</param>
        /// <returns>Block face byte enum</returns>
        public byte GetBlockFace(Direction direction)
        {
            switch (direction)
            {
                case Direction.Down: return 0;
                case Direction.Up: return 1;
                case Direction.North: return 2;
                case Direction.South: return 3;
                case Direction.West: return 4;
                case Direction.East: return 5;
                default: throw new NotImplementedException("Unknown direction: " + direction.ToString());
            }
        }

        /// <summary>
        /// Easily append several byte arrays
        /// </summary>
        /// <param name="bytes">Bytes to append</param>
        /// <returns>Array containing all the data</returns>
        public byte[] ConcatBytes(params byte[][] bytes)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        /// <summary>
        /// C-like atoi function for parsing an int from string
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <returns>Int parsed</returns>
        public int Atoi(string str)
        {
            return int.Parse(new string(str.Trim().TakeWhile(char.IsDigit).ToArray()));
        }
    }
}
