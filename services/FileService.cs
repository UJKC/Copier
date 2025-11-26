using Avalonia.Controls;
using copier.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace copier.Services;

public class FileService
{
    [System.Obsolete]
    public async Task<List<EntryData>?> ImportAsync(Window window)
    {
        var ofd = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters =
            {
                new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } }
            }
        };

        var result = await ofd.ShowAsync(window);
        var path = result?.FirstOrDefault();
        if (path == null || !File.Exists(path)) return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<EntryData>>(json);
    }

    [System.Obsolete]
    public async Task ExportAsync(Window window, List<EntryData> entries)
    {
        var sfd = new SaveFileDialog
        {
            DefaultExtension = "json",
            Filters =
            {
                new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } }
            }
        };

        var path = await sfd.ShowAsync(window);
        if (path == null) return;

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }
}
