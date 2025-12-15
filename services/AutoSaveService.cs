using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using copier.Models;

namespace copier.Services
{
    public class AutoSaveService
    {
        private readonly string _filePath;
        private readonly Window _window;
        private readonly EntryManager _entryManager;
        private readonly List<StackPanel> _allEntryPanels;

        public AutoSaveService(
            string filePath,
            Window window,
            EntryManager entryManager,
            List<StackPanel> allEntryPanels)
        {
            _filePath = filePath;
            _window = window;
            _entryManager = entryManager;
            _allEntryPanels = allEntryPanels;
        }

        public async Task AutoLoadAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return;

                var json = await File.ReadAllTextAsync(_filePath);
                var entries = JsonSerializer.Deserialize<List<EntryData>>(json);
                if (entries == null)
                    return;

                var stack = _window.FindControl<StackPanel>("ItemsPanel")!;

                _allEntryPanels.Clear();
                stack.Children.Clear();
                _entryManager.Panels.Clear();

                _entryManager.LoadPanels(entries, _window, stack);
            }
            catch
            {
                // optionally log
            }
        }

        public async Task AutoSaveAsync()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                var entries = _entryManager.ToEntryList();
                var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_filePath, json);
            }
            catch
            {
                // suppress autosave errors
            }
        }
    }
}
