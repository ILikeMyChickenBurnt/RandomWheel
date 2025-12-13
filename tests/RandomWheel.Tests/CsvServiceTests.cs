using RandomWheel.Services;
using System.IO;
using Xunit;

namespace RandomWheel.Tests
{
    public class CsvServiceTests
    {
        [Fact]
        public void Csv_RoundTrip()
        {
            var svc = new CsvService();
            var tmp = Path.GetTempFileName();
            try
            {
                var items = new[] { "Alice", "Bob, Jr.", "\"Quoted\"", " spaced " };
                svc.WriteItems(tmp, items);
                var read = svc.ReadItems(tmp);
                Assert.Contains("Alice", read);
                Assert.Contains("Bob, Jr.", read);
                Assert.Contains("\"Quoted\"", read);
                Assert.Contains("spaced", read);
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }
    }
}
