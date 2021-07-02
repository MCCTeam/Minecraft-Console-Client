using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net;
using System.IO;
using System.Drawing;

namespace MinecraftClient.WinAPI
{
    /// <summary>
    /// Allow to set the player skin as console icon, on Windows only.
    /// See StackOverflow no. 2986853
    /// </summary>
    public static class ConsoleIcon
    {
        /// <summary>
        /// An application sends the WM_SETICON message to associate a new large or small icon with a window.
        /// The system displays the large icon in the ALT+TAB dialog box, and the small icon in the window caption.
        /// </summary>
        public enum WinMessages : uint
        {
            SETICON = 0x0080,
        }

        private static void SetWindowIcon(System.Drawing.Icon icon) {
            if (Settings.DebugMessages) {
                Console.WriteLine("ConsoleIcon.SetWindowIcon Called");
            }
            return;
            //IntPtr mwHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            //IntPtr result01 = SendMessage(mwHandle, (int)WinMessages.SETICON, 0, icon.Handle);
            //IntPtr result02 = SendMessage(mwHandle, (int)WinMessages.SETICON, 1, icon.Handle);
        }

        /// <summary>
        /// Asynchronously download the player's skin and set the head as console icon
        /// </summary>
        public static void setPlayerIconAsync(string playerName) {
            if (Settings.DebugMessages) {
                Console.WriteLine("ConsoleIcon.SetPlayerIconAsync Called");
            }

            return;
            
            if (!Program.isUsingMono) //Windows Only
            {
                Thread t = new Thread(new ThreadStart(delegate {
                        HttpWebRequest httpWebRequest =
                            (HttpWebRequest) HttpWebRequest.Create(
                                "https://minotar.net/helm/" + playerName + "/100.png");
                        try {
                            using (HttpWebResponse httpWebReponse = (HttpWebResponse) httpWebRequest.GetResponse()) {
                                try {
                                    Bitmap skin =
                                        new Bitmap(Image.FromStream(httpWebReponse
                                            .GetResponseStream())); //Read skin from network
                                    SetWindowIcon(Icon.FromHandle(skin.GetHicon())); // Windows 10+ (New console)
                                    // SetConsoleIcon(skin.GetHicon()); // Windows 8 and lower (Older console)
                                }
                                catch (ArgumentException) {
                                    /* Invalid image in HTTP response */
                                }
                            }
                        }
                        catch (WebException) //Skin not found? Reset to default icon
                        {
                            revertToMCCIcon();
                        }
                    }
                ));
                t.Name = "Player skin icon setter";
                t.Start();

            }
        }

        /// <summary>
        /// Set the icon back to the default MCC icon
        /// </summary>
        public static void revertToMCCIcon()
        {
            if (Settings.DebugMessages) {
                Console.WriteLine("ConsoleIcon.revertToMCCIcon Called");
            }
            return;
           /*
            if (!Program.isUsingMono) //Windows Only
            {
                try
                {
                    //Icon defaultIcon = Icon.ExtractAssociatedIcon(Environment.SystemDirectory + "\\cmd.exe");
                    Icon defaultIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    SetWindowIcon(Icon.FromHandle(defaultIcon.Handle)); // Windows 10+ (New console)
                    SetConsoleIcon(defaultIcon.Handle); // Windows 8 and lower (Older console)
                }
                catch { }
            }
            */
        }
    }
}
