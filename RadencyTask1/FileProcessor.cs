using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadencyTask1
{
    //todo: check on invalid data
    //  SOLID
    class FileProcessor<T, Validation, AllData, FileProcessorFactory> where T : InputData
                             where Validation : IValidationStrategy<T>, new()
                             where AllData : IAllData<T>, new()
                             where FileProcessorFactory : IFileProcessorFactory<T>, new()
    {
        private string inputFolder;
        private string outputFilePath;
        private string outputDayFilePath;
        private string todayInfoFilePath;
        private string todayFolder;
        
        Logging logger;
        FileProcessorFactory factory;
        Validation validation = new Validation();
      
        public FileProcessor(string _inputFolder, string _outputFilePath, string _todayInfoFilePath)
        {
            inputFolder = _inputFolder;
            outputFilePath = _outputFilePath;
            todayInfoFilePath = _todayInfoFilePath;

            logger = new Logging(outputFilePath, todayInfoFilePath);
            factory = new FileProcessorFactory();
            validation = new Validation();

            todayFolder = Path.Combine(outputFilePath, DateTime.Now.ToString("MM-dd-yyyy"));
            if (!Directory.Exists(todayFolder))
                Directory.CreateDirectory(todayFolder);
            outputDayFilePath = Path.Combine(todayFolder, "output");

            logger.setMidnightTimer();
            
        }
        
        //i maybe should move this function to other class for SRP?
        private void SaveOutputDataToFile(ConcurrentBag<T> data, string filePath)
        {
            AllData allData = new AllData();
            allData.init(data.ToList());
            string jsonString = allData.getJSONString(); 
            Console.WriteLine(jsonString);
            using (var stream  = new FileStream(filePath, FileMode.Create)) {
                byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
                stream.Write(buffer, 0, buffer.Length);
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

        //that maybe should be in Program class (gui out of logic)
        private int ReadFSWCommand()
        {
            while (true)
            {
                Console.WriteLine("Type 1 to stop Watcher, 2 to restart");
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
                logger.SaveTodayInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if(e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
            {
                //if adding multiple files it will be work good in parallel, but it also reads existing files, which i think is not so much problem
                ProcessExistingFiles();
            } 
        }

        public void ProcessExistingFiles()
        {
            try
            {
                var files = Directory.GetFiles(inputFolder,"*.*",SearchOption.TopDirectoryOnly);
                Parallel.ForEach(files, file =>
                 {
                     ProcessFile(file);
                 });
                logger.SaveTodayInfo();

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
        
        public void ProcessFile(string file)
        {
            try
            {
                ConcurrentBag<T> validPaymentRecords = new ConcurrentBag<T>();
                bool isValid = true;
                Console.WriteLine($"Processing file {file}...");
                IFileProcessor<T> fileProcessor = factory.CreateFileProcessor(file);
                var paymentRecords = fileProcessor.ProcessFile(file);
                
                Parallel.ForEach(paymentRecords, record =>
                {
                    if (validation.validate(record))
                    {
                        logger.parsed_lines++;
                        validPaymentRecords.Add(record);
                    }
                    else
                    {
                        logger.found_errors++;
                        isValid = false;
                    }
                });
                lock (validPaymentRecords)
                {
                    logger.parsed_files++;
                    SaveOutputDataToFile(validPaymentRecords, outputDayFilePath + logger.parsed_files.ToString() + ".json");
                }
                
                ArchiveFile(file);
                Console.WriteLine($"Finished processing file {file}.");
                if (!isValid)
                    logger.invalidFiles.Add(file);
            }
            catch (ArgumentException exA)
            {
                Console.WriteLine($"File {file} has invalid extension and will not be processed. {exA}");
                logger.invalidFiles.Add(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }
}
