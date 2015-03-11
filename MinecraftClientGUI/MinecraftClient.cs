using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MinecraftClientGUI
{
    /// <summary>
    /// This class acts as a wrapper for MinecraftClient.exe
    /// Allows the rest of the program to consider this class as the Minecraft client itself.
    /// </summary>

    class MinecraftClient
    {
        public static string ExePath = "MinecraftClient.exe";
        public bool Disconnected { get { return disconnected; } }

        private LinkedList<string> OutputBuffer = new LinkedList<string>();
        private LinkedList<string> tabAutoCompleteBuffer = new LinkedList<string>();
        private bool disconnected = false;
        private Process Client;
        private Thread Reader;

        /// <summary>
        /// Start a client using command-line arguments
        /// </summary>
        /// <param name="args">Arguments to pass</param>

        public MinecraftClient(string[] args)
        {
            initClient("\"" + String.Join("\" \"", args) + "\" BasicIO");
        }

        /// <summary>
        /// Start the client using username, password and server IP
        /// </summary>
        /// <param name="username">Username or email</param>
        /// <param name="password">Password for the given username</param>
        /// <param name="serverip">Server IP to join</param>

        public MinecraftClient(string username, string password, string serverip)
        {
            initClient('"' + username + "\" \"" + password + "\" \"" + serverip + "\" BasicIO");
        }

        /// <summary>
        /// Inner function for launching the external console application
        /// </summary>
        /// <param name="arguments">Arguments to pass</param>

        private void initClient(string arguments)
        {
            if (File.Exists(ExePath))
            {
                Client = new Process();
                Client.StartInfo.FileName = ExePath;
                Client.StartInfo.Arguments = arguments;
                Client.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                Client.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
                Client.StartInfo.UseShellExecute = false;
                Client.StartInfo.RedirectStandardOutput = true;
                Client.StartInfo.RedirectStandardInput = true;
                Client.StartInfo.CreateNoWindow = true;
                Client.Start();

                Reader = new Thread(new ThreadStart(t_reader));
                Reader.Start();
            }
            else throw new FileNotFoundException("Cannot find Minecraft Client Executable!", ExePath);
        }

        /// <summary>
        /// Thread for reading output and app messages from the console
        /// </summary>

        private void t_reader()
        {
            while (true)
            {
                try
                {
                    string line = "";
                    while (line.Trim() == "")
                    {
                        line = Client.StandardOutput.ReadLine() + Client.MainWindowTitle;
                        if (line.Length > 0)
                        {
                            if (line == "Server was successfuly joined.") { disconnected = false; }
                            if (line == "You have left the server.") { disconnected = true; }
                            if (line[0] == (char)0x00)
                            {
                                //App message from the console
                                string[] command = line.Substring(1).Split((char)0x00);
                                switch (command[0].ToLower())
                                {
                                    case "autocomplete":
                                        if (command.Length > 1) { tabAutoCompleteBuffer.AddLast(command[1]); }
                                        else tabAutoCompleteBuffer.AddLast("");
                                        break;
                                }
                            }
                            else OutputBuffer.AddLast(line);
                        }
                    }
                }
                catch (NullReferenceException) { break; }
            }
        }

        /// <summary>
        /// Get the first queuing output line to print
        /// </summary>
        /// <returns></returns>

        public string ReadLine()
        {
            while (OutputBuffer.Count < 1) { }
            string line = OutputBuffer.First.Value;
            OutputBuffer.RemoveFirst();
            return line;
        }

        /// <summary>
        /// Complete a playername or a command, usually by pressing the TAB key
        /// </summary>
        /// <param name="text_behindcursor">Text to complete</param>
        /// <returns>Returns an autocompletion for the provided text</returns>

        public string tabAutoComplete(string text_behindcursor)
        {
            tabAutoCompleteBuffer.Clear();
            if (text_behindcursor != null && text_behindcursor.Trim().Length > 0)
            {
                text_behindcursor = text_behindcursor.Trim();
                SendText((char)0x00 + "autocomplete" + (char)0x00 + text_behindcursor);
                int maxwait = 30; while (tabAutoCompleteBuffer.Count < 1 && maxwait > 0) { Thread.Sleep(100); maxwait--; }
                if (tabAutoCompleteBuffer.Count > 0)
                {
                    string text_completed = tabAutoCompleteBuffer.First.Value;
                    tabAutoCompleteBuffer.RemoveFirst();
                    return text_completed;
                }
                else return text_behindcursor;
            }
            else return "";
        }

        /// <summary>
        /// Send a message or a command to the server
        /// </summary>
        /// <param name="text">Text to send</param>

        public void SendText(string text)
        {
            if (text != null)
            {
                text = text.Replace("\t", "");
                text = text.Replace("\r", "");
                text = text.Replace("\n", "");
                text = text.Trim();
                if (text.Length > 0)
                {
                    Client.StandardInput.WriteLine(text);
                }
            }
        }

        /// <summary>
        /// Properly disconnect from the server and dispose the client
        /// </summary>

        public void Close()
        {
            Client.StandardInput.WriteLine("/quit");
            if (Reader.IsAlive) { Reader.Abort(); }
            if (!Client.WaitForExit(3000))
            {
                try { Client.Kill(); } catch { }
            }
        }
    }
}
