using RandomWheel.Services;
using System;
using System.IO;
using Xunit;

namespace RandomWheel.Tests
{
    public class LoggingServiceTests
    {
        [Fact]
        public void Log_AppendsLines()
        {
            Paths.EnsureAppFolder();
            // Clear log
            if (File.Exists(Paths.AuditLogFile)) File.Delete(Paths.AuditLogFile);
            var logger = new LoggingService();
            logger.Log("AddItem", "List1", "Added 'X'");
            logger.Log("RemoveItem", "List1", "Removed 'Y'");
            var lines = File.ReadAllLines(Paths.AuditLogFile);
            Assert.Equal(2, lines.Length);
            Assert.Contains("AddItem", lines[0]);
            Assert.Contains("RemoveItem", lines[1]);
        }
    }
}
