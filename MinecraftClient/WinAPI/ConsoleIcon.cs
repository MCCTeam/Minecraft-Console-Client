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
