using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using copier.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace copier.Services;

public class EntryManager
{

    public EntryManager(List<StackPanel> allEntryPanels)
    {
        this.Panels = allEntryPanels;
    }

    public List<StackPanel> Panels { get; }

    public StackPanel CreateEntryPanel(
        string title,
        string text,
        Window window)
    {
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
            TextWrapping = TextWrapping.Wrap,
            Background = Brushes.LightGray,
            Foreground = Brushes.Black,
            BorderBrush = Brushes.Transparent,
            IsReadOnly = true,
            Focusable = false,
            IsHitTestVisible = false
        };

        var copyButton = new Button { Content = "Copy", Width = 60 };
        var editButton = new Button { Content = "Edit", Width = 60 };
        var removeButton = new Button { Content = "Remove", Width = 60 };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5
        };
        buttonPanel.Children.Add(copyButton);
        buttonPanel.Children.Add(editButton);
        buttonPanel.Children.Add(removeButton);

        var entryPanel = new StackPanel { Spacing = 5 };
        entryPanel.Children.Add(titleText);
        entryPanel.Children.Add(editableText);
        entryPanel.Children.Add(buttonPanel);

        // copy event
        copyButton.Click += async (_, _) =>
        {
            var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(editableText.Text ?? "");
        };

        // remove event handled by UI file
        removeButton.Tag = entryPanel;

        // edit toggle
        editButton.Click += (_, _) =>
        {
            if (editableText.IsReadOnly)
            {
                // enable editing
                editableText.IsReadOnly = false;
                editableText.Focusable = true;                 // <- important
                editableText.IsHitTestVisible = true;
                editableText.Background = Brushes.White;
                editableText.Foreground = Brushes.Black;

                // put caret at the end so user can continue typing
                editableText.CaretIndex = (editableText.Text ?? "").Length;

                // focus
                editableText.Focus();

                editButton.Content = "Save";
            }
            else
            {
                // disable editing (save state is the TextBox.Text)
                editableText.IsReadOnly = true;
                editableText.Focusable = false;                // <- disable again
                editableText.IsHitTestVisible = false;
                editableText.Background = Brushes.LightGray;
                editableText.Foreground = Brushes.Black;

                editButton.Content = "Edit";
            }
        };


        Panels.Add(entryPanel);
        return entryPanel;
    }

    public List<EntryData> ToEntryList() =>
        Panels.Select(panel => new EntryData
        {
            Title = (panel.Children[0] as TextBlock)?.Text ?? "",
            Text = (panel.Children[1] as TextBox)?.Text ?? ""
        }).ToList();

    public void LoadPanels(IEnumerable<EntryData> entries, Window window, Panel itemsPanel)
    {
        Panels.Clear();
        itemsPanel.Children.Clear();

        foreach (var entry in entries)
            itemsPanel.Children.Add(CreateEntryPanel(entry.Title, entry.Text, window));
    }
}
