using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Linq;
using System.Threading.Tasks;
using copier.Views;
using copier.Services;
using System;

namespace copier.Services
{
    public class KeyboardManager
    {
        private readonly UIManager _uiManager;
        private readonly SearchService _searchService;
        private readonly Window _window;

        public KeyboardManager(Window window, UIManager uiManager, SearchService searchService)
        {
            _window = window;
            _uiManager = uiManager;
            _searchService = searchService;
        }

        public async void HandleKeyUp(object? sender, KeyEventArgs e)
        {
            var stack = _window.FindControl<StackPanel>("ItemsPanel");
            if (stack == null || stack.Children.Count == 0)
                return;

            var selected = _uiManager.SelectedPanel;

            // CTRL + F opens search
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F)
            {
                _uiManager.ShowSearchPanel();
                return;
            }

            // CTRL + N opens input panel
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N)
            {
                _uiManager.ShowInputPanel();
                return;
            }

            // ESC closes whichever panel is open
            if (e.Key == Key.Escape)
            {
                HandleEscape();
                return;
            }

            // Prevent movement if editing
            if (selected != null)
            {
                var editingBox = selected.Children
                    .OfType<TextBox>()
                    .FirstOrDefault(tb => !tb.IsReadOnly);
                if (editingBox != null) return;
            }

            // Arrow navigation
            if (e.Key == Key.Down)
            {
                MoveSelection(stack, 1);
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(stack, -1);
            }

            // CTRL + C copies content
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
            {
                CopySelectedText(selected);
            }

            // F2 edits the selected entry
            if (e.Key == Key.F2 && selected != null)
            {
                EditSelectedEntry(selected);
            }
        }

        private void HandleEscape()
        {
            if (_uiManager.IsSearchPanelOpen)
            {
                var searchBox = _window.FindControl<TextBox>("SearchInputBox")!;
                searchBox.Text = "";
                _searchService.FilterEntries("");
                _uiManager.HideSearchPanel();
            }
            else if (_uiManager.IsNewPanelOpen)
            {
                _uiManager.HideInputPanel();
            }
        }

        private void MoveSelection(StackPanel stack, int direction)
        {
            int currentIndex = _uiManager.SelectedPanel != null
                ? stack.Children.IndexOf(_uiManager.SelectedPanel)
                : (direction > 0 ? -1 : stack.Children.Count);

            int nextIndex = direction > 0
                ? Math.Min(currentIndex + 1, stack.Children.Count - 1)
                : Math.Max(currentIndex + direction, 0);

            _uiManager.SetSelectedPanel(stack.Children[nextIndex] as StackPanel);
            _uiManager.UpdateSelection(stack);
        }

        private void CopySelectedText(StackPanel? selected)
        {
            if (selected == null) return;

            string text = "";
            if (selected.Children[1] is TextBox tb)
                text = tb.Text ?? "";
            else if (selected.Children[1] is TextBlock tblock)
                text = tblock.Text ?? "";

            _window.Clipboard.SetTextAsync(text);
        }

        private void EditSelectedEntry(StackPanel selected)
        {
            var editableText = selected.Children.OfType<TextBox>().FirstOrDefault();
            var buttonsContainer = selected.Children.OfType<Panel>().FirstOrDefault();
            Button? editButton = buttonsContainer?.Children
                .OfType<Button>()
                .FirstOrDefault(b => (b.Content?.ToString() == "Edit") || (b.Content?.ToString() == "Save"));

            if (editableText != null && editButton != null && editableText.IsReadOnly)
            {
                editableText.IsReadOnly = false;
                editableText.Focusable = true;
                editableText.IsHitTestVisible = true;
                editableText.Background = Brushes.White;
                editableText.Foreground = Brushes.Black;

                editableText.Focus();
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    editableText.CaretIndex = (editableText.Text ?? "").Length;
                });

                editButton.Content = "Save";
            }
        }
    }
}
