using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NLog;

namespace TrayPingTest
{
    class CloudLogging
    {

        private static Logger log = null;

        /// <summary>
        /// Add a log entry to the local cache to be uploaded later
        /// </summary>
        /// <param name="LogMessage">The string of the log message to upload</param>
        /// <param name="LogLevel">The level (1/Debug 2/Info 3/Warn 4/Error 5/Fatal)</param>
        /// <returns></returns>
        public static bool AddLogEntry(string LogMessage, int LogLevel)
        {

            string fileName = string.Format("traypingtest_{1}_{0}.log", Guid.NewGuid(), LogLevel);
            string filePath = Path.Combine(Path.GetTempPath(), fileName);

            Dictionary<int, string> levels = new Dictionary<int, string>();
            levels.Add(1, "Debug");
            levels.Add(2, "Info");
            levels.Add(3, "Warn");
            levels.Add(4, "Error");
            levels.Add(5, "Fatal");

            // NLog.Targets.Target target = LogManager.Configuration.FindTargetByName("logentries");
            // ${date:format=ddd MMM dd} ${time:format=HH:mm:ss} ${date:format=zzz yyyy} ${logger} : ${LEVEL} :

            string timestampedMessage = string.Format("{0} {1} {2} {3} : {4} : {5}", DateTime.Now.ToString("ddd MMM dd"), DateTime.Now.ToString("HH:mm:ss"), DateTime.Now.ToString("zzz yyyy"), "TrayPingTest.CloudLogging", levels[LogLevel], LogMessage);

            File.WriteAllText(filePath, timestampedMessage);

            return true;
        }

        /// <summary>
        /// Attempt to upload all the log entries
        /// </summary>
        public static void UploadLogEntries()
        {

            if (!Properties.Settings.Default.EnableCloudLogging) return;

            // If not initialized, create it here
            // Set the LOGENTRIES_TOKEN appsetting to the Properties.Settings.Default value so NLog can read it
            if (log == null)
            {
                var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
                config.AppSettings.Settings["LOGENTRIES_TOKEN"].Value = Properties.Settings.Default.LogEntriesAPIKey;
                config.Save(System.Configuration.ConfigurationSaveMode.Modified, true);
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");

                log = LogManager.GetCurrentClassLogger();

            }

            string[] logfiles = Directory.GetFiles(Path.GetTempPath(), "traypingtest_*");

            foreach(string file in logfiles)
            {
                string message = File.ReadAllText(file);
                string[] parts = Path.GetFileNameWithoutExtension(file).Split('_');

                try
                {

                    switch (int.Parse(parts[1]))
                    {
                        case 1: log.Debug(message); break;
                        case 2: log.Info(message); break;
                        case 3: log.Warn(message); break;
                        case 4: log.Error(message); break;
                        case 5: log.Fatal(message); break;
                        default: /* do nothing */ break;
                    }

                    // No exceptions
                    File.Delete(file);

                }
                catch(Exception ex)
                {
                    // Error in uploading, don't delete the file.
                }

            }

        }
    }
}
