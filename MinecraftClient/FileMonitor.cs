using System;
using System.IO;
using System.Threading;

namespace MinecraftClient.Protocol.Session
{
    /// <summary>
    /// Monitor session file changes on disk
    /// </summary>
    class SessionFileMonitor
    {
        private FileSystemWatcher monitor;
        private Thread polling;

        /// <summary>
        /// Create a new SessionFileMonitor and start monitoring
        /// </summary>
        /// <param name="folder">Folder to monitor</param>
        /// <param name="filename">Filename inside folder</param>
        /// <param name="handler">Callback for file changes</param>
        public SessionFileMonitor(string folder, string filename, FileSystemEventHandler handler)
        {
            if (Settings.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8Initializing disk session cache using FileSystemWatcher");

            try
            {
                monitor = new FileSystemWatcher();
                monitor.Path = folder;
                monitor.IncludeSubdirectories = false;
                monitor.Filter = filename;
                monitor.NotifyFilter = NotifyFilters.LastWrite;
                monitor.Changed += handler;
                monitor.EnableRaisingEvents = true;
            }
            catch
            {
                if (Settings.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8Failed to initialize FileSystemWatcher, retrying using Polling");

                polling = new Thread(() => PollingThread(folder, filename, handler));
                polling.Start();
            }
        }

        /// <summary>
        /// Fallback polling thread for use when operating system does not support FileSystemWatcher
        /// </summary>
        /// <param name="folder">Folder to monitor</param>
        /// <param name="filename">File name to monitor</param>
        /// <param name="handler">Callback when file changes</param>
        private void PollingThread(string folder, string filename, FileSystemEventHandler handler)
        {
            string filePath = String.Concat(folder, Path.DirectorySeparatorChar, filename);
            DateTime lastWrite = GetLastWrite(filePath);
            while (true)
            {
                Thread.Sleep(5000);
                DateTime lastWriteNew = GetLastWrite(filePath);
                if (lastWriteNew != lastWrite)
                {
                    lastWrite = lastWriteNew;
                    handler(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, folder, filename));
                }
            }
        }

        /// <summary>
        /// Get last write for a given file
        /// </summary>
        /// <param name="path">File path to get last write from</param>
        /// <returns>Last write time, or DateTime.MinValue if the file does not exist</returns>
        private DateTime GetLastWrite(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                return fileInfo.LastWriteTime;
            }
            else return DateTime.MinValue;
        }
    }
}
