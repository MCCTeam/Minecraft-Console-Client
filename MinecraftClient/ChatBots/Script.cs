using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

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
        private int nextline = 0;
        private string owner;
        private bool csharp;
        private Thread thread;
        private ManualResetEvent tpause;

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
            char dir_slash = Program.isUsingMono ? '/' : '\\';
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

            return false;
        }

        public override void Initialize()
        {
            //Load the given file from the startup parameters
            if (lookForScript(ref file))
            {
                lines = System.IO.File.ReadAllLines(file);
                csharp = file.EndsWith(".cs");
                thread = null;

                if (owner != null)
                    SendPrivateMessage(owner, "Script '" + file + "' loaded.");
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
            if (csharp) //C# compiled script
            {
                //Initialize thread on first update
                if (thread == null)
                {
                    tpause = new ManualResetEvent(false);
                    thread = new Thread(() =>
                    {
                        if (!RunCSharpScript(String.Join("\n", lines), file, tpause) && owner != null)
                            SendPrivateMessage(owner, "Script '" + file + "' failed to run.");
                    });
                    thread.Start();
                }

                //Let the thread run for a short span of time
                if (thread != null)
                {
                    tpause.Set();
                    tpause.Reset();
                    if (thread.Join(100))
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
                                instruction_line = Settings.ExpandVars(instruction_line);
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
                                        if (!PerformInternalCommand(instruction_line))
                                        {
                                            Update(); //Unknown command : process next line immediately
                                        }
                                        else if (instruction_name.ToLower() != "log") { LogToConsole(instruction_line); }
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

        private bool RunCSharpScript(string script, string filename = "C# Script", ManualResetEvent tpause = null)
        {
            //Script compatibility check for handling future versions differently
            if (!script.ToLower().StartsWith("//mccscript 1.0"))
            {
                ConsoleIO.WriteLineFormatted("§8Script file '" + filename + "' does not start with a valid //MCCScript comment.");
                return false;
            }

            //Create a simple ChatBot class from the given script, allowing access to ChatBot API
            string code = String.Join("\n", new string[]
            {
                "using System;",
                "using System.IO;",
                "using System.Threading;",
                "using MinecraftClient;",
                "namespace ScriptLoader {",
                "public class Script : ChatBot {",
                "public void Run(ChatBot master, ManualResetEvent tpause) {",
                "SetMaster(master);",
                    tpause != null
                        ? script.Replace(";\n", ";\ntpause.WaitOne();\n")
                        : script,
                "}}}",
            });

            //Compile the C# class in memory using all the currently loaded assemblies
            CSharpCodeProvider compiler = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.ReferencedAssemblies
                .AddRange(AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .Select(a => a.Location).ToArray());
            parameters.CompilerOptions = "/t:library";
            parameters.GenerateInMemory = true;
            CompilerResults result
                = compiler.CompileAssemblyFromSource(parameters, code);

            //Process compile warnings and errors
            if (result.Errors.Count > 0)
            {
                ConsoleIO.WriteLineFormatted("§8Error loading '" + filename + "':\n" + result.Errors[0].ErrorText);
                return false;
            }

            //Run the compiled script with exception handling
            object compiledScript = result.CompiledAssembly.CreateInstance("ScriptLoader.Script");
            try { compiledScript.GetType().GetMethod("Run").Invoke(compiledScript, new object[] { this, tpause }); }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§8Runtime error for '" + filename + "':\n" + e);
                return false;
            }

            return true;
        }
    }
}
