using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace RadencyTask1
{
    //todo: validation
    //      logging and folders by date
    class FileProcessor
    {
        private string inputFolder;
        private string outputFilePath;
        private string countFile;
        //meta.log fields
        private List<string> invalidFiles = new List<string>();
        private int found_errors = 0;
        private int parsed_files = 0;
        private int parsed_lines = 0;
        //todays file count
        private int file_id = 0;

        public FileProcessor(string inputFolder, string outputFilePath, string countFile)
        {
            this.inputFolder = inputFolder;
            this.outputFilePath = outputFilePath;
            this.countFile = countFile;
            file_id = ReadCount();
        }

        private bool IsAcceptedFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            Console.WriteLine(ext);
            return ext == ".txt" || ext == ".csv";
        }
        //?
        private void ValidatePaymentRecord(InputData record)
        {
            if (string.IsNullOrEmpty(record.FirstName))
                throw new Exception("First name is required.");
            if (string.IsNullOrEmpty(record.LastName))
                throw new Exception("Last name is required.");
            if (string.IsNullOrEmpty(record.City))
                throw new Exception("Address c is required.");
            /*
            if (string.IsNullOrEmpty(record.Street))
                throw new Exception("Address s is required.");
            if (record.HouseNumber < 0)
                throw new Exception("Address h is required.");
            if (record.AppartmentNumber < 0)
                throw new Exception("Address a is required.");
                 */
            if (record.Payment < 0)
                throw new Exception("Payment amount cannot be negative.");
            if (record.Date > DateTime.Now)
                throw new Exception("Payment date cannot be in the future.");
            /*
            if (record.AccountNumber <= 0)
                throw new Exception("Account number is invalid.");
                */
            if (string.IsNullOrEmpty(record.Service))
                throw new Exception("Service is required.");
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
                watcher.Filter = "*.txt"; // only watch for txt files
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
                SaveCount();
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
                var files = Directory.GetFiles(inputFolder);

                foreach (var file in files)
                {
                    ProcessFile(file);
                }
                SaveCount();
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
                    file_id++;
                    Console.WriteLine($"Processing file {file}...");
                    IFileProcessor fileProcessor = FileProcessorFactory.CreateFileProcessor(file);
                    var paymentRecords = fileProcessor.ProcessFile(file);
                    foreach (var record in paymentRecords)
                    {
                        ValidatePaymentRecord(record);
                    }
                    SaveOutputDataToFile(paymentRecords, outputFilePath + file_id + ".json");
                    //ArchiveFile(file);
                    Console.WriteLine($"Finished processing file {file}.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
        }

        public void WriteMetaLog() {

        }

        public void SaveCount()
        {
            using (var writer = new StreamWriter(countFile, false))
            {
                writer.WriteLine(file_id);
            }
        }
        public int ReadCount()
        {
            var countFile = ConfigurationManager.AppSettings["todayCountFilePath"];
            if (File.Exists(countFile))
            {
                using (var reader = new StreamReader(countFile))
                {
                    return int.Parse(reader.ReadLine());
                }
            }
            return 0;
        }
    }
}
