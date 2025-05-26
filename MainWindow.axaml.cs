using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;

namespace copier;

public partial class MainWindow : Window
{
    private readonly List<StackPanel> allEntryPanels = new();

    public MainWindow()
    {
        InitializeComponent();
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
            Width = 400,
            Height = 60,
            TextWrapping = TextWrapping.Wrap
        };

        var copyButton = new Button
        {
            Content = "Copy",
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
}
