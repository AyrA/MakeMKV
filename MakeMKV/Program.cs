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
                var ForceUpdate = args.Any(m => m.ToLower() == "/force") || Settings.VersionChanged();
                var KeyDate = ForceUpdate ? DateTime.MinValue : Settings.InstalledKeyExpiration();
                //Only try to update the key if it actually expired
                if (KeyDate < DateTime.UtcNow)
                {
                    var K = GetKey();
                    if (!string.IsNullOrEmpty(K.key))
                    {
                        Settings.InstallKey(K);
                        Console.Error.WriteLine("Key updated: {0}", K.key);
                    }
                    else
                    {
                        Console.Error.WriteLine("Error obtaining Key from Internet");
                        WaitForKey();
                    }
                }
                else
                {
                    Console.Error.WriteLine("Using existing key. Expires: {0}", KeyDate.ToLocalTime());
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
        /// Gets the current MakeMKV Key from the API
        /// </summary>
        /// <returns>MakeMKV Key (null on problems)</returns>
        private static MakeMKV GetKey()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest WReq = WebRequest.CreateHttp("https://cable.ayra.ch/makemkv/api.php?xml");
            WebResponse WRes;
            //If you modify the tool, please add some personal twist to the user agent string
            WReq.UserAgent = string.Format("AyrA/MakeMKVUpdater-{0} ({1}/{2};{3}) +https://github.com/AyrA/MakeMKV",
                Settings.GetVersion(),
                Environment.OSVersion.Platform,
                Environment.OSVersion.Version,
                Environment.OSVersion.VersionString);
            try
            {
                WRes = WReq.GetResponse();
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
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
