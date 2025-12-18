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
        private readonly EntryManager _entryManager;
        private readonly Window _window;

        public KeyboardManager(Window window, UIManager uiManager, SearchService searchService, EntryManager entryManager)
        {
            _window = window;
            _uiManager = uiManager;
            _searchService = searchService;
            _entryManager = entryManager;
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

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.S)
            {
                if (selected != null)
                {
                    SaveSelectedEntry(selected);
                    e.Handled = true;
                }
                return;
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.P)
            {
                if (selected != null)
                {
                    TogglePinSelectedEntry(selected);
                    e.Handled = true;
                }
                return;
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.D)
            {
                if (selected != null)
                {
                    RemoveSelectedEntry(selected);
                    e.Handled = true;
                }
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

                // Pass the currently selected panel
                _searchService.FilterEntries("", _uiManager.SelectedPanel);

                _uiManager.HideSearchPanel();
            }
            else if (_uiManager.IsNewPanelOpen)
            {
                _uiManager.HideInputPanel();
            }
        }


        private void MoveSelection(StackPanel stack, int direction)
        {
            if (stack.Children.Count == 0)
                return;

            int currentIndex = _uiManager.SelectedPanel != null
                ? stack.Children.IndexOf(_uiManager.SelectedPanel)
                : (direction > 0 ? -1 : stack.Children.Count);

            int nextIndex = direction > 0
                ? Math.Min(currentIndex + 1, stack.Children.Count - 1)
                : Math.Max(currentIndex - 1, 0);

            var nextPanel = stack.Children[nextIndex] as StackPanel;

            if (nextPanel != null && nextPanel != _uiManager.SelectedPanel)
            {
                _uiManager.SetSelectedPanel(nextPanel);
                _uiManager.UpdateSelection(stack);
            }
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

        private void SaveSelectedEntry(StackPanel selected)
        {
            var editableText = selected.Children
                .OfType<TextBox>()
                .FirstOrDefault(tb => !tb.IsReadOnly);

            if (editableText == null)
                return;

            var buttonsContainer = selected.Children.OfType<Panel>().FirstOrDefault();
            Button? editButton = buttonsContainer?.Children
                .OfType<Button>()
                .FirstOrDefault(b =>
                    b.Content?.ToString() == "Save" ||
                    b.Content?.ToString() == "Edit");

            // Lock textbox
            editableText.IsReadOnly = true;
            editableText.Focusable = false;
            editableText.IsHitTestVisible = false;
            editableText.Background = Brushes.LightGray;
            editableText.Foreground = Brushes.Black;

            TopLevel.GetTopLevel(editableText)?
                .FocusManager?
                .ClearFocus();

            if (editButton != null)
                editButton.Content = "Edit";
        }

        private void TogglePinSelectedEntry(StackPanel selected)
        {
            if (selected.Tag is not EntryPanelState state)
                return;

            // Toggle pinned state
            state.IsPinned = !state.IsPinned;

            // Update button text
            var buttonPanel = selected.Children.OfType<Panel>().FirstOrDefault();
            var pinButton = buttonPanel?.Children
                .OfType<Button>()
                .FirstOrDefault(b =>
                    b.Content?.ToString() == "Pin" ||
                    b.Content?.ToString() == "Unpin");

            if (pinButton != null)
                pinButton.Content = state.IsPinned ? "Unpin" : "Pin";

            // 🔥 Delegate reorder to EntryManager
            var parent = selected.Parent as Panel;
            if (parent != null)
                _entryManager.ReorderPanels(parent);

            // Clear search box
            var searchBox = _window.FindControl<TextBox>("SearchInputBox");
            if (searchBox != null)
                searchBox.Text = "";
        }

        private void RemoveSelectedEntry(StackPanel selected)
        {
            var parent = selected.Parent as StackPanel;
            if (parent == null)
                return;

            // Remove from UI
            parent.Children.Remove(selected);

            // Remove from EntryManager list
            _entryManager.Panels.Remove(selected);

            // Clear selection
            _uiManager.ClearSelection();
        }


    }
}
