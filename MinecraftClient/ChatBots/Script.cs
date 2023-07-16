using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Runs a list of commands
    /// </summary>

    public class Script : ChatBot
    {
        private string? file;
        private string[] lines = Array.Empty<string>();
        private string[] args = Array.Empty<string>();
        private int sleepticks = 10;
        private int nextline = 0;
        private readonly string? owner;
        private bool csharp;
        private Thread? thread;
        private readonly Dictionary<string, object>? localVars;

        public Script(string filename)
        {
            ParseArguments(filename);
        }

        public Script(string filename, string? ownername, Dictionary<string, object>? localVars)
            : this(filename)
        {
            owner = ownername;
            this.localVars = localVars;
        }

        private void ParseArguments(string argstr)
        {
            List<string> args = new();
            StringBuilder str = new();

            bool escape = false;
            bool quotes = false;

            foreach (char c in argstr)
            {
                if (escape)
                {
                    if (c != '"')
                        str.Append('\\');
                    str.Append(c);
                    escape = false;
                }
                else
                {
                    if (c == '\\')
                        escape = true;
                    else if (c == '"')
                        quotes = !quotes;
                    else if (c == ' ' && !quotes)
                    {
                        if (str.Length > 0)
                            args.Add(str.ToString());
                        str.Clear();
                    }
                    else str.Append(c);
                }
            }

            if (str.Length > 0)
                args.Add(str.ToString());

            if (args.Count > 0)
            {
                file = args[0];
                args.RemoveAt(0);
                this.args = args.ToArray();
            }
            else file = "";
        }

        public static bool LookForScript(ref string filename)
        {
            //Automatically look in subfolders and try to add ".txt" file extension
             char dir_slash = Path.DirectorySeparatorChar;
            string[] files = new string[]
            {
                filename,
                filename + ".txt",
                filename + ".cs",
                "scripts" + dir_slash + filename,
                "scripts" + dir_slash + filename + ".txt",
                "scripts" + dir_slash + filename + ".cs",
                "config" + dir_slash + filename,
                "config" + dir_slash + filename + ".txt",
                "config" + dir_slash + filename + ".cs",
            };

            foreach (string possible_file in files)
            {
                if (System.IO.File.Exists(possible_file))
                {
                    filename = possible_file;
                    return true;
                }
            }

            if (Settings.Config.Logging.DebugMessages)
            {
                string caller = "Script";
                try
                {
                    StackFrame frame = new(1);
                    MethodBase method = frame.GetMethod()!;
                    Type type = method.DeclaringType!;
                    caller = type.Name;
                }
                catch { }
                ConsoleIO.WriteLineFormatted(string.Format(Translations.bot_script_not_found, caller, filename));
            }

            return false;
        }

        public override void Initialize()
        {
            //Load the given file from the startup parameters
            if (LookForScript(ref file!))
            {
                lines = System.IO.File.ReadAllLines(file, Encoding.UTF8);
                csharp = file.EndsWith(".cs");
                thread = null;

                if (!String.IsNullOrEmpty(owner))
                    SendPrivateMessage(owner, string.Format(Translations.bot_script_pm_loaded, file));
            }
            else
            {
                LogToConsole(string.Format(Translations.bot_script_file_not_found, System.IO.Path.GetFullPath(file)));

                if (!String.IsNullOrEmpty(owner))
                    SendPrivateMessage(owner, string.Format(Translations.bot_script_file_not_found, file));

                UnloadBot(); //No need to keep the bot active
            }
        }

        public override void Update()
        {
            if (csharp) //C# compiled script
            {
                //Initialize thread on first update
                if (thread == null)
                {
                    thread = new Thread(() =>
                    {
                        try
                        {
                            CSharpRunner.Run(this, lines, args, localVars, scriptName: file!);
                        }
                        catch (CSharpException e)
                        {
                            string errorMessage = string.Format(Translations.bot_script_fail, file, e.ExceptionType);
                            LogToConsole(errorMessage);
                            if (owner != null)
                                SendPrivateMessage(owner, errorMessage);
                            LogToConsole(e.InnerException);
                        }
                    })
                    {
                        Name = "MCC Script - " + file
                    };
                    thread.Start();
                }

                //Unload bot once the thread has finished running
                if (thread != null && !thread.IsAlive)
                {
                    UnloadBot();
                }
            }
            else //Classic MCC script interpreter
            {
                if (sleepticks > 0) { sleepticks--; }
                else
                {
                    if (nextline < lines.Length) //Is there an instruction left to interpret?
                    {
                        string instruction_line = lines[nextline].Trim(); // Removes all whitespaces at start and end of current line
                        nextline++; //Move the cursor so that the next time the following line will be interpreted

                        if (instruction_line.Length > 1)
                        {
                            if (instruction_line[0] != '#' && instruction_line[0] != '/' && instruction_line[1] != '/')
                            {
                                instruction_line = Settings.Config.AppVar.ExpandVars(instruction_line, localVars);
                                string instruction_name = instruction_line.Split(' ')[0];
                                switch (instruction_name.ToLower())
                                {
                                    case "wait":
                                        int ticks = 10;
                                        try
                                        {
                                            if (instruction_line[5..].Contains("to", StringComparison.OrdinalIgnoreCase) ||
                                                instruction_line[5..].Contains("-"))
                                            {
                                                var processedLine = instruction_line.Replace("wait", "")
                                                    .Trim()
                                                    .ToLower();
                                                processedLine = string.Join("", processedLine.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                                                var parts = processedLine.Contains("to") ? processedLine.Split("to") : processedLine.Split("-");
                                                
                                                if (parts.Length == 2)
                                                {
                                                    var min = Convert.ToInt32(parts[0]);
                                                    var max = Convert.ToInt32(parts[1]);

                                                    if (min > max)
                                                    {
                                                        (min, max) = (max, min);
                                                        LogToConsole(Translations.cmd_wait_random_min_bigger);
                                                    }
                                                    
                                                    ticks = new Random().Next(min, max);
                                                } else ticks = Convert.ToInt32(instruction_line[5..]);
                                            } else ticks = Convert.ToInt32(instruction_line[5..]);
                                        }
                                        catch { }
                                        sleepticks = ticks;
                                        break;
                                    default:
                                        CmdResult response = new();
                                        if (PerformInternalCommand(instruction_line, ref response))
                                        {
                                            if (instruction_name.ToLower() != "log")
                                            {
                                                LogToConsole(instruction_line);
                                            }
                                            if (response.status != CmdResult.Status.Done || !string.IsNullOrWhiteSpace(response.result))
                                            {
                                                LogToConsole(response);
                                            }
                                        }
                                        else
                                        {
                                            Update(); //Unknown command : process next line immediately
                                        }
                                        break;
                                }
                            }
                            else { Update(); } //Comment: process next line immediately
                        }
                    }
                    else
                    {
                        //No more instructions to interpret
                        UnloadBot();
                    }
                }
            }
        }
    }
}
