using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace RandomWheel.Services
{
    public class CsvService
    {
        /// <summary>
        /// Validates a CSV file before import.
        /// </summary>
        /// <param name="path">Path to the CSV file</param>
        /// <returns>Tuple of (isValid, errorMessage)</returns>
        public (bool IsValid, string? ErrorMessage) ValidateCsvFile(string path)
        {
            if (!File.Exists(path))
                return (false, "File does not exist.");

            try
            {
                // Check file size (reasonable limit of 10MB)
                var fileInfo = new FileInfo(path);
                if (fileInfo.Length > 10 * 1024 * 1024)
                    return (false, "File is too large (max 10MB).");

                if (fileInfo.Length == 0)
                    return (false, "File is empty.");

                // Try to read as UTF-8
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                
                // Read first few lines to validate structure
                int lineCount = 0;
                int itemCount = 0;
                string? line;
                
                while ((line = reader.ReadLine()) != null && lineCount < 10)
                {
                    lineCount++;
                    if (!string.IsNullOrWhiteSpace(line))
                        itemCount++;
                }

                if (itemCount == 0)
                    return (false, "No valid items found in the CSV file.");

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error reading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads items from CSV file, only from the first column.
        /// Expects UTF-8 encoding.
        /// </summary>
        public IEnumerable<string> ReadItems(string path)
        {
            // Use UTF-8 encoding explicitly
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                DetectDelimiter = true,
                BadDataFound = null, // Ignore bad data
                MissingFieldFound = null, // Ignore missing fields
            });

            while (csv.Read())
            {
                // Only read the FIRST column
                if (csv.Parser.Record != null && csv.Parser.Record.Length > 0)
                {
                    var value = csv.GetField(0); // Only first column
                    if (!string.IsNullOrWhiteSpace(value))
                        yield return value.Trim();
                }
            }
        }

        public void WriteItems(string path, IEnumerable<string> items)
        {
            // Write with UTF-8 encoding
            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            foreach (var item in items)
            {
                csv.WriteField(item);
                csv.NextRecord();
            }
        }
    }
}
