using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using java.security;
using java.security.spec;
using javax.crypto;
using javax.crypto.spec;

namespace MinecraftClient
{
    /// <summary>
    /// Cryptographic functions ported from Minecraft Source Code (Java). Decompiled with MCP. Copy, paste, little adjustements.
    /// </summary>

    public class Crypto
    {
        public static PublicKey GenerateRSAPublicKey(byte[] key)
        {
            X509EncodedKeySpec localX509EncodedKeySpec = new X509EncodedKeySpec(key);
            KeyFactory localKeyFactory = KeyFactory.getInstance("RSA");
            return localKeyFactory.generatePublic(localX509EncodedKeySpec);
        }

        public static SecretKey GenerateAESPrivateKey()
        {
            AesManaged aes = new AesManaged();
            aes.KeySize = 128; aes.GenerateKey();
            return new SecretKeySpec(aes.Key, "AES"); 
        }

        public static byte[] Encrypt(Key par0Key, byte[] par1ArrayOfByte)
        {
            return func_75885_a(1, par0Key, par1ArrayOfByte);
        }

        private static byte[] func_75885_a(int par0, Key par1Key, byte[] par2ArrayOfByte)
        {
            try
            {
                return cypherencrypt(par0, par1Key.getAlgorithm(), par1Key).doFinal(par2ArrayOfByte);
            }
            catch (IllegalBlockSizeException var4)
            {
                var4.printStackTrace();
            }
            catch (BadPaddingException var5)
            {
                var5.printStackTrace();
            }

            Console.Error.WriteLine("Cipher data failed!");
            return null;
        }
        private static Cipher cypherencrypt(int par0, String par1Str, Key par2Key)
        {
            try
            {
                Cipher var3 = Cipher.getInstance(par1Str);
                var3.init(par0, par2Key);
                return var3;
            }
            catch (InvalidKeyException var4)
            {
                var4.printStackTrace();
            }
            catch (NoSuchAlgorithmException var5)
            {
                var5.printStackTrace();
            }
            catch (NoSuchPaddingException var6)
            {
                var6.printStackTrace();
            }

            Console.Error.WriteLine("Cipher creation failed!");
            return null;
        }

        /* Server Hash Computation */

        public static string getServerHash(String toencode, PublicKey par1PublicKey, SecretKey par2SecretKey)
        {
            byte[] hash = digest(new byte[][] { Encoding.GetEncoding("iso-8859-1").GetBytes(toencode), par2SecretKey.getEncoded(), par1PublicKey.getEncoded() });
            bool negative = (hash[0] & 0x80) == 0x80;
            if (negative) { hash = TwosComplimentLittleEndian(hash); }
            string result = GetHexString(hash).TrimStart('0');
            if (negative) { result = "-" + result; }
            return result;
        }

        private static byte[] digest(byte[][] tohash)
        {
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            for (int i = 0; i < tohash.Length; i++)
                sha1.TransformBlock(tohash[i], 0, tohash[i].Length, tohash[i], 0);
            sha1.TransformFinalBlock(new byte[] { }, 0, 0);
            return sha1.Hash;
        }

        private static string GetHexString(byte[] p)
        {
            string result = string.Empty;
            for (int i = 0; i < p.Length; i++)
                result += p[i].ToString("x2");
            return result;
        }

        private static byte[] TwosComplimentLittleEndian(byte[] p)
        {
            int i;
            bool carry = true;
            for (i = p.Length - 1; i >= 0; i--)
            {
                p[i] = (byte)~p[i];
                if (carry)
                {
                    carry = p[i] == 0xFF;
                    p[i]++;
                }
            }
            return p;
        }

        /* Encrypted Stream between client and server */

        public static AesStream SwitchToAesMode(System.IO.Stream stream, Key key)
        {
            return new AesStream(stream, key.getEncoded());
        }

        /// <summary>
        /// An encrypted stream using AES
        /// </summary>

        public class AesStream : System.IO.Stream
        {
            CryptoStream enc;
            CryptoStream dec;
            public AesStream(System.IO.Stream stream, byte[] key)
            {
                BaseStream = stream;
                enc = new CryptoStream(stream, GenerateAES(key).CreateEncryptor(), CryptoStreamMode.Write);
                dec = new CryptoStream(stream, GenerateAES(key).CreateDecryptor(), CryptoStreamMode.Read);
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
                return dec.ReadByte();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return dec.Read(buffer, offset, count);
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
                enc.WriteByte(b);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                enc.Write(buffer, offset, count);
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
}
