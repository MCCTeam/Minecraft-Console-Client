using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace MinecraftClient.Crypto.Streams
{
    /// <summary>
    /// An encrypted stream using AES, used for encrypting network data on the fly using AES.
    /// This is a mono-compatible adaptation which only sends and receive 16 bytes at a time, and manually transforms blocks.
    /// Data is cached before reaching the 128bits block size necessary for mono which is not CFB-8 compatible.
    /// </summary>

    public class MonoAesStream : Stream, IAesStream
    {
        IPaddingProvider pad;
        ICryptoTransform enc;
        ICryptoTransform dec;
        List<byte> dec_cache = new List<byte>();
        List<byte> tosend_cache = new List<byte>();
        public MonoAesStream(System.IO.Stream stream, byte[] key, IPaddingProvider provider)
        {
            BaseStream = stream;
            RijndaelManaged aes = GenerateAES(key);
            enc = aes.CreateEncryptor();
            dec = aes.CreateDecryptor();
            pad = provider;
        }
        public System.IO.Stream BaseStream { get; set; }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int ReadByte()
        {
            byte[] temp = new byte[1];
            Read(temp, 0, 1);
            return temp[0];
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            while (dec_cache.Count < count)
            {
                byte[] temp_in = new byte[16];
                byte[] temp_out = new byte[16];
                int read = 0;
                while (read < 16)
                    read += BaseStream.Read(temp_in, read, 16 - read);
                dec.TransformBlock(temp_in, 0, 16, temp_out, 0);
                foreach (byte b in temp_out)
                    dec_cache.Add(b);
            }

            for (int i = offset; i - offset < count; i++)
            {
                buffer[i] = dec_cache[0];
                dec_cache.RemoveAt(0);
            }

            return count;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte b)
        {
            Write(new byte[] { b }, 0, 1);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i - offset < count; i++)
                tosend_cache.Add(buffer[i]);

            if (tosend_cache.Count < 16)
                tosend_cache.AddRange(pad.getPaddingPacket());

            while (tosend_cache.Count > 16)
            {
                byte[] temp_in = new byte[16];
                byte[] temp_out = new byte[16];
                for (int i = 0; i < 16; i++)
                {
                    temp_in[i] = tosend_cache[0];
                    tosend_cache.RemoveAt(0);
                }
                enc.TransformBlock(temp_in, 0, 16, temp_out, 0);
                BaseStream.Write(temp_out, 0, 16);
            }
        }

        private RijndaelManaged GenerateAES(byte[] key)
        {
            RijndaelManaged cipher = new RijndaelManaged();
            cipher.Mode = CipherMode.CFB;
            cipher.Padding = PaddingMode.None;
            cipher.KeySize = 128;
            cipher.FeedbackSize = 8;
            cipher.Key = key;
            cipher.IV = key;
            return cipher;
        }
    }
}
