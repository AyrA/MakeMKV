using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace MakeMKV
{
    class Program
    {
        /// <summary>
        /// Registry Key of MakeMKV
        /// </summary>
        public const string KEYNAME = @"HKEY_CURRENT_USER\Software\MakeMKV";
        /// <summary>
        /// Update Interval for Key
        /// </summary>
        public const int UPDATE_INTERVAL_H = 24;
        /// <summary>
        /// File Name of MakeMKV Executable
        /// </summary>
        public const string EXE = "MakeMKV.exe";

        static void Main(string[] args)
        {
#if DEBUG
            //Hardcode for debugging purposes
            var CurrentDir = $@"C:\Program Files (x86)\MakeMKV\{EXE}";
#else
            //Assume current directory (for portable installations)
            var CurrentDir = Path.Combine(Environment.CurrentDirectory, EXE);
            if (!File.Exists(CurrentDir))
            {
                //Assume application directory (if exe is placed in MakeMKV Folder)
                using (var P = Process.GetCurrentProcess())
                {
                    CurrentDir = Path.Combine(Path.GetDirectoryName(P.MainModule.FileName), EXE);
                }
                if (!File.Exists(CurrentDir))
                {
                    //Assume regular Installation directory, adapted for 64 bit OS if needed
                    CurrentDir = Path.Combine(Environment.GetFolderPath(Environment.Is64BitOperatingSystem ? Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles), "MakeMKV", EXE);
                }
            }
#endif
            //If executable was still not found. Give up.
            if (File.Exists(CurrentDir))
            {
                var KeyDate = InstalledKeyExpiration();
                //Only try to update the key if it actually expired
                if (KeyDate < DateTime.UtcNow || args.Any(m => m.ToLower() == "/force"))
                {
                    var K = GetKey();
                    if (!string.IsNullOrEmpty(K.key))
                    {
                        InstallKey(K);
                        Console.Error.WriteLine("Key updated: {0}", K.key);
                    }
                    else
                    {
                        Console.Error.WriteLine("Error obtaining Key from Internet");
                        WaitForKey();
                    }
                }
                ExecDir(CurrentDir);
            }
            else
            {
                Console.Error.WriteLine(@"Can't find {0}.
MakeMKV is neither installed in the regular Program files Directory
nor the directory of this updater. Please do either one of them", EXE);
                WaitForKey();
            }
        }

        /// <summary>
        /// Runs an application in it's own directory
        /// </summary>
        /// <param name="ExePath">Path to Executable</param>
        private static void ExecDir(string ExePath)
        {
            using (var P = new Process())
            {
                P.StartInfo.FileName = ExePath;
                P.StartInfo.WorkingDirectory = Path.GetDirectoryName(ExePath);
                P.Start();
            }
        }

        /// <summary>
        /// Gets the Date and Time when the currently used key will expire
        /// </summary>
        /// <returns>DateTime for KeyUpdate.</returns>
        private static DateTime InstalledKeyExpiration()
        {
            var Raw = Registry.GetValue(KEYNAME, "updater_KeyExpires", -1);
            var L = Raw == null ? -1 : (int)Raw;
            if (L == -1)
            {
                return DateTime.MinValue;
            }
            return new DateTime(L, DateTimeKind.Utc);
        }

        /// <summary>
        /// Gets the Date and Time of the last Key Update
        /// </summary>
        /// <returns>DateTime for KeyUpdate.</returns>
        private static DateTime LastKeyInstall()
        {
            var Raw = Registry.GetValue(KEYNAME, "updater_KeyCheck", -1);
            var L = Raw == null ? -1 : (int)Raw;
            if (L == -1)
            {
                return DateTime.MinValue;
            }
            return new DateTime(L, DateTimeKind.Utc);
        }

        /// <summary>
        /// Installs the current Key in the Registry
        /// </summary>
        /// <param name="K">Key details</param>
        private static void InstallKey(MakeMKV K)
        {
            Registry.SetValue(KEYNAME, "app_Key", K.key);
            Registry.SetValue(KEYNAME, "updater_KeyCheck", K.date, RegistryValueKind.DWord);
            Registry.SetValue(KEYNAME, "updater_KeyExpires", K.keydate, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Gets the current Application Version
        /// </summary>
        /// <returns>Application Version</returns>
        private static string GetVersion()
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

        /// <summary>
        /// Gets the current MakeMKV Key from the API
        /// </summary>
        /// <returns>MakeMKV Key (null on problems)</returns>
        private static MakeMKV GetKey()
        {
            HttpWebRequest WReq = WebRequest.CreateHttp("https://cable.ayra.ch/makemkv/api.php?xml");
            WebResponse WRes;
            //If you modify the tool, please add some personal twist to the user agent string
            WReq.UserAgent = string.Format("AyrA/MakeMKV-Updater-{0} ({1}/{2};{3}) +https://github.com/AyrA/MakeMKV",
                GetVersion(),
                Environment.OSVersion.Platform,
                Environment.OSVersion.Version,
                Environment.OSVersion.VersionString);
            try
            {
                WRes = WReq.GetResponse();
            }
            catch
            {
                return default(MakeMKV);
            }
            using (WRes)
            {
                using (var S = WRes.GetResponseStream())
                {
                    using (var SR = new StreamReader(S))
                    {
                        var Ser = new XmlSerializer(typeof(MakeMKV));
                        try
                        {
                            return (MakeMKV)Ser.Deserialize(SR);
                        }
                        catch
                        {
                            return default(MakeMKV);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Waits for a key press, discarding already queued keys
        /// </summary>
        /// <returns>Key pressed</returns>
        private static ConsoleKey WaitForKey()
        {
            Console.Error.WriteLine("Press any key to continue...");
            while (Console.KeyAvailable) { Console.ReadKey(true); }
            return Console.ReadKey(true).Key;
        }
    }
}
