using System;
using System.IO;

namespace copier.Helper;

public static class AppFileLogger
{
    private const string FileName = "AppLog.txt";

    public static void AddText(string message)
    {
        try
        {
            string appDirectory = AppContext.BaseDirectory;
            string filePath = Path.Combine(appDirectory, FileName);

            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

            File.AppendAllText(filePath, logEntry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
