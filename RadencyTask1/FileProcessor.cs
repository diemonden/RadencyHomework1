using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;

namespace RadencyTask1
{
    //todo: 
    //      concurency
    //      dont read in archive
    class FileProcessor
    {
        static private string inputFolder;
        static private string outputFilePath;
        static private string outputDayFilePath;
        static private string todayInfoFilePath;
        //meta.log fields
        static private List<string> invalidFiles = new List<string>();
        static private string invalidFilesString = "";
        static private int found_errors = 0;
        static private int parsed_files = 0;
        static private int parsed_lines = 0;
        static private string pastDateString = "";
        //todays file count
        private int file_id = 0;
        
        public void WriteMetaLog(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"parsed_files: {parsed_files}");
                writer.WriteLine($"parsed_lines: {parsed_lines}");
                writer.WriteLine($"found_errors: {found_errors}");
                writer.WriteLine("invalid_files: {invalidFilesString} ]");
            }
        }
        public FileProcessor(string _inputFolder, string _outputFilePath, string _todayInfoFilePath)
        {
            inputFolder = _inputFolder;
            outputFilePath = _outputFilePath;
            todayInfoFilePath = _todayInfoFilePath;
            ReadTodayInfo();
            if (pastDateString != DateTime.Now.ToString("MM-dd-yyyy"))
            {
                var pastMetaLogPath = Path.Combine(outputFilePath, pastDateString, "meta.log");
                if (!File.Exists(pastMetaLogPath))
                {
                    WriteMetaLog(pastMetaLogPath);
                }
            }
            var todayFolder = Path.Combine(outputFilePath, DateTime.Now.ToString("MM-dd-yyyy"));
            if (!Directory.Exists(todayFolder))
                Directory.CreateDirectory(todayFolder);
            outputDayFilePath = Path.Combine(todayFolder, "output");
            // set up timer to trigger at midnight
            var now = DateTime.Now;
            var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var timeUntilMidnight = startOfDay.AddDays(1) - now;
            var timer = new Timer(TimerCallback, null, timeUntilMidnight, TimeSpan.FromDays(1));
        }

        private void TimerCallback(object state)
        {
            // create meta.log file in subfolder C
            string filePath = Path.Combine(outputDayFilePath, "meta.log");

            invalidFilesString = string.Join(", ", invalidFiles.Select(f => $"\"{f}\""));
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"parsed_files: {parsed_files}");
                writer.WriteLine($"parsed_lines: {parsed_lines}");
                writer.WriteLine($"found_errors: {found_errors}");
                writer.WriteLine("invalid_files: {invalidFilesString} ]");
            }

            // reset statistics
            parsed_files = 0;
            parsed_lines = 0;
            found_errors = 0;
            invalidFiles.Clear();
            pastDateString = DateTime.Now.ToString("MM-dd-yyyy");
        }
        private bool IsAcceptedFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            Console.WriteLine(ext);
            return ext == ".txt" || ext == ".csv";
        }
        //?
        private bool ValidatePaymentRecord(InputData record)
        {
            if (string.IsNullOrEmpty(record.FirstName))
                return false;
            if (string.IsNullOrEmpty(record.LastName))
                return false;
            if (string.IsNullOrEmpty(record.Address))
                return false;
            if (string.IsNullOrEmpty(record.City))
                return false;
            if (record.Payment < 0)
                return false;
            if (record.Date > DateTime.Now)
                return false;
            if (record.AccountNumber <= 0)
                return false;
            if (string.IsNullOrEmpty(record.Service))
                throw new Exception("Service is required.");
            return true;
        }

        //new
        private void SaveOutputDataToFile(List<InputData> data, string filePath)
        {
            AllData allData = new AllData(data);

            string jsonString = allData.getJSONString();
            Console.WriteLine(jsonString);
            using (var writer = new StreamWriter(filePath, false)) { 
                writer.WriteLine(jsonString);
            }
        }

        private void ArchiveFile(string filePath)
        {
            var archiveFolder = Path.Combine(inputFolder, "Archive");
            if (!Directory.Exists(archiveFolder))
                Directory.CreateDirectory(archiveFolder);

            var fileName = Path.GetFileName(filePath);
            var archivePath = Path.Combine(archiveFolder, fileName);

            if (File.Exists(archivePath))
            {
                // Generate a new name for the file by adding a timestamp to the end
                var newFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(fileName);
                archivePath = Path.Combine(archiveFolder, newFileName);
            }

            File.Move(filePath, archivePath);
        }

        private int ReadFSWCommand()
        {
            while (true)
            {
                Console.WriteLine("Type 1 to stop, 2 to restart");
                string command = Console.ReadLine();
                switch (command)
                {
                    case "1": return 1;
                    case "2": return 2;
                    default: continue;
                }
            }
        }

        public void StartWatcher()
        {
            try
            {
                var watcher = new FileSystemWatcher(inputFolder);
                watcher.Filter = "";
                watcher.IncludeSubdirectories = false; // only watch the specified folder
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.EnableRaisingEvents = true;

                Console.WriteLine($"Watching folder {inputFolder} for new txt files...");
                int command = ReadFSWCommand();
                watcher.Dispose();
                if (command == 2)
                    StartWatcher();
                SaveTodayInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
            {
                ProcessFile(e.FullPath);
            }
        }

        public void ProcessExistingFiles()
        {
            try
            {
                var files = Directory.GetFiles(inputFolder,"*.*",SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    ProcessFile(file);
                }
                SaveTodayInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        public void ProcessFile(string file)
        {
            if (IsAcceptedFormat(file))
            {
                try
                {
                    bool isValid = true;
                    file_id++;
                    Console.WriteLine($"Processing file {file}...");
                    IFileProcessor fileProcessor = FileProcessorFactory.CreateFileProcessor(file);
                    var paymentRecords = fileProcessor.ProcessFile(file);
                    foreach (var record in paymentRecords)
                    {
                        if (ValidatePaymentRecord(record))
                            parsed_lines++;
                        else
                        {
                            found_errors++;
                            isValid = false;
                            paymentRecords.Remove(record);
                        }
                            
                    }
                    SaveOutputDataToFile(paymentRecords, outputDayFilePath + file_id + ".json");
                    ArchiveFile(file);
                    Console.WriteLine($"Finished processing file {file}.");
                    parsed_files++;
                    if (!isValid)
                        invalidFiles.Add(file);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            else
            {
                invalidFiles.Add(file);
            }
        }
        
        public void SaveTodayInfo()
        {
            using (var writer = new StreamWriter(todayInfoFilePath, false))
            {
                writer.WriteLine(file_id);
                writer.WriteLine(parsed_files);
                writer.WriteLine(parsed_lines);
                writer.WriteLine(found_errors);
                writer.WriteLine(invalidFilesString);
                writer.WriteLine(DateTime.Now.ToString("MM-dd-yyyy"));
            }
        }
        public void ReadTodayInfo()
        {
            var countFile = ConfigurationManager.AppSettings["todayInfoFilePath"];
            if (File.Exists(countFile))
            {
                try
                {
                    using (var reader = new StreamReader(countFile))
                    {
                        file_id = int.Parse(reader.ReadLine());
                        parsed_files = int.Parse(reader.ReadLine());
                        parsed_lines = int.Parse(reader.ReadLine());
                        found_errors = int.Parse(reader.ReadLine());
                        invalidFilesString = reader.ReadLine();
                        invalidFiles = invalidFilesString.Split(", ").ToList();
                        pastDateString = reader.ReadLine();
                    }
                } catch (Exception ex)
                {
                    SaveTodayInfo();
                }
                
            }
        }
        ~FileProcessor()
        {
            SaveTodayInfo();
        }
    }
}
