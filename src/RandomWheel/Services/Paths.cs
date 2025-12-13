using System;
using System.IO;

namespace RandomWheel.Services
{
    public static class Paths
    {
        public static string AppFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RandomWheel");
        public static string ListsFile => Path.Combine(AppFolder, "lists.json");
        public static string AuditLogFile => Path.Combine(AppFolder, "audit.log");

        public static void EnsureAppFolder()
        {
            if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
        }
    }
}
