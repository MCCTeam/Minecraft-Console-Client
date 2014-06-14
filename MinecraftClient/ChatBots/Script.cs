using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Runs a list of commands
    /// </summary>

    public class Script : ChatBot
    {
        private string file;
        private string[] lines = new string[0];
        private int sleepticks = 10;
        private int sleepticks_interval = 10;
        private int nextline = 0;
        private string owner;

        public Script(string filename)
        {
            file = filename;
        }

        public Script(string filename, string ownername)
            : this(filename)
        {
            if (ownername != "")
                owner = ownername;
        }

        public static bool lookForScript(ref string filename)
        {
            //Automatically look in subfolders and try to add ".txt" file extension
            string[] files = new string[]
                {
                    filename,
                    filename + ".txt",
                    "scripts\\" + filename,
                    "scripts\\" + filename + ".txt",
                    "config\\" + filename,
                    "config\\" + filename + ".txt",
                };

            foreach (string possible_file in files)
            {
                if (System.IO.File.Exists(possible_file))
                {
                    filename = possible_file;
                    return true;
                }
            }

            return false;
        }

        public override void Initialize()
        {
            //Load the given file from the startup parameters
            if (lookForScript(ref file))
            {
                lines = System.IO.File.ReadAllLines(file);
                if (owner != null) { SendPrivateMessage(owner, "Script '" + file + "' loaded."); }
            }
            else
            {
                LogToConsole("File not found: '" + file + "'");
                if (owner != null)
                    SendPrivateMessage(owner, "File not found: '" + file + "'");
                UnloadBot(); //No need to keep the bot active
            }
        }

        public override void Update()
        {
            if (sleepticks > 0) { sleepticks--; }
            else
            {
                if (nextline < lines.Length) //Is there an instruction left to interpret?
                {
                    string instruction_line = lines[nextline].Trim(); // Removes all whitespaces at start and end of current line
                    nextline++; //Move the cursor so that the next time the following line will be interpreted
                    sleepticks = sleepticks_interval; //Used to delay next command sending and prevent from beign kicked for spamming

                    if (instruction_line.Length > 1)
                    {
                        if (instruction_line[0] != '#' && instruction_line[0] != '/' && instruction_line[1] != '/')
                        {
                            string instruction_name = instruction_line.Split(' ')[0];
                            switch (instruction_name.ToLower())
                            {
                                case "wait":
                                    int ticks = 10;
                                    try
                                    {
                                        ticks = Convert.ToInt32(instruction_line.Substring(5, instruction_line.Length - 5));
                                    }
                                    catch { }
                                    sleepticks = ticks;
                                    break;
                                default:
                                    if (isInternalCommand(instruction_line))
                                    {
                                        performInternalCommand(instruction_line);
                                    }
                                    else sleepticks = 0; Update(); //Unknown command : process next line immediately
                                    break;
                            }
                        }
                        else { sleepticks = 0; Update(); } //Comment: process next line immediately
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
