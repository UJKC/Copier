using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using copier.Helper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace copier.Services
{
    public class SearchService
    {
        private readonly Window _window;
        private readonly List<StackPanel> _allEntryPanels;

        private readonly Func<bool> _canSwitchPanels;
        private readonly Action _hideInputPanel;
        private readonly Action<bool> _setSearchPanelOpen;
        private readonly Action<bool> _setNewPanelOpen;

        private System.Timers.Timer? _debounceTimer;

        private readonly Action _hideSearchPanel;

        private readonly UIManager _uiManager;

        public SearchService(
            Window window,
            List<StackPanel> allEntryPanels,
            UIManager uiManager,
            Func<bool> canSwitchPanels,
            Action hideInputPanel,
            Action hideSearchPanel,
            Action<bool> setSearchPanelOpen,
            Action<bool> setNewPanelOpen)
        {
            _window = window;
            _allEntryPanels = allEntryPanels;
            _uiManager = uiManager;
            _canSwitchPanels = canSwitchPanels;
            _hideInputPanel = hideInputPanel;
            _hideSearchPanel = hideSearchPanel;
            _setSearchPanelOpen = setSearchPanelOpen;
            _setNewPanelOpen = setNewPanelOpen;
        }

        // -----------------------------
        // Search typing (debounced)
        // -----------------------------
        public void SearchBox_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                return;


            AppFileLogger.AddText("Searching!!");
            var searchBox = _window.FindControl<TextBox>("SearchInputBox")!;
            string text = searchBox.Text ?? "";

            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();

            _debounceTimer = new System.Timers.Timer(250)
            {
                AutoReset = false
            };

            _debounceTimer.Elapsed += (_, _) =>
            {
                AppFileLogger.AddText("Searching afte Debounce");
                AppFileLogger.AddText("Text: " + text);
                Dispatcher.UIThread.Post(() => FilterEntries(text));
            };

            _debounceTimer.Start();
        }

        // -----------------------------
        // Filter entries
        // -----------------------------
        public void FilterEntries(string? filter, StackPanel? currentlySelected = null)
        {
            AppFileLogger.AddText("Filte Process Started");
            var itemsPanel = _window.FindControl<StackPanel>("ItemsPanel")!;
            itemsPanel.Children.Clear();

            filter = filter?.Trim().ToLower() ?? "";

            foreach (var entry in _allEntryPanels)
            {
                if (entry.Children[0] is TextBlock title &&
                    (title.Text ?? "").ToLower().Contains(filter))
                {
                    itemsPanel.Children.Add(entry);
                }
            }
        }

        // -----------------------------
        // Cancel search
        // -----------------------------
        public void CancelSearch_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var searchBox = _window.FindControl<TextBox>("SearchInputBox")!;
            searchBox.Text = "";

            FilterEntries("");
            _hideSearchPanel();
        }
    }
}
