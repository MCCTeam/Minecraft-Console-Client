using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Threading;
using System.ComponentModel;

namespace MinecraftClient
{
    /// <summary>
    /// C# Script runner - Compile on-the-fly and run C# scripts
    /// </summary>
    class CSharpRunner
    {
        private static readonly Dictionary<ulong, Assembly> CompileCache = new Dictionary<ulong, Assembly>();

        /// <summary>
        /// Run the specified C# script file
        /// </summary>
        /// <param name="apiHandler">ChatBot handler for accessing ChatBot API</param>
        /// <param name="lines">Lines of the script file to run</param>
        /// <param name="args">Arguments to pass to the script</param>
        /// <param name="localVars">Local variables passed along with the script</param>
        /// <param name="run">Set to false to compile and cache the script without launching it</param>
        /// <exception cref="CSharpException">Thrown if an error occured</exception>
        /// <returns>Result of the execution, returned by the script</returns>
        public static object Run(ChatBot apiHandler, string[] lines, string[] args, Dictionary<string, object> localVars, bool run = true)
        {
            //Script compatibility check for handling future versions differently
            if (lines.Length < 1 || lines[0] != "//MCCScript 1.0")
                throw new CSharpException(CSErrorType.InvalidScript,
                    new InvalidDataException(Translations.Get("exception.csrunner.invalid_head")));

            //Script hash for determining if it was previously compiled
            ulong scriptHash = QuickHash(lines);
            Assembly assembly = null;

            //No need to compile two scripts at the same time
            lock (CompileCache)
            {
                ///Process and compile script only if not already compiled
                if (!Settings.CacheScripts || !CompileCache.ContainsKey(scriptHash))
                {
                    //Process different sections of the script file
                    bool scriptMain = true;
                    List<string> script = new List<string>();
                    List<string> extensions = new List<string>();
                    List<string> libs = new List<string>();
                    List<string> dlls = new List<string>();
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("//using"))
                        {
                            libs.Add(line.Replace("//", "").Trim());
                        }
                        else if (line.StartsWith("//dll"))
                        {
                            dlls.Add(line.Replace("//dll ", "").Trim());
                        }
                        else if (line.StartsWith("//MCCScript"))
                        {
                            if (line.EndsWith("Extensions"))
                                scriptMain = false;
                        }
                        else if (scriptMain)
                            script.Add(line);
                        else extensions.Add(line);
                    }

                    //Add return statement if missing
                    if (script.All(line => !line.StartsWith("return ") && !line.Contains(" return ")))
                        script.Add("return null;");

                    //Generate a class from the given script
                    string code = String.Join("\n", new string[]
                    {
                        "using System;",
                        "using System.Collections.Generic;",
                        "using System.Text.RegularExpressions;",
                        "using System.Linq;",
                        "using System.Text;",
                        "using System.IO;",
                        "using System.Net;",
                        "using System.Threading;",
                        "using MinecraftClient;",
                        "using MinecraftClient.Mapping;",
                        "using MinecraftClient.Inventory;",
                        String.Join("\n", libs),
                        "namespace ScriptLoader {",
                        "public class Script {",
                        "public CSharpAPI MCC;",
                        "public object __run(CSharpAPI __apiHandler, string[] args) {",
                            "this.MCC = __apiHandler;",
                            String.Join("\n", script),
                        "}",
                            String.Join("\n", extensions),
                        "}}",
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
                    parameters.ReferencedAssemblies.AddRange(dlls.ToArray());
                    CompilerResults result = compiler.CompileAssemblyFromSource(parameters, code);

                    //Process compile warnings and errors
                    if (result.Errors.Count > 0)
                        throw new CSharpException(CSErrorType.LoadError,
                            new InvalidOperationException(result.Errors[0].ErrorText));

                    //Retrieve compiled assembly
                    assembly = result.CompiledAssembly;
                    if (Settings.CacheScripts)
                        CompileCache[scriptHash] = result.CompiledAssembly;
                }
                else if (Settings.CacheScripts)
                    assembly = CompileCache[scriptHash];
            }

