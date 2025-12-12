using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using System;
using copier.Models;
using copier.Services;
using copier.Helper;

namespace copier.Views
{
    public partial class MainWindow : Window
    {
        private readonly List<StackPanel> allEntryPanels = new();
        private readonly EntryManager entryManager;
        private System.Timers.Timer? _debounceTimer;

        private readonly string AutoSavePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CopierApp", "autosave.json");

        public MainWindow()
        {
            ShortcutHelper.CreateShortcutIfNeeded();
            InitializeComponent();

            // Create the EntryManager once and reuse it (shares the allEntryPanels list)
            entryManager = new EntryManager(allEntryPanels);

            AutoLoad();
            this.Closing += (_, _) => AutoSave();
            this.AddHandler(Button.ClickEvent, Remove_Click);
        }

        private void InitializeComponent() { AvaloniaXamlLoader.Load(this); }

        private void Add_Click(object? sender, RoutedEventArgs e)
        {
            var titleBox = this.FindControl<TextBox>("TitleInputBox")!;
            var textBox = this.FindControl<TextBox>("TextInputBox")!;

            string title = titleBox.Text ?? "";
            string text = textBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text))
                return;

            AddEntry(title, text);

            HideInputPanel();
        }


        /// <summary>
        /// Adds a new entry to UI and internal lists. New entries are unpinned by default.
        /// </summary>
        private void AddEntry(string title, string text)
        {
            var itemsPanel = this.FindControl<StackPanel>("ItemsPanel")!;

            // Use the shared EntryManager to create the entry UI
            var entryPanel = entryManager.CreateEntryPanel(title, text, this);

            // Add to visual container (at end). Reorder will place pinned items above automatically.
            itemsPanel.Children.Add(entryPanel);

            // Ensure Panels list (allEntryPanels) is consistent (CreateEntryPanel already added it)
            // Reorder so new pinned items (if any) are correctly placed (new ones are unpinned by default).
            entryManager.ReorderPanels(itemsPanel);
        }

        private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
        {
            var searchBox = this.FindControl<TextBox>("SearchBox")!;
            string text = searchBox.Text ?? "";

            // stop previous timer if typing continues
            if (_debounceTimer != null)
            {
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
            }

            _debounceTimer = new System.Timers.Timer(250); // 250ms debounce
            _debounceTimer.Elapsed += (s, _) =>
            {
                _debounceTimer?.Stop();
                _debounceTimer?.Dispose();
                _debounceTimer = null;

                // Ensure UI thread invocation
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    FilterEntries(text);
                });
            };

            _debounceTimer.AutoReset = false;
            _debounceTimer.Start();
        }


        private void FilterEntries(string? filter)
        {
            var stack = this.FindControl<StackPanel>("ItemsPanel")!;
            stack.Children.Clear();
            filter = filter?.Trim().ToLower() ?? "";

            foreach (var entry in allEntryPanels)
            {
                var titleText = entry.Children[0] as TextBlock;
                string title = titleText?.Text?.ToLower() ?? "";

                // If you also want to search in body, include editableText check:
                // var editableText = entry.Children[1] as TextBox;
                // string text = editableText?.Text?.ToLower() ?? "";
                // if (title.Contains(filter) || text.Contains(filter)) { ... }

                if (title.Contains(filter))
                {
                    stack.Children.Add(entry);
                }
            }
        }

        [Obsolete]
        private async void Export_Click(object? sender, RoutedEventArgs e)
        {
            var fileDialog = new SaveFileDialog
            {
                Filters = new List<FileDialogFilter> {
                    new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } }
                },
                DefaultExtension = "json"
            };
            var path = await fileDialog.ShowAsync(this);
            if (path == null) return;

            // Use EntryManager to build the data list (includes pinned state)
            var entries = entryManager.ToEntryList();

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }

        [Obsolete]
        private async void Import_Click(object? sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter> {
                    new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } }
                }
            };

            var result = await fileDialog.ShowAsync(this);
            var path = result?.FirstOrDefault();
            if (path == null || !File.Exists(path)) return;

            var json = await File.ReadAllTextAsync(path);
            var entries = JsonSerializer.Deserialize<List<EntryData>>(json);
            if (entries == null) return;

            // Clear existing entries
            var stack = this.FindControl<StackPanel>("ItemsPanel")!;
            stack.Children.Clear();
            allEntryPanels.Clear();
            entryManager.Panels.Clear();

            // Add imported ones (LoadPanels will ensure pinned-first order)
            entryManager.LoadPanels(entries, this, stack);
        }

        private async void AutoSave()
        {
            try
            {
                var saveDir = Path.GetDirectoryName(AutoSavePath);
                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir!);

                var entries = entryManager.ToEntryList();

                var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(AutoSavePath, json);
            }
            catch
            {
                // suppress autosave errors to avoid crashing on exit; optionally log
            }
        }

        private async void AutoLoad()
        {
            try
            {
                if (!File.Exists(AutoSavePath)) return;

                var json = await File.ReadAllTextAsync(AutoSavePath);
                var entries = JsonSerializer.Deserialize<List<EntryData>>(json);
                if (entries == null) return;

                var stack = this.FindControl<StackPanel>("ItemsPanel")!;
                allEntryPanels.Clear();
                stack.Children.Clear();
                entryManager.Panels.Clear();

                // Use EntryManager.LoadPanels which also reorders pinned-first
                entryManager.LoadPanels(entries, this, stack);
            }
            catch
            {
                // ignore load errors (corrupt file etc.), or optionally show a message
            }
        }

        private void ClearAll_Click(object? sender, RoutedEventArgs e)
        {
            var stack = this.FindControl<StackPanel>("ItemsPanel")!;
            stack.Children.Clear();
            allEntryPanels.Clear();
            entryManager.Panels.Clear();
        }

        private void Remove_Click(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Button btn && btn.Content?.ToString() == "Remove")
            {
                // The button contains the reference to the entry panel in Tag
                if (btn.Tag is StackPanel entryPanel)
                {
                    var stack = this.FindControl<StackPanel>("ItemsPanel")!;

                    // Remove from UI
                    stack.Children.Remove(entryPanel);

                    // Remove from internal list
                    allEntryPanels.Remove(entryPanel);
                    entryManager.Panels.Remove(entryPanel);
                }
            }
        }

        private void ShowInputPanel()
        {
            var inputPanel = this.FindControl<StackPanel>("InputPanel");
            inputPanel.IsVisible = true;

            // focus title box
            this.FindControl<TextBox>("TitleInputBox").Focus();
        }

        private void HideInputPanel()
        {
            var inputPanel = this.FindControl<StackPanel>("InputPanel");
            inputPanel.IsVisible = false;

            // clear fields
            var box = this.FindControl<TextBox>("TitleInputBox");
            if (box != null) box.Text = "";

            var box1= this.FindControl<TextBox>("TextInputBox");
            if (box1 != null) box1.Text = "";
        }

        private void New_Click(object? sender, RoutedEventArgs e)
        {
            ShowInputPanel();
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            HideInputPanel();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N)
            {
                ShowInputPanel();
            }
        }

    }
}
