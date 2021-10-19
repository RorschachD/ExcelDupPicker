using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorConsole;
using CsvHelper;
using CsvHelper.Configuration;

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
                serverRequestDic = new Dictionary<int, string>();
                csvFile = new List<List<string>>();

                Console.WriteLine("serverRequestColumn");
                var serverRequestColumn = int.Parse(Console.ReadLine());

                Console.WriteLine("safeHarbourScoreColumn");
                var safeHarbourScoreColumn = int.Parse(Console.ReadLine());

                //var workbook = new XLWorkbook();
                foreach (var filePath in filePathList)
                {
                    Console.WriteLine(filePath);
                    //From Debug folder(Hard coded in)
                    /*var indexOfFirstFileLetter = filePath.IndexOf("Debug", StringComparison.Ordinal) + 6;
                    var fileLength = filePath.IndexOf(".", StringComparison.Ordinal) - indexOfFirstFileLetter;
                    var fileName = filePath.Substring(indexOfFirstFileLetter, fileLength);*/

                    BuildServerRequestDic(serverRequestColumn, filePath);

                    duplicateLibrary = BuildDuplicateRequestDic();

                    WriteDuplicateIndicator(safeHarbourScoreColumn, filePath);

                    Console.WriteLine("Finished. Press anykey to exit");
                }
                //workbook.SaveAs($"Invoice.xlsx");
                //Console.WriteLine("Total " + clientCount + " has been procressed");
                Console.WriteLine("Finished. Press anykey to exit");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static IEnumerable<string> FileDirectoryGenerator()
        {
            var filePathList = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csv");

            return filePathList;
        }

        private static void BuildServerRequestDic(int serverRequestColumn, string filePath)
        {
            var reader = new StreamReader(filePath);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var rowIndex = 0;
            while (csv.Read())
            {
                if (rowIndex == 0)
                {
                    csvFile.Add(csv.Parser.Record.ToList());
                    rowIndex++;
                    continue;
                }
                var serverRequest = csv[serverRequestColumn - 1];
                Console.WriteLine(serverRequest);
                var referenceIndex = serverRequest.IndexOf("clientReference");
                var requestWithoutReferenceTitle = serverRequest.Substring(referenceIndex + 20);
                var commaIndex = requestWithoutReferenceTitle.IndexOf(",");
                var finalRequest = "{" + requestWithoutReferenceTitle.Substring(commaIndex + 3);
                //var finalRequest = $" { {requestWithoutReferenceTitle.Substring(commaIndex + 3)}";

                //Console.WriteLine(finalRequest);
                serverRequestDic.Add(rowIndex, finalRequest);
                Console.WriteLine();
                csvFile.Add(csv.Parser.Record.ToList());
                rowIndex++;
            }
            csv.Dispose();
        }

        private static IEnumerable<IGrouping<string, int>> BuildDuplicateRequestDic()
        {
            var lookup = serverRequestDic.ToLookup(x => x.Value, x => x.Key).Where(x => x.Count() > 1);

            /*var dasd = lookup.ElementAt(0);
            var asdsa = dasd.Key;
            foreach (var item in dasd)
            {
                Console.WriteLine(item);
            }
            var asdsaa = dasd.ElementAt(0);*/

            foreach (var item in lookup)
            {
                var keys = item.Aggregate("", (s, v) => s + ", " + v);
                var message = "The following keys have the value " + item.Key + ":" + keys;
                Console.WriteLine(message);
            }

            return lookup;
        }

        private static void WriteDuplicateIndicator(int safeHarbourScoreColumn, string filePath)
        {
            //var streamWriter = new StreamWriter(filePath);
            //var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);


            //using (var stream = File.Open("C:\\Users\\Rorschach\\Documents\\Visual Studio 2019\\Project\\ExcelDupPicker\\ExcelDupPicker\\bin\\Debug\\result.csv", FileMode.Append))
            using (var writer = new StreamWriter("C:\\Users\\Rorschach\\Documents\\Visual Studio 2019\\Project\\ExcelDupPicker\\ExcelDupPicker\\bin\\Debug\\result.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                /*foreach (var duplicate in duplicateLibrary)
                {
                    foreach (var item in duplicate)
                    {
                        csv.WriteRecord()
                    }
                }*/

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

        public class Foo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
