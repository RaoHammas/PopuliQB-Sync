using System.Diagnostics;
using System.IO;

namespace PopuliQB_Tool.Services;

public class IOService
{
    public void OpenLogsFolder()
    {
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var folderPath = Path.Combine(currentDirectory, "Logs");

        if (Directory.Exists(folderPath))
        {
            Process.Start("explorer.exe", folderPath);
        }
    }
}