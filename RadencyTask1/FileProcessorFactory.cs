using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RadencyTask1
{
    public interface IFileProcessor
    {
        List<InputData> ProcessFile(string filePath);
    }

    public class TxtFileProcessor : IFileProcessor
    {
        private static string[] SplitLine(string line)
        {
            var parts = new List<string>();
            var inQuotes = false;
            var start = 0;
            char[] quotes = new[] { '“', '\"', '”' };
            for (var i = 0; i < line.Length; i++)
            {
                if (quotes.Contains(line[i]))
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    var part = line.Substring(start, i - start).Trim();
                    parts.Add(part);
                    start = i + 1;
                }
            }

            var lastPart = line.Substring(start).Trim();
            parts.Add(lastPart);

            return parts.ToArray();
        }

        public List<InputData> ProcessFile(string filePath)
        {
            var paymentRecords = new List<InputData>();

            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = SplitLine(line);
                    Console.WriteLine(values.Length);
                    if (values.Length != 7)
                    {
                        throw new InvalidDataException("Invalid line: " + line);
                    }

                    var record = new InputData
                    {
                        FirstName = values[0],
                        LastName = values[1],
                        City = values[2].Substring(0, values[2].IndexOf(',')),
                        Address = values[2],
                        Payment = decimal.Parse(values[3], CultureInfo.InvariantCulture),
                        Date = DateTime.ParseExact(values[4], "yyyy-dd-MM", CultureInfo.InvariantCulture),
                        AccountNumber = long.Parse(values[5]),
                        Service = values[6]
                    };

                    paymentRecords.Add(record);
                }
            }
            return paymentRecords;
        }
    }

    public class CsvFileProcessor : IFileProcessor
    {
        public List<InputData> ProcessFile(string filePath)
        {
            var paymentRecords = new List<InputData>();
            using (var reader = new StreamReader(filePath))
            {
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    // Set the delimiter (default is ',')
                    Delimiter = ",",
                    // Ignore blank lines in the file
                    IgnoreBlankLines = true,
                    // Enable or disable header validation
                    HeaderValidated = null,
                    // Enable or disable field validation
                    MissingFieldFound = null,
                    // Set the class map for the data class
                    // (optional, but recommended for better performance and less reflection overhead)
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim,

                };
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    var options = new TypeConverterOptions { Formats = new[] { "yyyy-dd-MM" } };

                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                    paymentRecords = csv.GetRecords<InputData>().ToList();
                }
            }
            foreach (var record in paymentRecords)
            {
                record.City = record.Address.Substring(0, record.Address.IndexOf(','));
            }
            return paymentRecords;
        }
    }

    public static class FileProcessorFactory
    {
        public static IFileProcessor CreateFileProcessor(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            switch (extension)
            {
                case ".txt":
                    return new TxtFileProcessor();
                case ".csv":
                    return new CsvFileProcessor();
                default:
                    throw new ArgumentException("Unsupported file extension: " + extension);
            }
        }
    }
}
