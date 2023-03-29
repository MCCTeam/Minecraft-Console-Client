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

namespace MinecraftClient
{
    internal static class UpgradeHelper
    {
        private const string GithubReleaseUrl = "https://github.com/MCCTeam/Minecraft-Console-Client/releases";

        private static int running = 0; // Type: bool; 1 for running; 0 for stopped;
        private static CancellationTokenSource cancellationTokenSource = new();
        private static CancellationToken cancellationToken = CancellationToken.None;

        private static long lastBytesTransferred = 0, minNotifyThreshold = 5 * 1024 * 1024;
        private static DateTime downloadStartTime = DateTime.Now, lastNotifyTime = DateTime.Now;
        private static TimeSpan minNotifyInterval = TimeSpan.FromMilliseconds(3000);

        public static void CheckUpdate(bool forceUpdate = false)
        {
            bool needPromptUpdate = true;
            if (!forceUpdate && CompareVersionInfo(Settings.Config.Head.CurrentVersion, Settings.Config.Head.LatestVersion))
            {
                needPromptUpdate = false;
                ConsoleIO.WriteLineFormatted("§e" + string.Format(Translations.mcc_has_update, GithubReleaseUrl), true);
            }
            Task.Run(() =>
            {
                DoCheckUpdate(CancellationToken.None);
                if (needPromptUpdate)
                {
                    if (CompareVersionInfo(Settings.Config.Head.CurrentVersion, Settings.Config.Head.LatestVersion))
                    {
                        ConsoleIO.WriteLineFormatted("§e" + string.Format(Translations.mcc_has_update, GithubReleaseUrl), true);
                    }
                    else if (forceUpdate)
                    {
                        ConsoleIO.WriteLine(Translations.mcc_update_already_latest + ' ' + Translations.mcc_update_promote_force_cmd);
                    }
                }
            });
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
                        string latestVersion = DoCheckUpdate(cancellationToken);
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
                            AddProxySettings(httpClientHandler);

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
                                string downloadUrl = $"{GithubReleaseUrl}/download/{latestVersion}/";
                                string fileNameWithoutExtension = $"MinecraftClient-{latestVersion}-{OSInfo}";
                                string fileName = fileNameWithoutExtension + GetExecutableFileExtension();
                                downloadStartTime = DateTime.Now;
                                lastNotifyTime = DateTime.MinValue;
                                lastBytesTransferred = 0;

                                bool hasFile = true;
                                HttpResponseMessage response = await httpClient.GetAsync(downloadUrl + fileName, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                                if (!response.IsSuccessStatusCode)
                                {
                                    fileName = fileNameWithoutExtension + ".zip";
                                    response = await httpClient.GetAsync(downloadUrl + fileName, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                                    if (!response.IsSuccessStatusCode)
                                    {
                                        hasFile = false;
                                        ConsoleIO.WriteLine($"{Translations.mcc_update_download_fail} File {fileNameWithoutExtension}{GetExecutableFileExtension()} not found.");
                                    }
                                }

                                if (hasFile)
                                {
                                    using (FileStream fileStream = File.Create(fileName))
                                    {
                                        await response.Content.CopyToAsync(fileStream);
                                    }

                                    ConsoleIO.WriteLineFormatted("§e" + string.Format(Translations.mcc_update_save_as, fileName), true);

                                    if (!OperatingSystem.IsWindows())
                                        File.SetUnixFileMode(fileName, UnixFileMode.UserRead
                                                                       | UnixFileMode.UserWrite
                                                                       | UnixFileMode.UserExecute
                                                                       | UnixFileMode.GroupRead
                                                                       | UnixFileMode.GroupExecute
                                                                       | UnixFileMode.OtherRead
                                                                       | UnixFileMode.OtherExecute);
                                }

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

        private static string DoCheckUpdate(CancellationToken cancellationToken)
        {
            string latestBuildInfo = string.Empty;
            HttpClientHandler httpClientHandler = new() { AllowAutoRedirect = false };
            AddProxySettings(httpClientHandler);
            HttpClient httpClient = new(httpClientHandler);
            Task<HttpResponseMessage>? httpWebRequest = null;
            try
            {
                httpWebRequest = httpClient.GetAsync(GithubReleaseUrl + "/latest", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                httpWebRequest.Wait();
                if (!cancellationToken.IsCancellationRequested)
                {
                    HttpResponseMessage res = httpWebRequest.Result;
                    if (res.Headers.Location != null)
                    {
                        Match match = Regex.Match(res.Headers.Location.ToString(), GithubReleaseUrl + @"/tag/(\d{4})(\d{2})(\d{2})-(\d+)");
                        if (match.Success && match.Groups.Count == 5)
                        {
                            string year = match.Groups[1].Value, month = match.Groups[2].Value, day = match.Groups[3].Value, run = match.Groups[4].Value;
                            string latestVersion = string.Format("GitHub build {0}, built on {1}-{2}-{3}", run, year, month, day);
                            latestBuildInfo = string.Format("{0}{1}{2}-{3}", year, month, day, run);
                            if (latestVersion != Settings.Config.Head.LatestVersion)
                            {
                                Settings.Config.Head.LatestVersion = latestVersion;
                                Program.WriteBackSettings(false);
                            }
                        }
                    }
                    res.Dispose();
                }
                httpWebRequest.Dispose();
            }
            catch (Exception) { }
            finally { httpWebRequest?.Dispose(); }
            httpClient.Dispose();
            httpClientHandler.Dispose();
            return latestBuildInfo;
        }

        private static string GetOSIdentifier()
        {
            string OSPlatformName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                OSPlatformName = "win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                OSPlatformName = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                OSPlatformName = "osx";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                OSPlatformName = "freebsd";
            else
                return string.Empty;

            string architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm => "arm32",
                Architecture.Arm64 => "arm64",
                Architecture.Wasm => "wasm",
                Architecture.S390x => "s390x",
                Architecture.LoongArch64 => "loongarch64",
                Architecture.Armv6 => "armv6",
                Architecture.Ppc64le => "ppc64le",
                _ => RuntimeInformation.ProcessArchitecture.ToString(),
            };

            return OSPlatformName + '-' + architecture;
        }

        private static string GetExecutableFileExtension()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ".exe";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return string.Empty;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return string.Empty;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                return string.Empty;
            else
                return string.Empty;
        }

        private static bool CompareVersionInfo(string? current, string? latest)
        {
            if (current == null || latest == null)
                return false;
            Regex reg = new(@"\w+\sbuild\s(\d+),\sbuilt\son\s(\d{4})[-\/\.\s]?(\d{2})[-\/\.\s]?(\d{2}).*");
            Regex reg2 = new(@"\w+\sbuild\s(\d+),\sbuilt\son\s\w+\s(\d{2})[-\/\.\s]?(\d{2})[-\/\.\s]?(\d{4}).*");

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

        private static void AddProxySettings(HttpClientHandler httpClientHandler)
        {
            if (Settings.Config.Proxy.Enabled_Update)
            {
                string proxyAddress;
                if (!string.IsNullOrWhiteSpace(Settings.Config.Proxy.Username) && !string.IsNullOrWhiteSpace(Settings.Config.Proxy.Password))
                    proxyAddress = string.Format("{0}://{3}:{4}@{1}:{2}",
                        Settings.Config.Proxy.Proxy_Type.ToString().ToLower(),
                        Settings.Config.Proxy.Server.Host,
                        Settings.Config.Proxy.Server.Port,
                        Settings.Config.Proxy.Username,
                        Settings.Config.Proxy.Password);
                else
                    proxyAddress = string.Format("{0}://{1}:{2}",
                        Settings.Config.Proxy.Proxy_Type.ToString().ToLower(),
                        Settings.Config.Proxy.Server.Host, Settings.Config.Proxy.Server.Port);
                httpClientHandler.Proxy = new WebProxy(proxyAddress, true);
            }
        }
    }
}
