using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MinecraftClient
{
    /// <summary>
    /// Monitor file changes on disk
    /// </summary>
    public class FileMonitor : IDisposable
    {
        private FileSystemWatcher monitor = null;
        private Thread polling = null;

        /// <summary>
        /// Create a new FileMonitor and start monitoring
        /// </summary>
        /// <param name="folder">Folder to monitor</param>
        /// <param name="filename">Filename inside folder</param>
        /// <param name="handler">Callback for file changes</param>
        public FileMonitor(string folder, string filename, FileSystemEventHandler handler)
        {
            if (Settings.DebugMessages)
            {
                string callerClass = new System.Diagnostics.StackFrame(1).GetMethod().DeclaringType.Name;
                ConsoleIO.WriteLineFormatted(Translations.Get("filemonitor.init", callerClass, Path.Combine(folder, filename)));
            }

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
                {
                    string callerClass = new System.Diagnostics.StackFrame(1).GetMethod().DeclaringType.Name;
                    ConsoleIO.WriteLineFormatted(Translations.Get("filemonitor.fail", callerClass));
                }

                monitor = null;
                polling = new Thread(() => PollingThread(folder, filename, handler));
                polling.Name = String.Format("{0} Polling thread: {1}", this.GetType().Name, Path.Combine(folder, filename));
                polling.Start();
            }
        }

        /// <summary>
        /// Stop monitoring and dispose the inner resources
        /// </summary>
        public void Dispose()
        {
            if (monitor != null)
                monitor.Dispose();
            if (polling != null)
                polling.Abort();
        }

        /// <summary>
        /// Fallback polling thread for use when operating system does not support FileSystemWatcher
        /// </summary>
        /// <param name="folder">Folder to monitor</param>
        /// <param name="filename">File name to monitor</param>
        /// <param name="handler">Callback when file changes</param>
        private void PollingThread(string folder, string filename, FileSystemEventHandler handler)
        {
            string filePath = Path.Combine(folder, filename);
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

        /// <summary>
        /// Opens a text file, reads all lines of the file, and then closes the file. Retry several times if the file is in use
        /// </summary>
        /// <param name="filePath">The file to open for reading</param>
        /// <param name="maxTries">Maximum read attempts</param>
        /// <param name="encoding">Encoding (default is UTF8)</param>
        /// <exception cref="System.IO.IOException">Thrown when failing to read the file despite multiple retries</exception>
        /// <returns>All lines</returns>
        public static string[] ReadAllLinesWithRetries(string filePath, int maxTries = 3, Encoding encoding = null)
        {
            int attempt = 0;
            if (encoding == null)
                encoding = Encoding.UTF8;
            while (true)
            {
                try
                {
                    return File.ReadAllLines(filePath, encoding);
                }
                catch (IOException)
                {
                    attempt++;
                    if (attempt < maxTries)
                        Thread.Sleep(new Random().Next(50, 100) * attempt); // Back-off like CSMA/CD
                    else throw;
                }
            }
        }

        /// <summary>
        /// Creates a new file, writes a collection of strings to the file, and then closes the file.
        /// </summary>
        /// <param name="filePath">The file to open for writing</param>
        /// <param name="lines">The lines to write to the file</param>
        /// <param name="maxTries">Maximum read attempts</param>
        /// <param name="encoding">Encoding (default is UTF8)</param>
        public static void WriteAllLinesWithRetries(string filePath, IEnumerable<string> lines, int maxTries = 3, Encoding encoding = null)
        {
            int attempt = 0;
            if (encoding == null)
                encoding = Encoding.UTF8;
            while (true)
            {
                try
                {
                    File.WriteAllLines(filePath, lines, encoding);
                    return;
                }
                catch (IOException)
                {
                    attempt++;
                    if (attempt < maxTries)
                        Thread.Sleep(new Random().Next(50, 100) * attempt); // Back-off like CSMA/CD
                    else throw;
                }
            }
        }
    }
}
