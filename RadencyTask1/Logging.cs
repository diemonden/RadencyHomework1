using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace RadencyTask1
{
    class Logging
    {
        private string outputFilePath;
        private string todayInfoFilePath;
        //meta.log fields
        public int parsed_files = 0;
        public int parsed_lines = 0;
        public int found_errors = 0;
        public List<string> invalidFiles = new List<string>();
        private string invalidFilesString = "";
        private string pastDateString = "";

        public Logging(string _outputFilePath, string _todayInfoFilePath)
        {
            outputFilePath = _outputFilePath;
            todayInfoFilePath = _todayInfoFilePath;
            ReadTodayInfo();
            CheckPastLog();
        }
        public void setMidnightTimer() {
            // set up timer to trigger at midnight
            var now = DateTime.Now;
            var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var timeUntilMidnight = startOfDay.AddDays(1) - now;
            var timer = new Timer(TimerCallback, null, timeUntilMidnight, TimeSpan.FromDays(1));
        }
        private void ResetData()
        {
            parsed_files = 0;
            parsed_lines = 0;
            found_errors = 0;
            invalidFiles.Clear();
            pastDateString = DateTime.Now.ToString("MM-dd-yyyy");
        }
        private void TimerCallback(object state)
        {
            // create meta.log file in subfolder C
            string filePath = Path.Combine(outputFilePath,DateTime.Now.ToString("MM-dd-yyyy"), "meta.log");
            WriteMetaLog(filePath);
            // reset statistics
            ResetData();  
        }
        public void CheckPastLog()
        {
            if (pastDateString != DateTime.Now.ToString("MM-dd-yyyy"))
            {
                var pastMetaLogPath = Path.Combine(outputFilePath, pastDateString, "meta.log");
                if (!File.Exists(pastMetaLogPath))
                {
                    WriteMetaLog(pastMetaLogPath);
                }
                ResetData();

            }
        }
        public void WriteMetaLog(string filePath)
        {
            invalidFilesString = string.Join(", ", invalidFiles.Select(f => $"\"{f}\""));
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"parsed_files: {parsed_files}");
                writer.WriteLine($"parsed_lines: {parsed_lines}");
                writer.WriteLine($"found_errors: {found_errors}");
                writer.WriteLine($"invalid_files: [{invalidFilesString}]");
            }
        }

        public void SaveTodayInfo()
        {
            WriteMetaLog(todayInfoFilePath);
            using (var writer = new StreamWriter(todayInfoFilePath, true))
            {
                writer.WriteLine($"past_date: {DateTime.Now.ToString("MM-dd-yyyy")}");
            }
        }

        public void ReadTodayInfo()
        {
            try
            {
                if (File.Exists(todayInfoFilePath))
                {

                    using (var reader = new StreamReader(todayInfoFilePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] parts = line.Split(": ");
                            if (parts.Length == 2)
                            {
                                string key = parts[0];
                                string value = parts[1];
                                switch (key)
                                {
                                    case "parsed_files":
                                        parsed_files = int.Parse(value);
                                        break;
                                    case "parsed_lines":
                                        parsed_lines = int.Parse(value);
                                        break;
                                    case "found_errors":
                                        found_errors = int.Parse(value);
                                        break;
                                    case "invalid_files":
                                        invalidFilesString = value.Trim(' ', '[', ']');
                                        invalidFiles = invalidFilesString.Split(", ").ToList();
                                        break;
                                    case "past_date":
                                        pastDateString = value;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveTodayInfo();
            }
        }
        ~Logging(){
            SaveTodayInfo();
        }
    }
}
