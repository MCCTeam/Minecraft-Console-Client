using System;
using System.IO;
using System.Threading;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Debug-only ChatBot that monitors a text file for commands.
    /// Write lines to the file from any external tool (e.g. Cursor Shell)
    /// and this bot will execute them as MCC internal commands.
    ///
    /// Usage from Cursor Shell:
    ///   Add-Content mcc_input.txt "inventory"
    ///   Add-Content mcc_input.txt "send /give @s diamond_sword 1"
    ///
    /// Lines starting with "/" are sent as server chat; others are treated
    /// as MCC internal commands (same as typing in the MCC console).
    /// </summary>
    public class FileInputBot : ChatBot
    {
        private const string BotName = "FileInput";
        private string _filePath = string.Empty;
        private long _lastPosition;
        private int _tickCounter;

        public override void Initialize()
        {
            _filePath = Path.GetFullPath(
                Environment.GetEnvironmentVariable("MCC_INPUT_FILE") ?? "mcc_input.txt");

            if (File.Exists(_filePath))
                _lastPosition = new FileInfo(_filePath).Length;
            else
                File.WriteAllText(_filePath, "");

            LogToConsole(BotName, $"Watching: {_filePath}");
            LogToConsole(BotName, "Write commands to this file to execute them.");
        }

        public override void Update()
        {
            // Poll every ~500ms while the MCC main loop runs at 20 TPS.
            if (++_tickCounter < Settings.DoubleToTick(0.5))
                return;
            _tickCounter = 0;

            try
            {
                if (!File.Exists(_filePath))
                    return;

                var info = new FileInfo(_filePath);
                if (info.Length <= _lastPosition)
                    return;

                string newContent;
                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(_lastPosition, SeekOrigin.Begin);
                    using var reader = new StreamReader(fs);
                    newContent = reader.ReadToEnd();
                }
                _lastPosition = info.Length;

                foreach (var rawLine in newContent.Split('\n'))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    LogToConsole(BotName, $"> {line}");

                    if (line.StartsWith("/"))
                    {
                        SendText(line);
                    }
                    else
                    {
                        CmdResult result = new();
                        if (PerformInternalCommand(line, ref result))
                        {
                            if (!string.IsNullOrEmpty(result.ToString()))
                                LogToConsole(BotName, result.ToString());
                        }
                        else
                        {
                            // Not an internal command — send as chat
                            SendText(line);
                        }
                    }
                }
            }
            catch (IOException)
            {
                // File may be temporarily locked by the writer
            }
            catch (Exception ex)
            {
                LogToConsole(BotName, $"Error: {ex.Message}");
            }
        }
    }
}
