using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace MinecraftClient.WinAPI
{
    /// <summary>
    /// Allow to set the player skin as console icon, on Windows only.
    /// See StackOverflow no. 2986853
    /// </summary>

    public static class ConsoleIcon
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleIcon(IntPtr hIcon);

        /// <summary>
        /// Asynchronously download the player's skin and set the head as console icon
        /// </summary>
        public enum WinMessages : uint
        {
            /// <summary>
            /// An application sends the WM_SETICON message to associate a new large or small icon with a window. 
            /// The system displays the large icon in the ALT+TAB dialog box, and the small icon in the window caption. 
            /// </summary>
            SETICON = 0x0080,
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);


        private static void SetWindowIcon(System.Drawing.Icon icon)
        {
            IntPtr mwHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            IntPtr result01 = SendMessage(mwHandle, (int)WinMessages.SETICON, 0, icon.Handle);
            IntPtr result02 = SendMessage(mwHandle, (int)WinMessages.SETICON, 1, icon.Handle);
        }
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleIcon(IntPtr hIcon);

        /// <summary>
        /// Asynchronously download the player's skin and set the head as console icon
        /// </summary>

        public static void setPlayerIconAsync(string playerName)
        {
            if (!Program.isUsingMono) //Windows Only
            {
                Thread t = new Thread(new ThreadStart(delegate
                {
                    HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create("http://skins.minecraft.net/MinecraftSkins/" + playerName + ".png");
                    try
                    {
                        using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
                        {
                            try
                            {
                                Bitmap skin = new Bitmap(Image.FromStream(httpWebReponse.GetResponseStream())); //Read skin from network
                                skin = skin.Clone(new Rectangle(8, 8, 8, 8), skin.PixelFormat); //Crop skin
                                SetWindowIcon(Icon.FromHandle(skin.GetHicon()));
                                SetConsoleIcon(skin.GetHicon()); //Set skin as icon
                            }
                            catch (ArgumentException) { /* Invalid image in HTTP response */ }
                        }
                    }
                    catch (WebException) //Skin not found? Reset to default icon
                    {
                        try
                        {
                            SetConsoleIcon(Icon.ExtractAssociatedIcon(Application.ExecutablePath).Handle);
                        }
                        catch { }
                    }
                }
                ));
                t.Name = "Player skin icon setter";
                t.Start();
            }
        }

        /// <summary>
        /// Set the icon back to the default CMD icon
        /// </summary>

        public static void revertToCMDIcon()
        {
            if (!Program.isUsingMono) //Windows Only
            {
                try
                {
                    SetConsoleIcon(Icon.ExtractAssociatedIcon(Environment.SystemDirectory + "\\cmd.exe").Handle);
                }
                catch { }
            }
        }
    }
}
