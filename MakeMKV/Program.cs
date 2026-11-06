using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
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

        private static readonly HttpClient HttpClient = new HttpClient();

        static async Task Main(string[] args)
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
                var ForceUpdate = args.Any(m => string.Equals(m, "/force", StringComparison.OrdinalIgnoreCase)) || Settings.VersionChanged();
                var KeyDate = ForceUpdate ? DateTime.MinValue : Settings.InstalledKeyExpiration();
                //Only try to update the key if it actually expired
                if (KeyDate < DateTime.UtcNow)
                {
                    var K = await GetKeyAsync();
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
        private static async Task<MakeMKV> GetKeyAsync()
        {
            try
            {
                using (var Request = new HttpRequestMessage(HttpMethod.Get, "https://cable.ayra.ch/makemkv/api.php?xml"))
                {
                    //If you modify the tool, please add some personal twist to the user agent string
                    Request.Headers.UserAgent.ParseAdd(string.Format("AyrA/MakeMKVUpdater-{0} ({1}/{2};{3}; +https://github.com/AyrA/MakeMKV)",
                        Settings.GetVersion(),
                        Environment.OSVersion.Platform,
                        Environment.OSVersion.Version,
                        Environment.OSVersion.VersionString));

                    using (var Response = await HttpClient.SendAsync(Request))
                    {
                        Response.EnsureSuccessStatusCode();
                        using (var Stream = await Response.Content.ReadAsStreamAsync())
                        using (var Reader = XmlReader.Create(Stream, new XmlReaderSettings
                        {
                            DtdProcessing = DtdProcessing.Prohibit,
                            XmlResolver = null
                        }))
                        {
                            var Serializer = new XmlSerializer(typeof(MakeMKV));
                            return (MakeMKV)Serializer.Deserialize(Reader);
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Error requesting key: {0}", ex.Message);
                return default(MakeMKV);
            }
            catch (TaskCanceledException ex)
            {
                Console.Error.WriteLine("Key request timed out: {0}", ex.Message);
                return default(MakeMKV);
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine("Error parsing key response: {0}", ex.Message);
                return default(MakeMKV);
            }
            catch (XmlException ex)
            {
                Console.Error.WriteLine("Error parsing key response: {0}", ex.Message);
                return default(MakeMKV);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine("Error reading key response: {0}", ex.Message);
                return default(MakeMKV);
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
