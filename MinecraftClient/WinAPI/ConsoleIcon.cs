using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

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

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        /// <summary>
        /// An application sends the WM_SETICON message to associate a new large or small icon with a window.
        /// The system displays the large icon in the ALT+TAB dialog box, and the small icon in the window caption.
        /// </summary>
        public enum WinMessages : uint
        {
            SETICON = 0x0080,
        }

        private static void SetWindowIcon(System.Drawing.Icon icon)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr mwHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                SendMessage(mwHandle, (int)WinMessages.SETICON, 0, icon.Handle);
                SendMessage(mwHandle, (int)WinMessages.SETICON, 1, icon.Handle);
            }
        }

        /// <summary>
        /// Asynchronously download the player's skin and set the head as console icon
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static void SetPlayerIconAsync(string playerName)
        {
            Thread t = new(new ThreadStart(delegate
            {
                HttpClient httpClient = new();
                try
                {
                    Task<Stream> httpWebRequest = httpClient.GetStreamAsync("https://minotar.net/helm/" + playerName + "/100.png");
                    httpWebRequest.Wait();
                    Stream imageStream = httpWebRequest.Result;
                    try
                    {
                        Bitmap skin = new(Image.FromStream(imageStream)); //Read skin from network
                        SetWindowIcon(Icon.FromHandle(skin.GetHicon())); // Windows 10+ (New console)
                        SetConsoleIcon(skin.GetHicon()); // Windows 8 and lower (Older console)
                    }
                    catch (ArgumentException)
                    {
                        /* Invalid image in HTTP response */
                    }
                    imageStream.Dispose();
                    httpWebRequest.Dispose();
                }
                catch (AggregateException ae)
                {
                    bool needRevert = false;
                    foreach (var ex in ae.InnerExceptions)
                    {
                        if (ex is HttpRequestException || ex is TaskCanceledException) //Skin not found? Reset to default icon
                            needRevert = true;
                    }
                    if (needRevert)
                        RevertToMCCIcon();
                }
                catch (HttpRequestException) //Skin not found? Reset to default icon
                {
                    RevertToMCCIcon();
                }
                finally
                {
                    httpClient.Dispose();
                }
            }
            ))
            {
                Name = "Player skin icon setter"
            };
            t.Start();
        }

        /// <summary>
        /// Set the icon back to the default MCC icon
        /// </summary>
        public static void RevertToMCCIcon()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) //Windows Only
            {
                try
                {
                    Icon defaultIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!)!;
                    SetWindowIcon(Icon.FromHandle(defaultIcon.Handle)); // Windows 10+ (New console)
                    SetConsoleIcon(defaultIcon.Handle); // Windows 8 and lower (Older console)
                }
                catch { }
            }
        }
    }
}
