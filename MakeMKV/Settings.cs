using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace MakeMKV
{
    /// <summary>
    /// Handles Registry and application specifics
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Registry Key of MakeMKV
        /// </summary>
        public const string KEYNAME = @"HKEY_CURRENT_USER\Software\MakeMKV";

        /// <summary>
        /// Gets the Date and Time when the currently used key will expire
        /// </summary>
        /// <returns>DateTime for KeyUpdate.</returns>
        public static DateTime InstalledKeyExpiration()
        {
            var Raw = Registry.GetValue(KEYNAME, "updater_KeyExpires", -1);
            var UnixTime = Raw == null ? -1 : (int)Raw;
            if (UnixTime == -1)
            {
                return DateTime.MinValue;
            }
            return MakeMKV.FromUnixTime(UnixTime);
        }

        /// <summary>
        /// Gets the Date and Time of the last Key Update
        /// </summary>
        /// <returns>DateTime for KeyUpdate.</returns>
        public static DateTime LastKeyInstall()
        {
            var Raw = Registry.GetValue(KEYNAME, "updater_KeyCheck", -1);
            var UnixTime = Raw == null ? -1 : (int)Raw;
            if (UnixTime == -1)
            {
                return DateTime.MinValue;
            }
            return MakeMKV.FromUnixTime(UnixTime);
        }

        /// <summary>
        /// Installs the current Key in the Registry
        /// </summary>
        /// <param name="K">Key details</param>
        public static void InstallKey(MakeMKV K)
        {
            Registry.SetValue(KEYNAME, "app_Key", K.key);
            Registry.SetValue(KEYNAME, "updater_Version", GetVersion());
            Registry.SetValue(KEYNAME, "updater_KeyCheck", K.date, RegistryValueKind.DWord);
            Registry.SetValue(KEYNAME, "updater_KeyExpires", K.keydate, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Checks if the version has changed since the last run
        /// </summary>
        /// <returns>true if changed</returns>
        public static bool VersionChanged()
        {
            var Raw = Registry.GetValue(KEYNAME, "updater_Version", string.Empty);
            var Version = Raw == null ? string.Empty : (string)Raw;
            return Version != GetVersion();
        }

        /// <summary>
        /// Gets the current Application Version
        /// </summary>
        /// <returns>Application Version</returns>
        public static string GetVersion()
        {
            using (var P = Process.GetCurrentProcess())
            {
                try
                {
                    return P.MainModule.FileVersionInfo.FileVersion;
                }
                catch
                {
                    return "Unknown";
                }
            }
        }
    }
}
