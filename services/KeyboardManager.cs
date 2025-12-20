using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Linq;
using System.Threading.Tasks;
using copier.Views;
using copier.Services;
using System;
using copier.Helper;

namespace copier.Services
{
    public class KeyboardManager
    {
        private readonly UIManager _uiManager;
        private readonly SearchService _searchService;
        private readonly EntryManager _entryManager;
        private readonly AutoSaveService _autoSaveService;
        private readonly Window _window;

        public KeyboardManager(Window window, UIManager uiManager, SearchService searchService, EntryManager entryManager, AutoSaveService autoSaveService)
        {
            _window = window;
            _uiManager = uiManager;
            _searchService = searchService;
            _entryManager = entryManager;
            _autoSaveService = autoSaveService;
        }

        [Obsolete]
        public async void HandleKeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key is Key.LeftCtrl or Key.RightCtrl or
              Key.LeftShift or Key.RightShift or
              Key.LeftAlt or Key.RightAlt)
            {
                return;
            }
            AppFileLogger.AddText("Handling Key Up!");
            var stack = _window.FindControl<StackPanel>("ItemsPanel");
            if (_entryManager.Panels.Count == 0)
            {
                _uiManager.SetSelectedPanelNull(_uiManager.SelectedPanel);
                _uiManager.ShowInputPanel();
                return;
            }
            var selected = _uiManager.SelectedPanel;
            AppFileLogger.AddText("Var Selected: " + selected);

            AppFileLogger.AddText("Here1");

            // CTRL + F opens search and No need of Selection
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F)
            {
                AppFileLogger.AddText("Ctrl + F typed");
                if (_uiManager.SelectedPanel == null)
                {
                    _uiManager.SetSelectedPanelNull(_uiManager.SelectedPanel);
                    AppFileLogger.AddText("After making it null I came here!");
                }
                _uiManager.ShowSearchPanel();
                AppFileLogger.AddText("Selected Panel: " + _uiManager.SelectedPanel);
                return;
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.E)
            {
                AppFileLogger.AddText("Export");
                Export_Click();
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.I)
            {
                AppFileLogger.AddText("Import");
                Import_Click();
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.X)
            {
                AppFileLogger.AddText("Clear All");
                ClearAll_Click();
            }

            AppFileLogger.AddText("Here2");

            // CTRL + N opens input panel and no need of selection
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N)
            {
                AppFileLogger.AddText("Ctrl + N clicked!");
                _uiManager.ShowInputPanel();
                return;
            }

            AppFileLogger.AddText("Here3");

            // ESC closes whichever panel is open and no need of selection
            if (e.Key == Key.Escape)
            {
                HandleEscape();
                return;
            }

            AppFileLogger.AddText("Here4");

            // Selection Needed
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.S)
            {
                if (selected != null)
                {
                    SaveSelectedEntry(selected);
                    e.Handled = true;
                }
                return;
            }

            AppFileLogger.AddText("Here5");

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.P)
            {
                if (selected != null)
                {
                    TogglePinSelectedEntry(selected);
                    e.Handled = true;
                }
                return;
            }

            AppFileLogger.AddText("Here6");

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.D)
            {
                if (selected != null)
                {
                    RemoveSelectedEntry(selected);
                    e.Handled = true;
                }
                return;
            }

            AppFileLogger.AddText("Here7");

            // Prevent movement if editing
            if (selected != null)
            {
                var editingBox = selected.Children
                    .OfType<TextBox>()
                    .FirstOrDefault(tb => !tb.IsReadOnly);
                if (editingBox != null) return;
            }

            AppFileLogger.AddText("Here8");

            // Arrow navigation
            if (e.Key == Key.Down)
            {
                AppFileLogger.AddText("Down key pressed");
                if (_uiManager.isUpdatePossible == true)
                    MoveSelection(stack, 1);
            }
            else if (e.Key == Key.Up)
            {
                AppFileLogger.AddText("Down Up pressed");
                if (_uiManager.isUpdatePossible == true)
                    MoveSelection(stack, -1);
            }

            AppFileLogger.AddText("Here9");

            // CTRL + C copies content
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
            {
                CopySelectedText(selected);
            }

            AppFileLogger.AddText("Here10");

            // F2 edits the selected entry
            if (e.Key == Key.F2 && selected != null)
            {
                AppFileLogger.AddText("Here11");
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
            {
                AppFileLogger.AddText("No panels");
                return;
            }

            int currentIndex = _uiManager.SelectedPanel != null
                ? stack.Children.IndexOf(_uiManager.SelectedPanel)
                : (direction > 0 ? -1 : stack.Children.Count);

            AppFileLogger.AddText("Current Index: " + currentIndex);

            int nextIndex = direction > 0
                ? Math.Min(currentIndex + 1, stack.Children.Count - 1)
                : Math.Max(currentIndex - 1, 0);

            AppFileLogger.AddText("Next Index: " + nextIndex);

            var nextPanel = stack.Children[nextIndex] as StackPanel;

            if (nextPanel == null)
            {
                AppFileLogger.AddText("Next Panel is null");
            }

            if (nextPanel != null && nextPanel != _uiManager.SelectedPanel)
            {
                _uiManager.SetSelectedPanel(nextPanel);
                currentIndex = _uiManager.SelectedPanel != null ? stack.Children.IndexOf(_uiManager.SelectedPanel) : (direction > 0 ? -1 : stack.Children.Count);
                AppFileLogger.AddText("Current Panel: " + currentIndex);
                _uiManager.UpdateSelection(stack);
                AppFileLogger.AddText("Updated Panels");
                AppFileLogger.AddText("-------------------------------------------------------------------");
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

        [Obsolete]
        private async void Export_Click()
        {
            await _autoSaveService.ExportAsync();
        }

        [Obsolete]
        private async void Import_Click()
        {
            await _autoSaveService.ImportAsync();
        }

        private void ClearAll_Click()
        {
            var stack = _window.FindControl<StackPanel>("ItemsPanel")!;
            stack.Children.Clear();
            _entryManager.Panels.Clear();
        }
    }
}
