using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CsvHelper;

namespace ExcelDupPicker
{
    class Program
    {
        public static List<List<string>> csvFile;

        public static Dictionary<int, string> serverRequestDic;

        public static IEnumerable<IGrouping<string, int>> duplicateLibrary;

        static void Main(string[] args)
        {
            try
            {
                var filePathList = FileDirectoryGenerator();

                //var workbook = new XLWorkbook();
                foreach (var filePath in filePathList)
                {
                    serverRequestDic = new Dictionary<int, string>();
                    csvFile = new List<List<string>>();

                    BuildServerRequestDic(filePath);

                    duplicateLibrary = BuildDuplicateRequestDic();

                    if (duplicateLibrary.Count() == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("There is no duplicate in file: " + GetFileName(filePath));
                        Console.WriteLine();
                        Console.ResetColor();
                        Console.WriteLine("Process finished for " + GetFileName(filePath));
                        Console.WriteLine("[***************************************************************************************************]");
                        Console.WriteLine();
                        continue;
                    }

                    WriteDuplicateIndicator(filePath);
                    Console.WriteLine("Process finished for " + GetFileName(filePath));
                    Console.WriteLine("[***************************************************************************************************]");
                    Console.WriteLine();
                }
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine();
                Console.WriteLine("All Finished. Press anykey to exit");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e);
                Console.ReadKey();
                throw;
            }
        }

        private static IEnumerable<string> FileDirectoryGenerator()
        {
            var filePathList = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csv");

            return filePathList;
        }

        private static string GetFileName(string filePath)
        {
            var indexOfBackSlash = filePath.LastIndexOf("\\");
            var fileNameWithExtension = filePath.Substring(indexOfBackSlash);
            var indexOfComma = fileNameWithExtension.LastIndexOf(".");
            var fileName = fileNameWithExtension.Substring(1, indexOfComma - 1);

            return fileName;
        }

        private static void BuildServerRequestDic(string filePath)
        {
            var recordsLength = 0;
            var serverRequestColumn = 0;
            using (StreamReader sr = File.OpenText(filePath))
            {
                while (sr.ReadLine() != null)
                {
                    recordsLength++;
                }
                recordsLength--;
            }

            var reader = new StreamReader(filePath);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var rowIndex = 0;
            var progressInterval = (double) 100 / recordsLength;
            Console.WriteLine("Building Server Request Dictionary...");
            using (var progress = new ProgressBar())
            {
                while (csv.Read())
                {
                    if (rowIndex == 0)
                    {
                        csvFile.Add(csv.Parser.Record.ToList());

                        var header = csvFile[0];
                        var headerIndex = 0;
                        foreach (var item in header)
                        {
                            if (item.Contains("serverRequest"))
                            {
                                serverRequestColumn = headerIndex;
                                break;
                            }
                            headerIndex++;
                        }
                        
                        rowIndex++;
                        continue;
                    }
                    var serverRequest = csv[serverRequestColumn];
                    var referenceIndex = serverRequest.IndexOf("clientReference");
                    var requestWithoutReferenceTitle = serverRequest.Substring(referenceIndex + 20);
                    var commaIndex = requestWithoutReferenceTitle.IndexOf(",");
                    var finalRequest = "{" + requestWithoutReferenceTitle.Substring(commaIndex + 3);
                    serverRequestDic.Add(rowIndex, finalRequest);
                    csvFile.Add(csv.Parser.Record.ToList());
                    if ((rowIndex - 1) % 10 == 0)
                    {
                        progress.Report(progressInterval * rowIndex / 100);
                        Thread.Sleep(20);
                    }
                    rowIndex++;
                }
                progress.Report((double)100 / 100);
                Thread.Sleep(1000);
                Console.WriteLine(" Done.");
            }
            csv.Dispose();
        }

        private static IEnumerable<IGrouping<string, int>> BuildDuplicateRequestDic()
        {
            var lookup = serverRequestDic.ToLookup(x => x.Value, x => x.Key).Where(x => x.Count() > 1);
            
            foreach (var item in lookup)
            {
                var keys = item.Aggregate("", (s, v) => s + ", " + v);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("The following request has one or more duplicates: " + item.Key);
                Console.ResetColor();
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("The duplicates occure at data row: " + keys);
                Console.WriteLine();
                Console.ResetColor();
            }
            Console.ResetColor();
            return lookup;
        }

        private static void WriteDuplicateIndicator(string filePath)
        {
            var safeHarbourScoreColumn = 0;
            var header = csvFile[0];
            var headerIndex = 0;
            foreach (var item in header)
            {
                if (item.Contains("safeHarbourScore"))
                {
                    safeHarbourScoreColumn = headerIndex;
                    break;
                }
                headerIndex++;
            }

            foreach (var item in duplicateLibrary)
            {
                var skipFirst = true;
                foreach (var index in item)
                {
                    if (skipFirst)
                    {
                        skipFirst = false;
                        continue;
                    }
                    csvFile[index][safeHarbourScoreColumn] = "Duplicate";
                }
            }

            var fileName = GetFileName(filePath);

            using (var writer = new StreamWriter(Directory.GetCurrentDirectory() + "\\updated " + fileName + ".csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                foreach (var record in csvFile)
                {
                    foreach (var field in record)
                    {
                        csv.WriteField(field);
                    }
                    csv.NextRecord();
                }
            }
        }
    }
}
