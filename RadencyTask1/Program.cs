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
            Console.WriteLine("Write 2 if you want to only watch new adding files");
            Console.WriteLine("Write 3 if you want to only process existing files.");
            Console.WriteLine("Write 4 if you want to EXIT");

        }
        static void Main(string[] args)
        {
            string configFilePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,"App.config");

            if (File.Exists(configFilePath))
            {
                var inputFolder = ConfigurationManager.AppSettings["inputFilePath"];
                var outputFolder = ConfigurationManager.AppSettings["outputFilePath"];
                var countFile = ConfigurationManager.AppSettings["todayCountFilePath"];
                PrintCommands();
                while (true)
                {
                    var input = Console.ReadLine();
                    string[] startCommands = new[] { "1", "2", "3" };
                    if (startCommands.Contains(input))
                    {
                        var processor = new FileProcessor(inputFolder, outputFolder+"output",countFile);
                        if (input == "1" || input == "3")
                            processor.ProcessExistingFiles();
                        if (input == "1"  || input == "2")
                            processor.StartWatcher();
                        Console.WriteLine("Watcher stoped.");
                        PrintCommands();
                    }
                    else if (input == "4")
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
