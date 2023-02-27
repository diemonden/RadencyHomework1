using System;
using System.Linq;
using System.Configuration;
using System.IO;

namespace RadencyTask1
{
    class Program
    {

        static void PrintCommands()
        {
            Console.WriteLine("Write 1 if you want to watch new adding files and process existing.");
            Console.WriteLine("Write 2 if you want to only process existing files.");
            Console.WriteLine("Write 3 if you want to EXIT");

        }
        static void Main(string[] args)
        {
            string configFilePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,"App.config");

            if (File.Exists(configFilePath))
            {
                var inputFolder = ConfigurationManager.AppSettings["inputFilePath"];
                var outputFolder = ConfigurationManager.AppSettings["outputFilePath"];
                var todayInfo = ConfigurationManager.AppSettings["todayInfoFilePath"];
                PrintCommands();
                while (true)
                {
                    var input = Console.ReadLine();
                    if (input == "1" || input == "2")
                    {
                        var processor = new FileProcessor<PaymentData,PaymentValidation,AllPaymentData, PaymentFileProcessorFactory>(inputFolder, outputFolder, todayInfo);
                        processor.ProcessExistingFiles();
                        if (input == "1")
                        {
                            processor.StartWatcher();
                            Console.WriteLine("Watcher stoped.");
                        }
                        Console.WriteLine("Files processed successfully.");
                        PrintCommands();
                    }
                    else if (input == "3")
                    {
                        Console.WriteLine("Closing.. Press any buttton.");
                        Console.ReadKey();
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Wrong Command. Try again");
                    }
                }
            }
            else
            {
                Console.WriteLine("Error. Config file does not exist");
                Console.ReadKey();
            }

        }
    }
}
