using System;

namespace RadencyTask1
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var inputFolder = @"C:\test\input";
            var outputFilePath = @"C:\test\output\output";

            var processor = new FileProcessor(inputFolder, outputFilePath);
            processor.Run();

            Console.WriteLine("Hello World!");
        }
    }
}
