using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using copier.Helper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace copier.Services
{
    public class UIManager
    {
        private readonly Window _window;
        private readonly EntryManager _entryManager;
        public List<StackPanel> _allEntryPanels;

        private bool _isSearchPanelOpen;
        private bool _isNewPanelOpen;

        public bool isUpdatePossible = true;
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
            AppFileLogger.AddText("Selected Panel: " + _selectedPanel);
            AppFileLogger.AddText("Is Update Possible: " + isUpdatePossible);
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

            isUpdatePossible = false;

            var inputPanel = _window.FindControl<StackPanel>("InputPanel")!;
            inputPanel.IsVisible = true;

            _window.FindControl<TextBox>("TitleInputBox")!.Focus();

            _isNewPanelOpen = true;
            _isSearchPanelOpen = false;

            SetSelectedPanelNull(_selectedPanel);

            var itemsPanel = _window.FindControl<StackPanel>("ItemsPanel");
            if (itemsPanel != null)
            {
                UpdateSelection(itemsPanel);
            }
        }

        public void HideInputPanel()
        {
            var inputPanel = _window.FindControl<StackPanel>("InputPanel")!;
            inputPanel.IsVisible = false;

            _window.FindControl<TextBox>("TitleInputBox")!.Text = "";
            _window.FindControl<TextBox>("TextInputBox")!.Text = "";

            isUpdatePossible = true;

            _isNewPanelOpen = false;

            SetSelectedPanelNull(_selectedPanel);

            var itemsPanel = _window.FindControl<StackPanel>("ItemsPanel");
            if (itemsPanel != null)
            {
                UpdateSelection(itemsPanel);
            }
        }

        // -----------------------------
        // SEARCH PANEL
        // -----------------------------

        public void ShowSearchPanel()
        {
            if (!CanSwitchPanels())
                return;

            HideInputPanel();
            AppFileLogger.AddText("Input Panel Hidden!");

            var searchPanel = _window.FindControl<StackPanel>("SearchPanel")!;
            searchPanel.IsVisible = true;

            AppFileLogger.AddText("Search Panel Visible!");

            _window.FindControl<TextBox>("SearchInputBox")!.Focus();

            AppFileLogger.AddText("Focus on Search Input Box!");

            _isSearchPanelOpen = true;
            _isNewPanelOpen = false;

            SetSelectedPanelNull(_selectedPanel);

            var itemsPanel = _window.FindControl<StackPanel>("ItemsPanel");
            if (itemsPanel != null)
            {
                UpdateSelection(itemsPanel);
            }
            AppFileLogger.AddText("Search Open Process Complete!");
            AppFileLogger.AddText("------------------------------------------");
        }

        public void HideSearchPanel()
        {
            var searchPanel = _window.FindControl<StackPanel>("SearchPanel")!;
            searchPanel.IsVisible = false;

            _isSearchPanelOpen = false;

            SetSelectedPanelNull(_selectedPanel);

            var itemsPanel = _window.FindControl<StackPanel>("ItemsPanel");
            if (itemsPanel != null)
            {
                UpdateSelection(itemsPanel);
            }
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
            AppFileLogger.AddText("Set Selectd Panel came here!");
            _selectedPanel = panel;
        }

        public void SetSelectedPanelNull(StackPanel? panel)
        {
            AppFileLogger.AddText("Set Selectd Panel came here! As it is not null making it null");
            _selectedPanel = null;
        }

        public void UpdateSelection(StackPanel itemsPanel)
        {
            AppFileLogger.AddText("Update Selection in Progress!");
            var highlightBrush = new SolidColorBrush(Color.Parse("#ADD8E6"));
            var defaultBrush = new SolidColorBrush(Colors.Transparent);

            foreach (var child in itemsPanel.Children.OfType<StackPanel>())
            {
                child.Background = (child == _selectedPanel)
                    ? highlightBrush
                    : defaultBrush;
            }

            AppFileLogger.AddText("Updated Selection!");

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
