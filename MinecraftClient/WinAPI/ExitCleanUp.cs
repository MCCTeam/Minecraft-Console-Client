using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MinecraftClient.WinAPI
{
    /// <summary>
    /// Perform clean up before quitting application
    /// </summary>
    /// <remarks>
    /// Only ctrl+c/ctrl+break will be captured when running on mono
    /// </remarks>
    public static class ExitCleanUp
    {
        /// <summary>
        /// Store codes to run before quitting
        /// </summary>
        private static List<Action> actions = new List<Action>();

        static ExitCleanUp()
        {
            try
            {
                // Capture all close event
                _handler += CleanUp;
                // Use delegate directly cause program to crash
                SetConsoleCtrlHandler(_handler, true);
            }
            catch (DllNotFoundException)
            {
                // Probably on mono, fallback to ctrl+c only
                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
                {
                    RunCleanUp();
                };
            }
        }

        /// <summary>
        /// Add a new action to be performed before application exit
        /// </summary>
        /// <param name="cleanUpCode">Action to run</param>
        public static void Add(Action cleanUpCode)
        {
            actions.Add(cleanUpCode);
        }

        /// <summary>
        /// Run all actions
        /// </summary>
        /// <remarks>
        /// For .Net native
        /// </remarks>
        private static void RunCleanUp()
        {
            foreach (Action action in actions)
            {
                action();
            }
        }

        /// <summary>
        /// Run all actions
        /// </summary>
        /// <param name="sig"></param>
        /// <returns></returns>
        /// <remarks>
        /// For win32 API
        /// </remarks>
        private static bool CleanUp(CtrlType sig)
        {
            foreach (Action action in actions)
            {
                action();
            }
            return false;
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);
        private delegate bool ConsoleCtrlHandler(CtrlType sig);
        private static ConsoleCtrlHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
    }
}
