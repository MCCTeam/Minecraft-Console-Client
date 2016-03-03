using MinecraftClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace MinecraftClient.Cache
{
    public static class SessionCache
    {
        const string filename = "cache.bin";
        private static Dictionary<string, SessionToken> sessions = new Dictionary<string, SessionToken>();
        private static FileSystemWatcher cachemonitor = new FileSystemWatcher();

        private static BinaryFormatter formatter = new BinaryFormatter();

        public static bool Contains(string login)
        {
            return sessions.ContainsKey(login);
        }

        public static void Store(string login, SessionToken session)
        {
            if (Contains(login))
            {
                sessions[login] = session;
            }
            else
            {
                sessions.Add(login, session);
            }

            if (Settings.CacheType == CacheType.DISK)
            {
                SaveToDisk();
            }
        }

        public static SessionToken Get(string login)
        {
            return sessions[login];
        }

        public static bool LoadFromDisk()
        {
            cachemonitor.Path = AppDomain.CurrentDomain.BaseDirectory;
            cachemonitor.IncludeSubdirectories = false;
            cachemonitor.Filter = filename;
            cachemonitor.NotifyFilter = NotifyFilters.LastWrite;
            cachemonitor.Changed += new FileSystemEventHandler(OnChanged);
            cachemonitor.EnableRaisingEvents = true;

            return ReadCacheFile();
        }

        public static void OnChanged(object source, FileSystemEventArgs e)
        {
            ReadCacheFile();
        }

        private static bool ReadCacheFile()
        {
            if (File.Exists(filename))
            {
                try
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        sessions = (Dictionary<string, SessionToken>)formatter.Deserialize(fs);
                        return true;
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Error reading cached sessions from disk: " + ex.Message);
                }
                catch (SerializationException)
                {
                    Console.WriteLine("Error getting sessions from cache file ");
                }
            }
            return false;
        }

        public static void SaveToDisk()
        {
            bool fileexists = File.Exists(filename);

            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                cachemonitor.EnableRaisingEvents = false;
                if (fileexists)
                {
                    fs.SetLength(0);
                    fs.Flush();
                }

                formatter.Serialize(fs, sessions);
                cachemonitor.EnableRaisingEvents = true;
            }

        }

        private static byte[] GetHash(FileStream fs, bool resetposition = true)
        {
            using (var md5 = MD5.Create())
            {
                long pos = fs.Position;
                byte[] hash = md5.ComputeHash(fs);

                fs.Position = resetposition ? pos : fs.Position;
                return hash;
            }
        }

        private static bool HashesEqual(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length)
            {
                return false;
            }

            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    return false;
                }
            }

            return true;
        }

    }
}
