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
    [SupportedOSPlatform("windows")]
    public static class ConsoleIcon
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

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
            using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            IntPtr mwHandle = currentProcess.MainWindowHandle;
            SendMessage(mwHandle, (int)WinMessages.SETICON, 0, icon.Handle);
            SendMessage(mwHandle, (int)WinMessages.SETICON, 1, icon.Handle);
        }

        /// <summary>
        /// Asynchronously download the player's skin and set the head as console icon
        /// </summary>
        public static void SetPlayerIconAsync(string playerName)
        {
            Thread t = new(new ThreadStart(delegate
            {
                using HttpClient httpClient = new();
                try
                {
                    using Task<Stream> httpWebRequest = httpClient.GetStreamAsync("https://minotar.net/helm/" + playerName + "/100.png");
                    httpWebRequest.Wait();
                    using Stream imageStream = httpWebRequest.Result;
                    try
                    {
                        using Image loadedImage = Image.FromStream(imageStream);
                        using Bitmap skin = new(loadedImage); //Read skin from network
                        IntPtr hIcon = skin.GetHicon();
                        try
                        {
                            using Icon icon = Icon.FromHandle(hIcon);
                            SetWindowIcon(icon); // Windows 10+ (New console)
                            SetConsoleIcon(hIcon); // Windows 8 and lower (Older console)
                        }
                        finally
                        {
                            DestroyIcon(hIcon);
                        }
                    }
                    catch (ArgumentException)
                    {
                        /* Invalid image in HTTP response */
                    }
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
            try
            {
                using Icon? defaultIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
                if (defaultIcon is not null)
                {
                    SetWindowIcon(defaultIcon); // Windows 10+ (New console)
                    SetConsoleIcon(defaultIcon.Handle); // Windows 8 and lower (Older console)
                }
            }
            catch { }
        }
    }
}
