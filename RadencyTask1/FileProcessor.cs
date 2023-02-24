using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;

namespace RadencyTask1
{
    
    class FileProcessor
    {
        private string inputFolder;
        private string outputFilePath;

        public FileProcessor(string inputFolder, string outputFilePath)
        {
            this.inputFolder = inputFolder;
            this.outputFilePath = outputFilePath;
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
            using (var writer = new StreamWriter(filePath, true)) { 
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
        public void Run()
        {
            while (true)
            {
                try
                {
                    var files = Directory.GetFiles(inputFolder);
                    int file_id = 0;

                    foreach (var file in files)
                    {
                        if (IsAcceptedFormat(file))
                        {
                            file_id++;
                            //var paymentRecords = ReadPaymentRecordsFromFile(file);
                            IFileProcessor fileProcessor = FileProcessorFactory.CreateFileProcessor(file);
                            var paymentRecords = fileProcessor.ProcessFile(file);
                            foreach (var record in paymentRecords)
                            {
                                ValidatePaymentRecord(record);
                            }

                            SaveOutputDataToFile(paymentRecords, outputFilePath + file_id + ".json");

                            //ArchiveFile(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }

                // Sleep before processing the next batch of files
                System.Threading.Thread.Sleep(50000);
            }
        }
    }
}
