using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Proxy;

namespace MinecraftClient
{
    internal static partial class UpgradeHelper
    {
        internal const string GithubReleaseUrl = "https://github.com/MCCTeam/Minecraft-Console-Client/releases";

        // private static HttpClient? httpClient = null;

        private static int running = 0; // Type: bool; 1 for running; 0 for stopped;
        private static CancellationTokenSource cancellationTokenSource = new();
        private static CancellationToken cancellationToken = CancellationToken.None;

        private static long lastBytesTransferred = 0, minNotifyThreshold = 5 * 1024 * 1024;
        private static DateTime downloadStartTime = DateTime.Now, lastNotifyTime = DateTime.Now;
        private static TimeSpan minNotifyInterval = TimeSpan.FromMilliseconds(3000);

        public static async Task CheckUpdate(bool forceUpdate = false)
        {
            await DoCheckUpdate(CancellationToken.None);
            if (CompareVersionInfo(Settings.Config.Head.CurrentVersion, Settings.Config.Head.LatestVersion))
            {
                ConsoleIO.WriteLineFormatted("§e" + string.Format(Translations.mcc_has_update, GithubReleaseUrl), true);
            }
            else if (forceUpdate)
            {
                ConsoleIO.WriteLine(Translations.mcc_update_already_latest + ' ' + Translations.mcc_update_promote_force_cmd);
            }
        }

        public static bool DownloadLatestBuild(bool forceUpdate, bool isCommandLine = false)
        {
            if (Interlocked.Exchange(ref running, 1) == 1)
            {
                return false;
            }
            else
            {
                if (!cancellationTokenSource.TryReset())
                    cancellationTokenSource = new();
                cancellationToken = cancellationTokenSource.Token;
                Task.Run(async () =>
                {
                    string OSInfo = GetOSIdentifier();
                    if (Settings.Config.Logging.DebugMessages || string.IsNullOrEmpty(OSInfo))
                        ConsoleIO.WriteLine(string.Format("OS: {0}, Arch: {1}, Framework: {2}",
                            RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture, RuntimeInformation.FrameworkDescription));
                    if (string.IsNullOrEmpty(OSInfo))
                    {
                        ConsoleIO.WriteLine(Translations.mcc_update_platform_not_support);
                    }
                    else
                    {
                        string latestVersion = await DoCheckUpdate(cancellationToken);
                        if (cancellationToken.IsCancellationRequested)
                        {
                        }
                        else if (string.IsNullOrEmpty(latestVersion))
                        {
                            ConsoleIO.WriteLine(Translations.mcc_update_check_fail);
                        }
                        else if (!forceUpdate && !CompareVersionInfo(Settings.Config.Head.CurrentVersion, Settings.Config.Head.LatestVersion))
                        {
                            ConsoleIO.WriteLine(Translations.mcc_update_already_latest + ' ' +
                                (isCommandLine ? Translations.mcc_update_promote_force_cmd : Translations.mcc_update_promote_force));
                        }
                        else
                        {
                            ConsoleIO.WriteLine(string.Format(Translations.mcc_update_exist_update, latestVersion, OSInfo));

                            HttpClientHandler httpClientHandler = new() { AllowAutoRedirect = true };
                            ProxyHandler.AddProxySettings(ProxyHandler.ClientType.Update, ref httpClientHandler);

                            ProgressMessageHandler progressMessageHandler = new(httpClientHandler);
                            progressMessageHandler.HttpReceiveProgress += (_, info) =>
                            {
                                DateTime now = DateTime.Now;
                                if (now - lastNotifyTime > minNotifyInterval || info.BytesTransferred - lastBytesTransferred > minNotifyThreshold)
                                {
                                    lastNotifyTime = now;
                                    lastBytesTransferred = info.BytesTransferred;
                                    if (info.TotalBytes.HasValue)
                                    {
                                        ConsoleIO.WriteLine(string.Format(Translations.mcc_update_progress,
                                            (double)info.BytesTransferred / info.TotalBytes * 100.0,
                                            (double)info.BytesTransferred / 1024 / 1024,
                                            (double)info.TotalBytes / 1024 / 1024,
                                            (double)info.BytesTransferred / 1024 / (now - downloadStartTime).TotalSeconds,
                                            TimeSpan.FromMilliseconds(
                                                (double)(info.TotalBytes - info.BytesTransferred) / (info.BytesTransferred / (now - downloadStartTime).TotalMilliseconds)
                                                ).ToString("hh\\:mm\\:ss"))
                                        );
                                    }
                                    else
                                    {
                                        ConsoleIO.WriteLine(string.Format(Translations.mcc_update_progress_type2,
                                            (double)info.BytesTransferred / 1024 / 1024,
                                            (double)info.BytesTransferred / 1024 / (now - downloadStartTime).TotalSeconds)
                                        );
                                    }
                                }
                            };

                            HttpClient httpClient = new(progressMessageHandler);

                            try
                            {
                                string downloadUrl = $"{GithubReleaseUrl}/download/{latestVersion}/MinecraftClient-{OSInfo}.zip";
                                downloadStartTime = DateTime.Now;
                                lastNotifyTime = DateTime.MinValue;
                                lastBytesTransferred = 0;
                                using HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                                using Stream zipFileStream = await response.Content.ReadAsStreamAsync(cancellationToken);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    using ZipArchive zipArchive = new(zipFileStream, ZipArchiveMode.Read);
                                    ConsoleIO.WriteLine(Translations.mcc_update_download_complete);
                                    foreach (var archiveEntry in zipArchive.Entries)
                                    {
                                        if (archiveEntry.Name.StartsWith("MinecraftClient"))
                                        {
                                            string fileName = $"MinecraftClient-{latestVersion}{Path.GetExtension(archiveEntry.Name)}";
                                            archiveEntry.ExtractToFile(fileName, true);
                                            ConsoleIO.WriteLineFormatted("§e" + string.Format(Translations.mcc_update_save_as, fileName), true);
                                            break;
                                        }
                                    }
                                    zipArchive.Dispose();
                                }

                                zipFileStream.Dispose();
                                response.Dispose();
                            }
                            catch (TaskCanceledException) { }
                            catch (Exception e)
                            {
                                ConsoleIO.WriteLine($"{Translations.mcc_update_download_fail}\n{e.GetType().Name}: {e.Message}\n{e.StackTrace}");
                            }

                            httpClient.Dispose();
                            progressMessageHandler.Dispose();
                            httpClientHandler.Dispose();
                        }
                    }
                    Interlocked.Exchange(ref running, 0);
                }, cancellationToken);
                return true;
            }
        }

