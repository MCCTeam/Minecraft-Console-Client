using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net;
using System.IO;
using System.Drawing;

namespace MinecraftClient
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
            Thread t = new Thread(new ThreadStart(delegate
            {
                if (!Program.isUsingMono) //Windows Only
                {
                    HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create("http://skins.minecraft.net/MinecraftSkins/" + playerName + ".png");

                    try
                    {
                        using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
                        {
                            Bitmap skin = new Bitmap(Image.FromStream(httpWebReponse.GetResponseStream())); //Read skin from network
                            skin = skin.Clone(new Rectangle(8, 8, 8, 8), skin.PixelFormat); //Crop skin
                            SetConsoleIcon(skin.GetHicon()); //Set skin as icon
                        }
                    }
                    catch (WebException) { } //Skin not found
                }
            }
            ));
            t.Name = "Player skin icon setter";
            t.Start();
        }
    }
}
