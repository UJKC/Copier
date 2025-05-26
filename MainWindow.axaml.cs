using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
using System.Linq;
using System;


namespace copier;

public partial class MainWindow : Window
{
    private readonly List<StackPanel> allEntryPanels = new();

    private readonly string AutoSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CopierApp", "autosave.json");


    public MainWindow()
    {
        ShortcutHelper.CreateShortcutIfNeeded();

        InitializeComponent();

        AutoLoad();

        this.Closing += (_, _) => AutoSave(); // 🧠 save on exit
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        var titleBox = this.FindControl<TextBox>("TitleInputBox")!;
        var textBox = this.FindControl<TextBox>("TextInputBox")!;

        string title = titleBox.Text ?? "";
        string text = textBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text))
            return;

        AddEntry(title, text);

        titleBox.Text = "";
        textBox.Text = "";
    }

    private void AddEntry(string title, string text)
    {
        var stack = this.FindControl<StackPanel>("ItemsPanel")!;

        var titleText = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.Bold
        };

        var editableText = new TextBox
        {
            Text = text,
            AcceptsReturn = true,
            IsReadOnly = true,
            Focusable = false,
            Width = 400,
            Height = 60,
            TextWrapping = TextWrapping.Wrap,
            Background = Brushes.LightGray,            // No hover highlight
            Foreground = Brushes.Black,
            BorderBrush = Brushes.Transparent,         // No border change
            CaretBrush = Brushes.Transparent,
            IsHitTestVisible = false                   // ✅ Prevents mouse hover events
        };


        var copyButton = new Button
        {
            Content = "Copy",
            Width = 60
        };

        var editButton = new Button
        {
            Content = "Edit",
            Width = 60
        };

        var removeButton = new Button
        {
            Content = "Remove",
            Width = 60
        };

        var entryPanel = new StackPanel
        {
            Spacing = 5
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5
        };

        buttonPanel.Children.Add(copyButton);
        buttonPanel.Children.Add(editButton);
        buttonPanel.Children.Add(removeButton);

        entryPanel.Children.Add(titleText);
        entryPanel.Children.Add(editableText);
        entryPanel.Children.Add(buttonPanel);

        allEntryPanels.Add(entryPanel);
        stack.Children.Add(entryPanel);

        copyButton.Click += async (_, _) =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(editableText.Text ?? "");
        };

        removeButton.Click += (_, _) =>
        {
            allEntryPanels.Remove(entryPanel);
            stack.Children.Remove(entryPanel);
        };

        editButton.Click += (_, _) =>
        {
            if (editableText.IsReadOnly)
            {
                // Enable editing
                editableText.IsReadOnly = false;
                editableText.Background = new SolidColorBrush(Colors.White);
                editableText.Foreground = new SolidColorBrush(Colors.White);
                editableText.Focusable = true;
                editableText.Focus(); // optionally auto-focus
                editButton.Content = "Save";
            }
            else
            {
                // Disable editing
                editableText.IsReadOnly = true;
                editableText.Background = new SolidColorBrush(Colors.LightGray);
                editableText.Foreground = new SolidColorBrush(Colors.Black);
                editableText.Focusable = false;
                editButton.Content = "Edit";
            }
        };

    }


    private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
    {
        var searchBox = this.FindControl<TextBox>("SearchBox")!;
        FilterEntries(searchBox.Text);
    }

    private void FilterEntries(string? filter)
    {
        var stack = this.FindControl<StackPanel>("ItemsPanel")!;
        stack.Children.Clear();

        filter = filter?.Trim().ToLower() ?? "";

        foreach (var entry in allEntryPanels)
        {
            var titleText = entry.Children[0] as TextBlock;
            // Remove below to remove filtering editableText
            // var editableText = entry.Children[1] as TextBox;

            string title = titleText?.Text?.ToLower() ?? "";
            // Remove below to remove filtering editableText
            // string text = editableText?.Text?.ToLower() ?? "";

            // Remove second part to dosable filtering editable text
            if (title.Contains(filter))
            {
                stack.Children.Add(entry);
            }
        }
    }

    [System.Obsolete]
    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        var fileDialog = new SaveFileDialog
        {
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } }
            },
            DefaultExtension = "json"
        };

        var path = await fileDialog.ShowAsync(this);
        if (path == null) return;

        var entries = allEntryPanels.Select(panel =>
        {
            var title = (panel.Children[0] as TextBlock)?.Text ?? "";
            var text = (panel.Children[1] as TextBox)?.Text ?? "";
            return new EntryData { Title = title, Text = text };
        }).ToList();

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    [System.Obsolete]
    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        var fileDialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
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

        // Add imported ones
        foreach (var entry in entries)
        {
            AddEntry(entry.Title, entry.Text);
        }
    }

    private async void AutoSave()
    {
        var entries = allEntryPanels.Select(panel =>
        {
            var title = (panel.Children[0] as TextBlock)?.Text ?? "";
            var text = (panel.Children[1] as TextBox)?.Text ?? "";
            return new EntryData { Title = title, Text = text };
        }).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(AutoSavePath)!);

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(AutoSavePath, json);
    }

    private async void AutoLoad()
    {
        if (!File.Exists(AutoSavePath)) return;

        var json = await File.ReadAllTextAsync(AutoSavePath);
        var entries = JsonSerializer.Deserialize<List<EntryData>>(json);
        if (entries == null) return;

        var stack = this.FindControl<StackPanel>("ItemsPanel")!;
        stack.Children.Clear();
        allEntryPanels.Clear();

        foreach (var entry in entries)
            AddEntry(entry.Title, entry.Text);
    }

    private void ClearAll_Click(object? sender, RoutedEventArgs e)
    {
        var stack = this.FindControl<StackPanel>("ItemsPanel")!;
        stack.Children.Clear();
        allEntryPanels.Clear();
    }

}