        public static void CancelDownloadUpdate()
        {
            if (!cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();
        }

        public static void HandleBlockingUpdate(bool forceUpgrade)
        {
            minNotifyInterval = TimeSpan.FromMilliseconds(500);
            DownloadLatestBuild(forceUpgrade, true);
            while (running == 1)
                Thread.Sleep(500);
        }

        internal static async Task<string> DoCheckUpdate(CancellationToken cancellationToken)
        {
            string latestBuildInfo = string.Empty;

            HttpClientHandler httpClientHandler = new() { AllowAutoRedirect = false };
            ProxyHandler.AddProxySettings(ProxyHandler.ClientType.Update, ref httpClientHandler);
            using HttpClient httpClient = new(httpClientHandler);
            using HttpRequestMessage request = new(HttpMethod.Head, GithubReleaseUrl + "/latest");
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                if (response.Headers.Location != null)
                {
                    Match match = GetReleaseUrlRegex().Match(response.Headers.Location.ToString());
                    if (match.Success && match.Groups.Count == 5)
                    {
                        string year = match.Groups[1].Value, month = match.Groups[2].Value, day = match.Groups[3].Value, run = match.Groups[4].Value;
                        string latestVersion = string.Format("GitHub build {0}, built on {1}-{2}-{3}", run, year, month, day);
                        latestBuildInfo = string.Format("{0}{1}{2}-{3}", year, month, day, run);
                        if (latestVersion != Settings.Config.Head.LatestVersion)
                        {
                            Settings.Config.Head.LatestVersion = latestVersion;
                            _ = Program.WriteBackSettings(false);
                        }
                    }
                }
            }
            return latestBuildInfo;
        }

        private static string GetOSIdentifier()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    return "linux-arm64";
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    return "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    return "osx";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    return "windows-x64";
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                    return "windows-x86";
            }
            return string.Empty;
        }

        internal static bool CompareVersionInfo(string? current, string? latest)
        {
            if (current == null || latest == null)
                return false;
            Regex reg = GetVersionRegex1();
            Regex reg2 = GetVersionRegex2();

            DateTime? curTime = null, latestTime = null;

            Match curMatch = reg.Match(current);
            if (curMatch.Success && curMatch.Groups.Count == 5)
            {
                try { curTime = new(int.Parse(curMatch.Groups[2].Value), int.Parse(curMatch.Groups[3].Value), int.Parse(curMatch.Groups[4].Value)); }
                catch { curTime = null; }
            }
            if (curTime == null)
            {
                curMatch = reg2.Match(current);
                try { curTime = new(int.Parse(curMatch.Groups[4].Value), int.Parse(curMatch.Groups[3].Value), int.Parse(curMatch.Groups[2].Value)); }
                catch { curTime = null; }
            }
            if (curTime == null)
                return false;

            Match latestMatch = reg.Match(latest);
            if (latestMatch.Success && latestMatch.Groups.Count == 5)
            {
                try { latestTime = new(int.Parse(latestMatch.Groups[2].Value), int.Parse(latestMatch.Groups[3].Value), int.Parse(latestMatch.Groups[4].Value)); }
                catch { latestTime = null; }
            }
            if (latestTime == null)
            {
                latestMatch = reg2.Match(latest);
                try { latestTime = new(int.Parse(latestMatch.Groups[4].Value), int.Parse(latestMatch.Groups[3].Value), int.Parse(latestMatch.Groups[2].Value)); }
                catch { latestTime = null; }
            }
            if (latestTime == null)
                return false;

            int curBuildId, latestBuildId;
            try
            {
                curBuildId = int.Parse(curMatch.Groups[1].Value);
                latestBuildId = int.Parse(latestMatch.Groups[1].Value);
            }
            catch { return false; }

            if (latestTime > curTime)
                return true;
            else if (latestTime >= curTime && latestBuildId > curBuildId)
                return true;
            else
                return false;
        }

        [GeneratedRegex("https://github.com/MCCTeam/Minecraft-Console-Client/releases/tag/(\\d{4})(\\d{2})(\\d{2})-(\\d+)")]
        private static partial Regex GetReleaseUrlRegex();

        [GeneratedRegex("\\w+\\sbuild\\s(\\d+),\\sbuilt\\son\\s(\\d{4})[-\\/\\.\\s]?(\\d{2})[-\\/\\.\\s]?(\\d{2}).*")]
        private static partial Regex GetVersionRegex1();

        [GeneratedRegex("\\w+\\sbuild\\s(\\d+),\\sbuilt\\son\\s\\w+\\s(\\d{2})[-\\/\\.\\s]?(\\d{2})[-\\/\\.\\s]?(\\d{4}).*")]
        private static partial Regex GetVersionRegex2();
    }
}
