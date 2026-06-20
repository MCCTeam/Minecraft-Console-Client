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

        public MinecraftClient(string[] args)
        {
            initClient("\"" + String.Join("\" \"", args) + "\" BasicIO");
        }

        public MinecraftClient(string username, string password, string serverip)
        {
            // If the password is empty, pass an empty string to support Microsoft/Browser login
            if (password == null) password = "";
            initClient('"' + username + "\" \"" + password + "\" \"" + serverip + "\" BasicIO");
        }

        private void initClient(string arguments)
        {
            if (File.Exists(ExePath))
            {
                Client = new Process();
                Client.StartInfo.FileName = ExePath;
                Client.StartInfo.Arguments = arguments;
                Client.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                // FIX: Forcing UTF-8 fixes Polish characters and colors
                Client.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                Client.StartInfo.UseShellExecute = false;
                Client.StartInfo.RedirectStandardOutput = true;
                Client.StartInfo.RedirectStandardInput = true;
                Client.StartInfo.CreateNoWindow = true;
                Client.Start();

                Reader = new Thread(new ThreadStart(t_reader));
                Reader.Start();
            }
            else throw new FileNotFoundException("Nie znaleziono pliku MinecraftClient.exe!", ExePath);
        }

        private void t_reader()
        {
            while (true)
            {
                try
                {
                    if (Client.HasExited) { disconnected = true; break; }

                    string line = Client.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        if (line.Trim() != "")
                        {
                            if (line.Contains("Server was successfuly joined")) { disconnected = false; }
                            if (line.Contains("You have left the server")) { disconnected = true; }

                            if (line.Length > 0 && line[0] == (char)0x00)
                            {
                                string[] command = line.Substring(1).Split((char)0x00);
                                if (command[0].ToLower() == "autocomplete")
                                {
                                    if (command.Length > 1) { tabAutoCompleteBuffer.AddLast(command[1]); }
                                    else tabAutoCompleteBuffer.AddLast("");
                                }
                            }
                            else
                            {
                                OutputBuffer.AddLast(line);
                            }
                        }
                    }
                    else { Thread.Sleep(10); } // Small pause to avoid overloading the CPU
                }
                catch (Exception) { break; }
            }
        }

        public string ReadLine()
        {
            while (OutputBuffer.Count < 1)
            {
                if (disconnected) return "";
                Thread.Sleep(10); // Save CPU while waiting for data
            }
            string line = OutputBuffer.First.Value;
            OutputBuffer.RemoveFirst();
            return line;
        }

        public string tabAutoComplete(string text_behindcursor)
        {
            tabAutoCompleteBuffer.Clear();
            if (text_behindcursor != null && text_behindcursor.Trim().Length > 0)
            {
                text_behindcursor = text_behindcursor.Trim();
                SendText((char)0x00 + "autocomplete" + (char)0x00 + text_behindcursor);
                int maxwait = 30;
                while (tabAutoCompleteBuffer.Count < 1 && maxwait > 0)
                {
                    Thread.Sleep(100);
                    maxwait--;
                }
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

        public void SendText(string text)
        {
            if (text != null && !Client.HasExited)
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

        public void Close()
        {
            if (!Client.HasExited)
            {
                Client.StandardInput.WriteLine("/quit");
                if (Reader.IsAlive) { Reader.Abort(); }
                if (!Client.WaitForExit(2000))
                {
                    try { Client.Kill(); } catch { }
                }
            }
        }
    }
}
