using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace copier.Services
{
    public class UIManager
    {
        private readonly Window _window;
        private readonly EntryManager _entryManager;
        private readonly List<StackPanel> _allEntryPanels;

        private bool _isSearchPanelOpen;
        private bool _isNewPanelOpen;
        private StackPanel? _selectedPanel;

        public bool IsSearchPanelOpen => _isSearchPanelOpen;
        public bool IsNewPanelOpen => _isNewPanelOpen;
        public StackPanel? SelectedPanel => _selectedPanel;

        public UIManager(
            Window window,
            EntryManager entryManager,
            List<StackPanel> allEntryPanels)
        {
            _window = window;
            _entryManager = entryManager;
            _allEntryPanels = allEntryPanels;
        }

        public void SetSearchPanelOpen(bool value)
        {
            _isSearchPanelOpen = value;
        }

        public void SetNewPanelOpen(bool value)
        {
            _isNewPanelOpen = value;
        }


        // -----------------------------
        // INPUT PANEL
        // -----------------------------

        public void ShowInputPanel()
        {
            if (!CanSwitchPanels())
                return;

            HideSearchPanel();

            var inputPanel = _window.FindControl<StackPanel>("InputPanel")!;
            inputPanel.IsVisible = true;

            _window.FindControl<TextBox>("TitleInputBox")!.Focus();

            _isNewPanelOpen = true;
            _isSearchPanelOpen = false;
        }

        public void HideInputPanel()
        {
            var inputPanel = _window.FindControl<StackPanel>("InputPanel")!;
            inputPanel.IsVisible = false;

            _window.FindControl<TextBox>("TitleInputBox")!.Text = "";
            _window.FindControl<TextBox>("TextInputBox")!.Text = "";

            _isNewPanelOpen = false;
        }

        // -----------------------------
        // SEARCH PANEL
        // -----------------------------

        public void ShowSearchPanel()
        {
            if (!CanSwitchPanels())
                return;

            HideInputPanel();

            var searchPanel = _window.FindControl<StackPanel>("SearchPanel")!;
            searchPanel.IsVisible = true;

            _window.FindControl<TextBox>("SearchInputBox")!.Focus();

            _isSearchPanelOpen = true;
            _isNewPanelOpen = false;
        }

        public void HideSearchPanel()
        {
            var searchPanel = _window.FindControl<StackPanel>("SearchPanel")!;
            searchPanel.IsVisible = false;

            _isSearchPanelOpen = false;
        }

        // -----------------------------
        // PANEL SWITCH GUARD
        // -----------------------------

        public bool CanSwitchPanels()
        {
            var titleBox = _window.FindControl<TextBox>("TitleInputBox")!;
            var textBox = _window.FindControl<TextBox>("TextInputBox")!;
            var searchBox = _window.FindControl<TextBox>("SearchInputBox")!;

            bool inputHasText =
                !string.IsNullOrWhiteSpace(titleBox.Text) ||
                !string.IsNullOrWhiteSpace(textBox.Text);

            bool searchHasText =
                !string.IsNullOrWhiteSpace(searchBox.Text);

            if (_isNewPanelOpen && inputHasText)
                return false;

            if (_isSearchPanelOpen && searchHasText)
                return false;

            return true;
        }

        // -----------------------------
        // SELECTION
        // -----------------------------

        public void SetSelectedPanel(StackPanel? panel)
        {
            _selectedPanel = panel;
        }

        public void UpdateSelection(StackPanel itemsPanel)
        {
            var highlightBrush = new SolidColorBrush(Color.Parse("#ADD8E6"));
            var defaultBrush = new SolidColorBrush(Colors.Transparent);

            foreach (var child in itemsPanel.Children.OfType<StackPanel>())
            {
                child.Background = (child == _selectedPanel)
                    ? highlightBrush
                    : defaultBrush;
            }

            _selectedPanel?.BringIntoView();
        }

        // -----------------------------
        // KEEP AS IS (UNCHANGED)
        // -----------------------------

        public bool AddEntryAndClose()
        {
            var titleBox = _window.FindControl<TextBox>("TitleInputBox")!;
            var textBox = _window.FindControl<TextBox>("TextInputBox")!;

            string title = titleBox.Text ?? "";
            string text = textBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(text))
                return false;

            var itemsPanel = _window.FindControl<StackPanel>("ItemsPanel")!;

            var entryPanel = _entryManager.CreateEntryPanel(title, text, _window);
            itemsPanel.Children.Add(entryPanel);

            _entryManager.ReorderPanels(itemsPanel);

            HideInputPanel();
            return true;
        }

        public void ClearSelection()
        {
            if (_selectedPanel == null)
                return;

            // Optional: remove visual highlight
            _selectedPanel.Classes.Remove("selected");

            _selectedPanel = null;
        }

    }
}
