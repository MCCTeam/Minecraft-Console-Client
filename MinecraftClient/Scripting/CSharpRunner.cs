using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using MinecraftClient.Scripting.DynamicRun.Builder;
using static MinecraftClient.Settings;

namespace MinecraftClient.Scripting
{
    /// <summary>
    /// C# Script runner - Compile on-the-fly and run C# scripts
    /// </summary>
    class CSharpRunner
    {
        private static readonly Dictionary<ulong, byte[]> CompileCache = new();

        private readonly record struct ScriptSourceLine(int LineNumber, string Text);

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
        public static object? Run(ChatBot apiHandler, string[] lines, string[] args, Dictionary<string, object>? localVars, bool run = true, string scriptName = "Unknown Script", string? scriptOwnerKey = null)
        {
            //Script compatibility check for handling future versions differently
            if (lines.Length < 1 || lines[0] != "//MCCScript 1.0")
                throw new CSharpException(CSErrorType.InvalidScript,
                    new InvalidDataException(Translations.exception_csrunner_invalid_head));

            //Script hash for determining if it was previously compiled
            ulong scriptHash = QuickHash(lines);
            byte[]? assembly = null;

            Compiler compiler = new();
            CompileRunner runner = new();

            //No need to compile two scripts at the same time
            lock (CompileCache)
            {
                ///Process and compile script only if not already compiled
                if (!Config.Main.Advanced.CacheScript || !CompileCache.ContainsKey(scriptHash))
                {
                    //Process different sections of the script file
                    bool scriptMain = true;
                    List<ScriptSourceLine> script = new();
                    List<ScriptSourceLine> extensions = new();
                    List<string> libs = new();
                    List<string> dlls = new();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
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

                        if (scriptMain)
                            script.Add(new(i + 1, line));
                        else extensions.Add(new(i + 1, line));
                    }

                    //Add return statement if missing
                    bool hasImplicitReturn = script.All(line => !line.Text.StartsWith("return ", StringComparison.Ordinal)
                        && !line.Text.Contains(" return ", StringComparison.Ordinal));

                    //Generate a class from the given script
                    string code = BuildScriptCode(scriptName, script, extensions, libs, hasImplicitReturn);

                    ConsoleIO.WriteLogLine(string.Format(Translations.script_compile_started, scriptName));

                    //Compile the C# class in memory using all the currently loaded assemblies
                    var result = compiler.Compile(code, Guid.NewGuid().ToString(), dlls);

                    //Process compile warnings and errors
                    if (result.Failures is not null)
                    {

                        ConsoleIO.WriteLogLine(Translations.script_compile_failed);

                        foreach (var failure in result.Failures)
                        {
                            ConsoleIO.WriteLogLine(FormatCompilationFailure(failure, scriptName));
                        }

                        throw new CSharpException(CSErrorType.InvalidScript, new InvalidProgramException("Compilation failed due to error(s)."));
                    }

                    ConsoleIO.WriteLogLine(Translations.script_compile_succeeded);

                    //Retrieve compiled assembly
                    assembly = result.Assembly;
                    if (Config.Main.Advanced.CacheScript)
                        CompileCache[scriptHash] = assembly!;
                }
                else if (Config.Main.Advanced.CacheScript)
                    assembly = CompileCache[scriptHash];
            }

            //Run the compiled assembly with exception handling
            if (run)
            {
                try
                {
                    var compiled = runner.Execute(assembly!, args, localVars, apiHandler, scriptOwnerKey);
                    return compiled;
                }
                catch (Exception e) { throw new CSharpException(CSErrorType.RuntimeError, e); }
            }
            else return null;
        }

