using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadencyTask1
{
    public interface IFileProcessor<T> where T: InputData
    {
        ConcurrentBag<T> ProcessFile(string filePath);
    }

    public class TxtPaymentFileProcessor : IFileProcessor<PaymentData>
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

        public ConcurrentBag<PaymentData> ProcessFile(string filePath)
        {
            var paymentRecords = new ConcurrentBag<PaymentData>();
            try
            {
                Parallel.ForEach(File.ReadLines(filePath), line => {
                    var values = SplitLine(line);
                    PaymentData record;
                    if (values.Length != 7)
                    {
                        //it will be wrong on validation
                        Console.WriteLine($"Line: {line} in file {filePath} has wrong args length.");
                        record = new PaymentData();
                    }
                    else
                    {
                        try
                        {
                            record = new PaymentData
                            {
                                FirstName = values[0],
                                LastName = values[1],
                                Address = values[2],
                                City = values[2].Substring(1, values[2].IndexOf(',') - 1),
                                Payment = decimal.Parse(values[3], CultureInfo.InvariantCulture),
                                Date = DateTime.ParseExact(values[4], "yyyy-dd-MM", CultureInfo.InvariantCulture),
                                AccountNumber = long.Parse(values[5]),
                                Service = values[6]
                            };
                            
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Line: {line}  in file {filePath} throws exception: {ex}");
                            record = new PaymentData();
                        }
                        paymentRecords.Add(record);
                    }
                    
                });
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return paymentRecords;
        }
    }

    public class CsvPaymentFileProcessor : IFileProcessor<PaymentData>
    {
        public ConcurrentBag<PaymentData> ProcessFile(string filePath)
        {
            var paymentRecords = new ConcurrentBag<PaymentData>();
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
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    try
                    {
                        //paymentRecords = csv.GetRecords<PaymentData>().ToList();
                        var record = csv.GetRecord<PaymentData>();
                        record.City = record.Address.Substring(0, record.Address.IndexOf(','));
                        paymentRecords.Add(record);

                    }
                    catch (CsvHelperException)
                    {

                        paymentRecords.Add(new PaymentData());
                    }
                }
            }
            }
            return paymentRecords;
        }
    }
    
    public interface IFileProcessorFactory<T> where T : InputData
    {
        IFileProcessor<T> CreateFileProcessor(string filePath);
    }

    public class PaymentFileProcessorFactory : IFileProcessorFactory<PaymentData>
    {
        public IFileProcessor<PaymentData> CreateFileProcessor(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            switch (extension)
            {
                case ".txt":
                    return new TxtPaymentFileProcessor();
                case ".csv":
                    return new CsvPaymentFileProcessor();
                default:
                    throw new ArgumentException("Unsupported file extension: " + extension);
            }
        }
    }
}