            //Run the compiled assembly with exception handling
            if (run)
            {
                try
                {
                    object compiledScript = assembly.CreateInstance("ScriptLoader.Script");
                    return
                        compiledScript
                        .GetType()
                        .GetMethod("__run")
                        .Invoke(compiledScript,
                            new object[] { new CSharpAPI(apiHandler, localVars), args });
                }
                catch (Exception e) { throw new CSharpException(CSErrorType.RuntimeError, e); }
            }
            else return null;
        }

        /// <summary>
        /// Quickly calculate a hash for the given script
        /// </summary>
        /// <param name="lines">script lines</param>
        /// <returns>Quick hash as unsigned long</returns>
        private static ulong QuickHash(string[] lines)
        {
            ulong hashedValue = 3074457345618258791ul;
            for (int i = 0; i < lines.Length; i++)
            {
                for (int j = 0; j < lines[i].Length; j++)
                {
                    hashedValue += lines[i][j];
                    hashedValue *= 3074457345618258799ul;
                }
                hashedValue += '\n';
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }
    }

    /// <summary>
    /// Describe a C# script error type
    /// </summary>
    public enum CSErrorType { FileReadError, InvalidScript, LoadError, RuntimeError };

    /// <summary>
    /// Describe a C# script error with associated error type
    /// </summary>
    public class CSharpException : Exception
    {
        private CSErrorType _type;
        public CSErrorType ExceptionType { get { return _type; } }
        public override string Message { get { return InnerException.Message; } }
        public override string ToString() { return InnerException.ToString(); }
        public CSharpException(CSErrorType type, Exception inner)
            : base(inner != null ? inner.Message : "", inner)
        {
            _type = type;
        }
    }

    /// <summary>
    /// Represents the C# API object accessible from C# Scripts
    /// </summary>
    public class CSharpAPI : ChatBot
    {
        /// <summary>
        /// Holds local variables passed along with the script
        /// </summary>
        private Dictionary<string, object> localVars;

        /// <summary>
        /// Create a new C# API Wrapper
        /// </summary>
        /// <param name="apiHandler">ChatBot API Handler</param>
        /// <param name="tickHandler">ChatBot tick handler</param>
        /// <param name="localVars">Local variables passed along with the script</param>
        public CSharpAPI(ChatBot apiHandler, Dictionary<string , object> localVars)
        {
            SetMaster(apiHandler);
            this.localVars = localVars;
        }

        /* == Wrappers for ChatBot API with public visibility and call limit to one per tick for safety == */

        /// <summary>
        /// Write some text in the console. Nothing will be sent to the server.
        /// </summary>
        /// <param name="text">Log text to write</param>
        new public void LogToConsole(object text)
        {
            base.LogToConsole(text);
        }

