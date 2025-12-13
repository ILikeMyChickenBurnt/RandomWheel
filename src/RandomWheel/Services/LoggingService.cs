using System;
using System.IO;

namespace RandomWheel.Services
{
    public class LoggingService
    {
        public void Log(string actionType, string listName, string details)
        {
            Paths.EnsureAppFolder();
            var line = $"{DateTimeOffset.Now:O}\t{actionType}\t{listName}\t{details}";
            File.AppendAllText(Paths.AuditLogFile, line + Environment.NewLine);
        }
    }
}
