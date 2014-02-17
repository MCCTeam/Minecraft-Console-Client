using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MinecraftClient.Network.IO
{
    // Read- were not checked.
    public partial class MinecraftStream
    {
        static MinecraftStream()
        {
            StringEncoding = Encoding.UTF8;
        }

        public static Encoding StringEncoding;
        
        private List<byte[]> ToSend = new List<byte[]>();

        private static byte[] getVarInt(int paramInt)
        {
            List<byte> bytes = new List<byte>();
            while ((paramInt & -128) != 0)
            {
                bytes.Add((byte) (paramInt & 127 | 128));
                paramInt = (int) (((uint) paramInt) >> 7);
            }
            bytes.Add((byte) paramInt);
            return bytes.ToArray();
        }

        public byte ReadUInt8()
        {
            int value = BaseStream.ReadByte();
            if (value == -1)
                throw new EndOfStreamException();
            return (byte)value;
        }

        public void WriteUInt8(byte value)
        {
            ToSend.Add(new[] {value});
        }

        public sbyte ReadInt8()
        {
            return (sbyte)ReadUInt8();
        }

        public void WriteInt8(sbyte value)
        {
            ToSend.Add(new[] {(byte)value});
        }

        public ushort ReadUInt16()
        {
            return (ushort)(
                (ReadUInt8() << 8) |
                ReadUInt8());
        }

        public void WriteUInt16(ushort value)
        {
            ToSend.Add(new[]
            {
                (byte)((value & 0xFF00) >> 8),
                (byte)(value & 0xFF)
            });
        }

        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        public void WriteInt16(short value)
        {
            ToSend.Add(getVarInt(value));
        }

        public uint ReadUInt32()
        {
            return (uint)(
                (ReadUInt8() << 24) |
                (ReadUInt8() << 16) |
                (ReadUInt8() << 8) |
                 ReadUInt8());
        }

        public void WriteUInt32(uint value)
        {
            ToSend.Add(new[]
            {
                (byte)((value & 0xFF000000) >> 24),
                (byte)((value & 0xFF0000) >> 16),
                (byte)((value & 0xFF00) >> 8),
                (byte)(value & 0xFF)
            });
        }

        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        public void WriteInt32(int value)
        {
            ToSend.Add(getVarInt(value));
        }

        public ulong ReadUInt64()
        {
            return unchecked(
                   ((ulong)ReadUInt8() << 56) |
                   ((ulong)ReadUInt8() << 48) |
                   ((ulong)ReadUInt8() << 40) |
                   ((ulong)ReadUInt8() << 32) |
                   ((ulong)ReadUInt8() << 24) |
                   ((ulong)ReadUInt8() << 16) |
                   ((ulong)ReadUInt8() << 8) |
                    ReadUInt8());
        }

        public void WriteUInt64(ulong value)
        {
            ToSend.Add(new[]
            {
                (byte)((value & 0xFF00000000000000) >> 56),
                (byte)((value & 0xFF000000000000) >> 48),
                (byte)((value & 0xFF0000000000) >> 40),
                (byte)((value & 0xFF00000000) >> 32),
                (byte)((value & 0xFF000000) >> 24),
                (byte)((value & 0xFF0000) >> 16),
                (byte)((value & 0xFF00) >> 8),
                (byte)(value & 0xFF)
            });
        }

        public long ReadInt64()
        {
            return (long)ReadUInt64();
        }

        public void WriteInt64(long value)
        {
            ToSend.Add(getVarInt((int)value));
        }

        public byte[] ReadUInt8Array(int length)
        {
            var result = new byte[length];
            if (length == 0) return result;
            int n = length;
            while (true)
            {
                n -= Read(result, length - n, n);
                if (n == 0)
                    break;
                Thread.Sleep(1);
            }
            return result;
        }

        public void WriteUInt8Array(byte[] value)
        {
            ToSend.Add(value);
        }

        public sbyte[] ReadInt8Array(int length)
        {
            return (sbyte[])(Array)ReadUInt8Array(length);
        }

        public void WriteInt8Array(sbyte[] value)
        {
            ToSend.Add((byte[])(Array)value);
            ToSend.Add(getVarInt(value.Length));
        }

        public ushort[] ReadUInt16Array(int length)
        {
            var result = new ushort[length];
            if (length == 0) return result;
            for (int i = 0; i < length; i++)
                result[i] = ReadUInt16();
            return result;
        }

        public void WriteUInt16Array(ushort[] value)
        {
            foreach (ushort t in value)
                ToSend.Add(getVarInt(t));
        }

        public short[] ReadInt16Array(int length)
        {
            return (short[])(Array)ReadUInt16Array(length);
        }

        public void WriteInt16Array(short[] value)
        {
            foreach (short t in value)
                ToSend.Add(getVarInt(t));
        }

        public uint[] ReadUInt32Array(int length)
        {
            var result = new uint[length];
            if (length == 0) return result;
            for (int i = 0; i < length; i++)
                result[i] = ReadUInt32();
            return result;
        }

        public void WriteUInt32Array(uint[] value)
        {
            foreach (uint t in value)
                ToSend.Add(getVarInt((int)t));
        }

        public int[] ReadInt32Array(int length)
        {
            return (int[])(Array)ReadUInt32Array(length);
        }

        public void WriteInt32Array(int[] value)
        {
            foreach (int t in value)
                ToSend.Add(getVarInt(t));
        }

        public ulong[] ReadUInt64Array(int length)
        {
            var result = new ulong[length];
            if (length == 0) return result;
            for (int i = 0; i < length; i++)
                result[i] = ReadUInt64();
            return result;
        }

        public void WriteUInt64Array(ulong[] value)
        {
            foreach (ulong t in value)
                ToSend.Add(getVarInt((int)t));
        }

        public long[] ReadInt64Array(int length)
        {
            return (long[])(Array)ReadUInt64Array(length);
        }

        public void WriteInt64Array(long[] value)
        {
            foreach (long t in value)
                ToSend.Add(getVarInt((int)t));
        }

        public unsafe float ReadSingle()
        {
            uint value = ReadUInt32();
            return *(float*)&value;
        }

        public unsafe void WriteSingle(float value)
        {
            ToSend.Add(getVarInt(*(int*)&value));
        }

        public unsafe double ReadDouble()
        {
            ulong value = ReadUInt64();
            return *(double*)&value;
        }

        public unsafe void WriteDouble(double value)
        {
            ToSend.Add(getVarInt(*(int*)&value));
        }

        public bool ReadBoolean()
        {
            return ReadUInt8() != 0;
        }

        public void WriteBoolean(bool value)
        {
            ToSend.Add(getVarInt(value ? (byte)1 : (byte)0));
        }

        public string ReadString()
        {
            ushort length = ReadUInt16();
            if (length == 0) return string.Empty;
            var data = ReadUInt8Array(length * 2);
            return StringEncoding.GetString(data);
        }

        public void WriteString(string value)//
        {
            if (value.Length > 0)
            {
                ToSend.Add(getVarInt(value.Length));
                ToSend.Add(StringEncoding.GetBytes(value));
            }
            else ToSend.Add(getVarInt(value.Length));
        }

        public void Send()
        {
            byte[] array = ToSend
                .SelectMany(a => a)
                .ToArray();

            ToSend.Insert(0, getVarInt(array.Length));

            array = ToSend
                .SelectMany(a => a)
                .ToArray();

            Write(array, 0, array.Length);
            ToSend.Clear();
        }
    }
}