        private static string BuildScriptCode(string scriptName, IEnumerable<ScriptSourceLine> script, IEnumerable<ScriptSourceLine> extensions, IEnumerable<string> libs, bool hasImplicitReturn)
        {
            StringBuilder codeBuilder = new();
            codeBuilder.AppendLine("using System;");
            codeBuilder.AppendLine("using System.Collections.Generic;");
            codeBuilder.AppendLine("using System.Text.RegularExpressions;");
            codeBuilder.AppendLine("using System.Linq;");
            codeBuilder.AppendLine("using System.Text;");
            codeBuilder.AppendLine("using System.IO;");
            codeBuilder.AppendLine("using System.Net;");
            codeBuilder.AppendLine("using System.Threading;");
            codeBuilder.AppendLine("using MinecraftClient;");
            codeBuilder.AppendLine("using MinecraftClient.Scripting;");
            codeBuilder.AppendLine("using MinecraftClient.Mapping;");
            codeBuilder.AppendLine("using MinecraftClient.Inventory;");

            foreach (string lib in libs)
                codeBuilder.AppendLine(lib);

            codeBuilder.AppendLine("namespace ScriptLoader {");
            codeBuilder.AppendLine("public class Script {");
            codeBuilder.AppendLine("public CSharpAPI MCC;");
            codeBuilder.AppendLine("public object __run(CSharpAPI __apiHandler, string[] args) {");
            codeBuilder.AppendLine("this.MCC = __apiHandler;");
            AppendMappedSection(codeBuilder, scriptName, script);

            if (hasImplicitReturn)
                codeBuilder.AppendLine("return null;");

            codeBuilder.AppendLine("}");
            AppendMappedSection(codeBuilder, scriptName, extensions);
            codeBuilder.AppendLine("}}");
            return codeBuilder.ToString();
        }

        private static void AppendMappedSection(StringBuilder codeBuilder, string scriptName, IEnumerable<ScriptSourceLine> lines)
        {
            ScriptSourceLine[] sourceLines = lines.ToArray();
            if (sourceLines.Length == 0)
                return;

            codeBuilder.AppendLine($@"#line {sourceLines[0].LineNumber} ""{EscapeLineDirectivePath(scriptName)}""");

            foreach (ScriptSourceLine line in sourceLines)
                codeBuilder.AppendLine(line.Text);

            codeBuilder.AppendLine("#line default");
        }

        private static string EscapeLineDirectivePath(string path)
        {
            return path.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        }