        /// <summary>
        /// Send text to the server. Can be anything such as chat messages or commands
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        /// <returns>TRUE if successfully sent (Deprectated, always returns TRUE for compatibility purposes with existing scripts)</returns>
        public bool SendText(object text)
        {
            base.SendText(text is string ? (string)text : text.ToString());
            return true;
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command to process</param>
        /// <param name="localVars">Local variables passed along with the internal command</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>
        new public bool PerformInternalCommand(string command, Dictionary<string, object> localVars = null)
        {
            if (localVars == null)
                localVars = this.localVars;
            bool result = base.PerformInternalCommand(command, localVars);
            return result;
        }

        /// <summary>
        /// Disconnect from the server and restart the program
        /// It will unload and reload all the bots and then reconnect to the server
        /// </summary>
        /// <param name="extraAttempts">If connection fails, the client will make X extra attempts</param>
        /// <param name="delaySeconds">Optional delay, in seconds, before restarting</param>
        new public void ReconnectToTheServer(int extraAttempts = -999999, int delaySeconds = 0)
        {
            if (extraAttempts == -999999)
                base.ReconnectToTheServer();
            else base.ReconnectToTheServer(extraAttempts);
        }

        /// <summary>
        /// Disconnect from the server and exit the program
        /// </summary>
        new public void DisconnectAndExit()
        {
            base.DisconnectAndExit();
        }

        /// <summary>
        /// Load the provided ChatBot object
        /// </summary>
        /// <param name="bot">Bot to load</param>
        new public void LoadBot(ChatBot bot)
        {
            base.LoadBot(bot);
        }

        /// <summary>
        /// Return the list of currently online players
        /// </summary>
        /// <returns>List of online players</returns>
        new public string[] GetOnlinePlayers()
        {
            return base.GetOnlinePlayers();
        }

        /// <summary>
        /// Get a dictionary of online player names and their corresponding UUID
        /// </summary>
        /// <returns>
        ///     dictionary of online player whereby
        ///     UUID represents the key
        ///     playername represents the value</returns>
        new public Dictionary<string, string> GetOnlinePlayersWithUUID()
        {
            return base.GetOnlinePlayersWithUUID();
        }

        /* == Additional Methods useful for Script API == */

        /// <summary>
        /// Get a global variable by name
        /// </summary>
        /// <param name="varName">Name of the variable</param>
        /// <returns>Value of the variable or null if no variable</returns>
        public object GetVar(string varName)
        {
            if (localVars != null && localVars.ContainsKey(varName))
                return localVars[varName];
            return Settings.GetVar(varName);
        }

        /// <summary>
        /// Set a global variable for further use in any other script
        /// </summary>
        /// <param name="varName">Name of the variable</param>
        /// <param name="varValue">Value of the variable</param>
        public bool SetVar(string varName, object varValue)
        {
            if (localVars != null && localVars.ContainsKey(varName))
                localVars.Remove(varName);
            return Settings.SetVar(varName, varValue);
        }

        /// <summary>
        /// Get a global variable by name, as the specified type, and try converting it if possible.
        /// If you know what you are doing and just want a cast, use (T)MCC.GetVar("name") instead.
        /// </summary>
        /// <typeparam name="T">Variable type</typeparam>
        /// <param name="varName">Variable name</param>
        /// <returns>Variable as specified type or default value for this type</returns>
        public T GetVar<T>(string varName)
        {
            object value = GetVar(varName);
            if (value is T)
                return (T)value;
            if (value != null)
            {
                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                    if (converter != null)
                        return (T)converter.ConvertFromString(value.ToString());
                }
                catch (NotSupportedException) { /* Was worth trying */ }
            }
            return default(T);
        }

        //Named shortcuts for GetVar<type>(varname)
        public string GetVarAsString(string varName) { return GetVar<string>(varName); }
        public int GetVarAsInt(string varName) { return GetVar<int>(varName); }
        public double GetVarAsDouble(string varName) { return GetVar<double>(varName); }
        public bool GetVarAsBool(string varName) { return GetVar<bool>(varName); }

        /// <summary>
        /// Load login/password using an account alias and optionally reconnect to the server
        /// </summary>
        /// <param name="accountAlias">Account alias</param>
        /// <param name="andReconnect">Set to true to reconnecto to the server afterwards</param>
        /// <returns>True if the account was found and loaded</returns>
        public bool SetAccount(string accountAlias, bool andReconnect = false)
        {
            bool result = Settings.SetAccount(accountAlias);
            if (result && andReconnect)
                ReconnectToTheServer();
            return result;
        }

        /// <summary>
        /// Load new server information and optionally reconnect to the server
        /// </summary>
        /// <param name="server">"serverip:port" couple or server alias</param>
        /// <returns>True if the server IP was valid and loaded, false otherwise</returns>
        public bool SetServer(string server, bool andReconnect = false)
        {
            bool result = Settings.SetServerIP(server);
            if (result && andReconnect)
                ReconnectToTheServer();
            return result;
        }

        /// <summary>
        /// Synchronously call another script and retrieve the result
        /// </summary>
        /// <param name="script">Script to call</param>
        /// <param name="args">Arguments to pass to the script</param>
        /// <returns>An object returned by the script, or null</returns>
        public object CallScript(string script, string[] args)
        {
            string[] lines = null;
            ChatBots.Script.LookForScript(ref script);
            try { lines = File.ReadAllLines(script, Encoding.UTF8); }
            catch (Exception e) { throw new CSharpException(CSErrorType.FileReadError, e); }
            return CSharpRunner.Run(this, lines, args, localVars);
        }
    }
}
