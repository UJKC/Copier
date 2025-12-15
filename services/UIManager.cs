using Avalonia.Controls;
using copier.Services;
using System.Collections.Generic;

namespace copier.Services
{
    public class UIManager
    {
        private readonly Window _window;
        private readonly EntryManager _entryManager;
        private readonly List<StackPanel> _allEntryPanels;

        public UIManager(
            Window window,
            EntryManager entryManager,
            List<StackPanel> allEntryPanels)
        {
            _window = window;
            _entryManager = entryManager;
            _allEntryPanels = allEntryPanels;
        }

        private void HideInputPanel()
        {
            var inputPanel = _window.FindControl<StackPanel>("InputPanel")!;
            inputPanel.IsVisible = false;

            _window.FindControl<TextBox>("TitleInputBox")!.Text = "";
            _window.FindControl<TextBox>("TextInputBox")!.Text = "";
        }

        public bool AddEntryAndClose()
        {
            var titleBox = _window.FindControl<TextBox>("TitleInputBox")!;
            var textBox  = _window.FindControl<TextBox>("TextInputBox")!;

            string title = titleBox.Text ?? "";
            string text  = textBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(text))
                return false;

            var itemsPanel = _window.FindControl<StackPanel>("ItemsPanel")!;

            var entryPanel = _entryManager.CreateEntryPanel(title, text, _window);
            itemsPanel.Children.Add(entryPanel);

            _entryManager.ReorderPanels(itemsPanel);

            HideInputPanel();
            return true;
        }

    }
}
