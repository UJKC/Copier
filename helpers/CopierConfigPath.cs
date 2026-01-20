using System;
using System.IO;
using System.Runtime.InteropServices;

namespace copier.Helper;

public static class CopierConfigPath
{
    public static string GetRootConfigPath()
    {
        var dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Copier"
    );

        Directory.CreateDirectory(dir);

        return Path.Combine(dir, ".copierconf");
    }
}
