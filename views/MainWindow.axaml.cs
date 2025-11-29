using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using System;
using copier.Models;
using copier.Helper;
using copier.Services;
namespace copier.Views;

public partial class MainWindow : Window
{
    private readonly List<StackPanel> allEntryPanels = new();

    private readonly string AutoSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CopierApp", "autosave.json");
    public MainWindow()
    {
        ShortcutHelper.CreateShortcutIfNeeded();
        InitializeComponent();
        AutoLoad();
        this.Closing += (_, _) => AutoSave();
        this.AddHandler(Button.ClickEvent, Remove_Click);

        // 🧠 save on exit
    }
    private void InitializeComponent() { AvaloniaXamlLoader.Load(this); }
    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        var titleBox = this.FindControl<TextBox>("TitleInputBox")!;
        var textBox = this.FindControl<TextBox>("TextInputBox")!;

        string title = titleBox.Text ?? "";
        string text = textBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text)) return;

        AddEntry(title, text);
        titleBox.Text = "";
        textBox.Text = "";
    }

    private void AddEntry(string title, string text)
    {
        var stack = this.FindControl<StackPanel>("ItemsPanel")!;
        EntryManager EM = new EntryManager(allEntryPanels);

        // Use EntryManager to create the entry UI
        var entryPanel = EM.CreateEntryPanel(title, text, this);

        // Add to visual panel
        stack.Children.Add(entryPanel);
    }

    private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
    {
        var searchBox = this.FindControl<TextBox>("SearchBox")!;
        FilterEntries(searchBox.Text);
    }

    private void FilterEntries(string? filter)
    {
        var stack = this.FindControl<StackPanel>("ItemsPanel")!; stack.Children.Clear(); filter = filter?.Trim().ToLower() ?? ""; foreach (var entry in allEntryPanels)
        {
            var titleText = entry.Children[0] as TextBlock;
            // Remove below to remove filtering editableText
            // var editableText = entry.Children[1] as TextBox;
            string title = titleText?.Text?.ToLower() ?? "";

            // Remove below to remove filtering editableText
            // string text = editableText?.Text?.ToLower() ?? "";

            // Remove second part to dosable filtering editable text
            if (title.Contains(filter)) { stack.Children.Add(entry); }
        }
    }
    [System.Obsolete]
    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        var fileDialog = new SaveFileDialog
        {
            Filters = new List<FileDialogFilter> {
                new FileDialogFilter { Name = "JSON Files", Extensions = { "json" }
                }
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
        var fileDialog = new OpenFileDialog { AllowMultiple = false, Filters = new List<FileDialogFilter> { new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } } } }; var result = await fileDialog.ShowAsync(this); var path = result?.FirstOrDefault(); if (path == null || !File.Exists(path)) return; var json = await File.ReadAllTextAsync(path); var entries = JsonSerializer.Deserialize<List<EntryData>>(json); if (entries == null) return;

        // Clear existing entries
        var stack = this.FindControl<StackPanel>("ItemsPanel")!; stack.Children.Clear(); allEntryPanels.Clear();

        // Add imported ones
        foreach (var entry in entries) { AddEntry(entry.Title, entry.Text); }
    }
    private async void AutoSave()
    {
        EntryManager EM = new EntryManager(allEntryPanels);
        AutoSaveService AS = new AutoSaveService(AutoSavePath);

        await AS.SaveAsync(EM.ToEntryList());
    }

    private async void AutoLoad()
    {
        AutoSaveService AS = new AutoSaveService(AutoSavePath);
        EntryManager EM = new EntryManager(allEntryPanels);

        var entries = await AS.LoadAsync();
        if (entries == null) return;

        var stack = this.FindControl<StackPanel>("ItemsPanel")!;
        allEntryPanels.Clear();
        stack.Children.Clear();
        EM.Panels.Clear();

        foreach (var entry in entries)
        {
            var panel = EM.CreateEntryPanel(entry.Title, entry.Text, this);
            stack.Children.Add(panel);
        }
    }
    private void ClearAll_Click(object? sender, RoutedEventArgs e)
    {
        var stack = this.FindControl<StackPanel>("ItemsPanel")!;
        stack.Children.Clear();
        allEntryPanels.Clear();
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
            }
        }
    }

}