using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace copier;

public partial class MainWindow : Window
{
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

        // Create controls
        var titleText = new TextBlock
        {
            Text = title,
            FontWeight = Avalonia.Media.FontWeight.Bold
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

        // Create panel to hold title + text + buttons
        var entryPanel = new StackPanel
        {
            Spacing = 5
        };

        // Buttons in a row
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

        stack.Children.Add(entryPanel);

        // Clipboard support
        copyButton.Click += async (_, _) =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(editableText.Text ?? "");
        };

        removeButton.Click += (_, _) =>
        {
            stack.Children.Remove(entryPanel);
        };
    }
}
