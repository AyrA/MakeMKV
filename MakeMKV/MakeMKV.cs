using System;

namespace MakeMKV
{
    [Serializable]
    public struct MakeMKV
    {
        public const string UNIX_EPOCH = "1970-01-01T00:00:00Z";

        /// <summary>
        /// Current UTC date
        /// </summary>
        public int date;
        /// <summary>
        /// First moment in time the key is considered expired
        /// </summary>
        public int keydate;
        /// <summary>
        /// Earliest point in time to ask for a key again
        /// </summary>
        public int cache;
        /// <summary>
        /// MakeMKV Key
        /// </summary>
        public string key;

        public static int ToUnixTime(DateTime T)
        {
            return (int)T.ToUniversalTime().Subtract(DateTime.Parse(UNIX_EPOCH).ToUniversalTime()).TotalSeconds;
        }

        public static DateTime FromUnixTime(int UnixTime)
        {
            return DateTime.Parse(UNIX_EPOCH).ToUniversalTime().AddSeconds(UnixTime);
        }
    }
}
