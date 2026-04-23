using MegaCrit.Sts2.Core.Logging;

namespace Fog_of_war
{
    public class Logger
    {
        public Logger(string modId)
        {
            Log = new(modId, LogType.Generic);
        }

        public static MegaCrit.Sts2.Core.Logging.Logger Log { get; set; }

        public void LogWithTimestamp(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log.Warn($"[{timestamp}]: {message}");
        }
    }
}