        private static string FormatCompilationFailure(Diagnostic failure, string scriptName)
        {
            if (failure.Location.IsInSource)
            {
                var location = failure.Location.GetMappedLineSpan();
                string sourcePath = string.IsNullOrWhiteSpace(location.Path) ? scriptName : location.Path;
                return string.Format(Translations.script_compile_error,
                    sourcePath,
                    location.StartLinePosition.Line + 1,
                    location.StartLinePosition.Character + 1,
                    failure.Id,
                    failure.GetMessage());
            }

            return string.Format(Translations.script_compile_error_no_location, scriptName, failure.Id, failure.GetMessage());
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
        private readonly CSErrorType _type;
        public CSErrorType ExceptionType { get { return _type; } }
        public override string Message { get { return InnerException!.Message; } }
        public override string ToString() { return InnerException!.ToString(); }
        public CSharpException(CSErrorType type, Exception inner) : base(inner.Message, inner)
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
        private readonly Dictionary<string, object>? localVars;

        /// <summary>
        /// Create a new C# API Wrapper
        /// </summary>
        /// <param name="apiHandler">ChatBot API Handler</param>
        /// <param name="tickHandler">ChatBot tick handler</param>
        /// <param name="localVars">Local variables passed along with the script</param>
        public CSharpAPI(ChatBot apiHandler, Dictionary<string, object>? localVars, string? scriptOwnerKey = null)
        {
            SetMaster(apiHandler);
            this.localVars = localVars;
            SetScriptOwnerKey(scriptOwnerKey);
        }

        /// <summary>
        /// Access the shared MCC gameplay API used by bots and the embedded MCP server.
        /// </summary>
        new public MccGameApi Game => base.Game;

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
            return base.SendText(text is string str ? str : text.ToString() ?? string.Empty);
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command to process</param>
        /// <param name="localVars">Local variables passed along with the internal command</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>
        new public bool PerformInternalCommand(string command, Dictionary<string, object>? localVars = null)
        {
            localVars ??= this.localVars;
            return base.PerformInternalCommand(command, localVars);
        }

        /// <summary>
        /// Disconnect from the server and restart the program
        /// It will unload and reload all the bots and then reconnect to the server
        /// </summary>
        /// <param name="extraAttempts">If connection fails, the client will make X extra attempts</param>
        /// <param name="delaySeconds">Optional delay, in seconds, before restarting</param>
        new public void ReconnectToTheServer(int extraAttempts = -999999, int delaySeconds = 0, bool keepAccountAndServerSettings = false)
        {
            if (extraAttempts == -999999)
                base.ReconnectToTheServer(delaySeconds: delaySeconds, keepAccountAndServerSettings: keepAccountAndServerSettings);
            else
                base.ReconnectToTheServer(extraAttempts, delaySeconds, keepAccountAndServerSettings);
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
        public object? GetVar(string varName)
        {
            if (localVars is not null && localVars.ContainsKey(varName))
                return localVars[varName];
            else
                return Config.AppVar.GetVar(varName);
        }

        /// <summary>
        /// Set a global variable for further use in any other script
        /// </summary>
        /// <param name="varName">Name of the variable</param>
        /// <param name="varValue">Value of the variable</param>
        public bool SetVar(string varName, object varValue)
        {
            if (localVars is not null && localVars.ContainsKey(varName))
                localVars.Remove(varName);
            return Config.AppVar.SetVar(varName, varValue);
        }

        /// <summary>
        /// Get a global variable by name, as the specified type, and try converting it if possible.
        /// If you know what you are doing and just want a cast, use (T)MCC.GetVar("name") instead.
        /// </summary>
        /// <typeparam name="T">Variable type</typeparam>
        /// <param name="varName">Variable name</param>
        /// <returns>Variable as specified type or default value for this type</returns>
        public T? GetVar<T>(string varName)
        {
            object? value = GetVar(varName);
            if (value is T Tval)
                return Tval;
            if (value is not null)
            {
                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                    if (converter is not null)
                        return (T?)converter.ConvertFromString(value.ToString() ?? string.Empty);
                }
                catch (NotSupportedException) { /* Was worth trying */ }
            }
            return default;
        }

        //Named shortcuts for GetVar<type>(varname)
        public string? GetVarAsString(string varName) { return GetVar<string>(varName); }
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
            bool result = Config.Main.Advanced.SetAccount(accountAlias);
            if (result && andReconnect)
                ReconnectToTheServer(keepAccountAndServerSettings: true);
            return result;
        }

        /// <summary>
        /// Load new server information and optionally reconnect to the server
        /// </summary>
        /// <param name="server">"serverip:port" couple or server alias</param>
        /// <returns>True if the server IP was valid and loaded, false otherwise</returns>
        public bool SetServer(string server, bool andReconnect = false)
        {
            bool result = Config.Main.SetServerIP(new MainConfigHelper.MainConfig.ServerInfoConfig(server), true);
            if (result && andReconnect)
                ReconnectToTheServer(keepAccountAndServerSettings: true);
            return result;
        }

        /// <summary>
        /// Synchronously call another script and retrieve the result
        /// </summary>
        /// <param name="script">Script to call</param>
        /// <param name="args">Arguments to pass to the script</param>
        /// <returns>An object returned by the script, or null</returns>
        public object? CallScript(string script, string[] args)
        {
            ChatBots.Script.LookForScript(ref script);
            string[] lines;
            try
            {
                lines = File.ReadAllLines(script, Encoding.UTF8);
            }
            catch (Exception e)
            {
                throw new CSharpException(CSErrorType.FileReadError, e);
            }
            return CSharpRunner.Run(this, lines, args, localVars, scriptName: script);
        }
    }
}
