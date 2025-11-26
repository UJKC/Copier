using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace copier.Helper;

public static class ShortcutHelper
{
    private static readonly string AppName = "CopierApp";
    private static readonly string FirstRunFlagPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppName, "shortcut.created"
    );

    public static void CreateShortcutIfNeeded()
    {
        if (File.Exists(FirstRunFlagPath))
            return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CreateWindowsShortcut();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                CreateLinuxShortcut();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                CreateMacShortcut();

            Directory.CreateDirectory(Path.GetDirectoryName(FirstRunFlagPath)!);
            File.WriteAllText(FirstRunFlagPath, "true");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Shortcut creation failed: {ex.Message}");
        }
    }

    private static void CreateWindowsShortcut()
    {
        // Instead of .lnk, create a .bat launcher shortcut
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string shortcutPath = Path.Combine(desktopPath, $"{AppName}.bat");
        string exePath = Process.GetCurrentProcess().MainModule!.FileName!;

        string content = $"start \"\" \"{exePath}\"";

        File.WriteAllText(shortcutPath, content);
    }

    private static void CreateLinuxShortcut()
    {
        string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{AppName}.desktop");
        string exePath = Process.GetCurrentProcess().MainModule!.FileName!;

        string contents = $"""
            [Desktop Entry]
            Version=1.0
            Type=Application
            Name={AppName}
            Exec="{exePath}"
            Icon=utilities-terminal
            Terminal=false
            """;

        File.WriteAllText(desktopPath, contents);
        Process.Start("chmod", $"+x \"{desktopPath}\"");
    }

    private static void CreateMacShortcut()
    {
        string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName);
        string appPath = Process.GetCurrentProcess().MainModule!.FileName!;

        Process.Start("ln", $"-s \"{appPath}\" \"{desktopPath}\"");
    }
}
