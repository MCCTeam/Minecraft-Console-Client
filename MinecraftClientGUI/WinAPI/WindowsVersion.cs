using Microsoft.Win32;

namespace MinecraftClient.WinAPI
{
    /// <summary>
    /// Retrieve information about the current Windows version
    /// </summary>
    /// <remarks>
    /// Environment.OSVersion does not work with Windows 10.
    /// It returns 6.2 which is Windows 8
    /// </remarks>
    /// <seealso>
    /// https://stackoverflow.com/a/37755503
    /// </seealso>
    class WindowsVersion
    {
        /// <summary>
        /// Returns the Windows major version number for this computer.
        /// </summary>
        public static uint WinMajorVersion
        {
            get
            {
                dynamic major;
                // The 'CurrentMajorVersionNumber' string value in the CurrentVersion key is new for Windows 10, 
                // and will most likely (hopefully) be there for some time before MS decides to change this - again...
                if (TryGetRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentMajorVersionNumber", out major))
                {
                    return (uint) major;
                }

                // When the 'CurrentMajorVersionNumber' value is not present we fallback to reading the previous key used for this: 'CurrentVersion'
                dynamic version;
                if (!TryGetRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentVersion", out version))
                    return 0;

                var versionParts = ((string) version).Split('.');
                if (versionParts.Length != 2) return 0;
                uint majorAsUInt;
                return uint.TryParse(versionParts[0], out majorAsUInt) ? majorAsUInt : 0;
            }
        }

        /// <summary>
        /// Returns the Windows minor version number for this computer.
        /// </summary>
        public static uint WinMinorVersion
        {
            get
            {
                dynamic minor;
                // The 'CurrentMinorVersionNumber' string value in the CurrentVersion key is new for Windows 10, 
                // and will most likely (hopefully) be there for some time before MS decides to change this - again...
                if (TryGetRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentMinorVersionNumber",
                    out minor))
                {
                    return (uint) minor;
                }

                // When the 'CurrentMinorVersionNumber' value is not present we fallback to reading the previous key used for this: 'CurrentVersion'
                dynamic version;
                if (!TryGetRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentVersion", out version))
                    return 0;

                var versionParts = ((string) version).Split('.');
                if (versionParts.Length != 2) return 0;
                uint minorAsUInt;
                return uint.TryParse(versionParts[1], out minorAsUInt) ? minorAsUInt : 0;
            }
        }

        /// <summary>
        /// Try retrieving a registry key
        /// </summary>
        /// <param name="path">key path</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value (output)</param>
        /// <returns>TRUE if successfully retrieved</returns>
        private static bool TryGetRegistryKey(string path, string key, out dynamic value)
        {
            value = null;
            try
            {
                var rk = Registry.LocalMachine.OpenSubKey(path);
                if (rk == null) return false;
                value = rk.GetValue(key);
                return value != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
