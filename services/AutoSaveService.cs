using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using copier.Models;

namespace copier.Services;

public class AutoSaveService
{
    private readonly string filePath;

    public AutoSaveService(string path)
    {
        filePath = path;
    }

    public async Task SaveAsync(List<EntryData> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<List<EntryData>?> LoadAsync()
    {
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<EntryData>>(json);
    }
}
