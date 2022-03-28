using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using CsvHelper;
using UserActivityTracker.Interfaces;

namespace UserActivityTracker.Models
{
    internal class DataRepository : IDataRepository
    {
        private IFileReader _fileReader;

        private List<Record> records;

        public DataRepository(IFileReader fileReader)
        {
            _fileReader = fileReader;
        }

        public void GetFileData(string filePath)
        {
            using (var reader = _fileReader.Read(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                records = csv.GetRecords<Record>().ToList();
            }
        }

        public void WriteToInput(string filePath)
        {
            if (records != null)
            {
                if (filePath.EndsWith(".txt"))
                {
                    string transformedData = TransformRecords();
                    File.WriteAllText(filePath, transformedData);
                }
                else
                    throw new FileFormatException();
            }
        }

        private string TransformRecords()
        {
            // foreach records by ip address
            // get relevant data for every user
            // - calculate the occurences of each activity
            // - build string with the data
            // - add new line
            // removeAll records with the used ip address
            // continue to next ip address
            // repeat until there are no more records

            var addresses = new List<string>();
            foreach (var record in records)
            {
                addresses.Add(record.IPAddress);
            }

            var users = addresses.Distinct().ToList(); // Remove duplicate addresses

            var data = new StringBuilder();

            while (records.Count > 0)
            {
                foreach (var user in users)
                {
                    // Activity type, count of occurences
                    var activityCountDictionary = new Dictionary<int, int>()
                    {
                        { 1, 0 },
                        { 2, 0 },
                        { 3, 0 },
                        { 4, 0 },
                        { 5, 0 },
                        { 6, 0 },
                        { 7, 0 },
                        { 8, 0 },
                        { 9, 0 },
                        { 10, 0 },
                        { 11, 0 }
                    };

                    foreach (var record in records)
                    {
                        if (record.IPAddress.Equals(user))
                        {
                            activityCountDictionary[(int)record.Type + 1]++; // Increment the count for current activity
                        }
                    }

                    int sum = 0;

                    // Generate {type}[{clicks}] -1 ... -1 -2 SUtility:{sum of all[{clicks}]}
                    foreach (KeyValuePair<int, int> item in activityCountDictionary)
                    {
                        if (item.Value > 0)
                        {
                            data.Append($"{item.Key}[{item.Value}] -1 ");
                            sum += item.Value;
                        }
                    }

                    data.Append($"-2 SUtility:{sum}{Environment.NewLine}");

                    records.RemoveAll(r => r.IPAddress.Equals(user));
                }
            }

            return data.ToString();
        }
    }
}