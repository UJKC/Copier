using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using System;
using copier.Models;
using copier.Services;
using copier.Helper;
using Avalonia.Media;

namespace copier.Views
{
    public partial class MainWindow : Window
    {
        private readonly List<StackPanel> allEntryPanels = new();
        private readonly EntryManager entryManager;
        private readonly UIManager uiManager;
        private readonly AutoSaveService autoSaveService;
        private System.Timers.Timer? _debounceTimer;
        private bool _isAutoSaveDone = false;
        private bool _isNewPanelOpen = false;
        private bool _isSearchPanelOpen = false;
        private int _selectedIndex = -1;
        private StackPanel? _selectedPanel = null;

        private readonly string AutoSavePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CopierApp", "autosave.json");

        public MainWindow()
        {
            ShortcutHelper.CreateShortcutIfNeeded();
            InitializeComponent();

            // Create the EntryManager once and reuse it (shares the allEntryPanels list)
            entryManager = new EntryManager(allEntryPanels);
            uiManager = new UIManager(this, entryManager, allEntryPanels);
            autoSaveService = new AutoSaveService(AutoSavePath, this, entryManager, allEntryPanels);

            AutoLoad();
            this.KeyUp += MainWindow_KeyUp;
            this.Closing += OnWindowClosing;
            this.AddHandler(Button.ClickEvent, Remove_Click);
        }

        private void InitializeComponent() { AvaloniaXamlLoader.Load(this); }

        private void Add_Click(object? sender, RoutedEventArgs e)
        {
            if (uiManager.AddEntryAndClose())
                    _isNewPanelOpen = false;
        }

        private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
        {
            // 🛑 Ignore ESC key – let MainWindow handle it
            if (e.Key == Key.Escape)
                return;

            var searchBox = this.FindControl<TextBox>("SearchInputBox")!;
            string text = searchBox.Text ?? "";

            // stop previous timer if typing continues
            if (_debounceTimer != null)
            {
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
            }

            _debounceTimer = new System.Timers.Timer(250); // debounce
            _debounceTimer.Elapsed += (s, _) =>
            {
                _debounceTimer?.Stop();
                _debounceTimer?.Dispose();
                _debounceTimer = null;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    FilterEntries(text);
                });
            };

            _debounceTimer.AutoReset = false;
            _debounceTimer.Start();
        }



        private void FilterEntries(string? filter)
        {
            var stack = this.FindControl<StackPanel>("ItemsPanel")!;
            stack.Children.Clear();
            filter = filter?.Trim().ToLower() ?? "";

            foreach (var entry in allEntryPanels)
            {
                var titleText = entry.Children[0] as TextBlock;
                string title = titleText?.Text?.ToLower() ?? "";

                if (title.Contains(filter))
                {
                    stack.Children.Add(entry);
                }
            }

            // If the currently selected panel is visible in the new filtered list, keep it.
            // Otherwise, select the first visible panel (if any)
            if (_selectedPanel == null || !stack.Children.Contains(_selectedPanel))
            {
                _selectedPanel = stack.Children.FirstOrDefault() as StackPanel;
            }

            UpdateSelection(stack);
        }


        [Obsolete]
        private async void Export_Click(object? sender, RoutedEventArgs e)
        {
            var fileDialog = new SaveFileDialog
            {
                Filters = new List<FileDialogFilter> {
                    new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } }
                },
                DefaultExtension = "json"
            };
            var path = await fileDialog.ShowAsync(this);
            if (path == null) return;

            // Use EntryManager to build the data list (includes pinned state)
            var entries = entryManager.ToEntryList();

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }

        [Obsolete]
        private async void Import_Click(object? sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter> {
                    new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } }
                }
            };

            var result = await fileDialog.ShowAsync(this);
            var path = result?.FirstOrDefault();
            if (path == null || !File.Exists(path)) return;

            var json = await File.ReadAllTextAsync(path);
            var entries = JsonSerializer.Deserialize<List<EntryData>>(json);
            if (entries == null) return;

            // Clear existing entries
            var stack = this.FindControl<StackPanel>("ItemsPanel")!;
            stack.Children.Clear();
            allEntryPanels.Clear();
            entryManager.Panels.Clear();

            // Add imported ones (LoadPanels will ensure pinned-first order)
            entryManager.LoadPanels(entries, this, stack);
        }

        private async void AutoLoad()
        {
            await autoSaveService.AutoLoadAsync();
        }

        private void ClearAll_Click(object? sender, RoutedEventArgs e)
        {
            var stack = this.FindControl<StackPanel>("ItemsPanel")!;
            stack.Children.Clear();
            allEntryPanels.Clear();
            entryManager.Panels.Clear();
        }

        private void Remove_Click(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Button btn && btn.Content?.ToString() == "Remove")
            {
                // The button contains the reference to the entry panel in Tag
                if (btn.Tag is StackPanel entryPanel)
                {
                    var stack = this.FindControl<StackPanel>("ItemsPanel")!;

                    // Remove from UI
                    stack.Children.Remove(entryPanel);

                    // Remove from internal list
                    allEntryPanels.Remove(entryPanel);
                    entryManager.Panels.Remove(entryPanel);
                }
            }
        }

        private void ShowInputPanel()
        {
            if (!CanSwitchPanels()) return;

            HideSearchPanel(); // ensure no conflict

            var inputPanel = this.FindControl<StackPanel>("InputPanel");
            inputPanel.IsVisible = true;

            this.FindControl<TextBox>("TitleInputBox").Focus();

            _isNewPanelOpen = true;
            _isSearchPanelOpen = false;
        }

        private void HideInputPanel()
        {
            var inputPanel = this.FindControl<StackPanel>("InputPanel");
            inputPanel.IsVisible = false;

            this.FindControl<TextBox>("TitleInputBox").Text = "";
            this.FindControl<TextBox>("TextInputBox").Text = "";

            _isNewPanelOpen = false;
        }

        private void New_Click(object? sender, RoutedEventArgs e)
        {
            ShowInputPanel();
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            HideInputPanel();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N)
            {
                ShowInputPanel();
            }
        }

        private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            await autoSaveService.AutoSaveAsync();
        }

        private void ShowSearchPanel()
        {
            if (!CanSwitchPanels()) return;

            // Ensure NewPanel is closed first
            HideInputPanel();

            var panel = this.FindControl<StackPanel>("SearchPanel");
            panel.IsVisible = true;

            _isSearchPanelOpen = true;
            _isNewPanelOpen = false;

            this.FindControl<TextBox>("SearchInputBox").Focus();
        }

        private void HideSearchPanel()
        {
            var panel = this.FindControl<StackPanel>("SearchPanel");
            panel.IsVisible = false;

            _isSearchPanelOpen = false;
        }

        private void Search_Click(object? sender, RoutedEventArgs e)
        {
            ShowSearchPanel();
        }

        private void CancelSearch_Click(object? sender, RoutedEventArgs e)
        {
            var searchBox = this.FindControl<TextBox>("SearchInputBox")!;

            // Clear the text
            searchBox.Text = "";

            // Reset the filter (show all items)
            FilterEntries("");

            _selectedIndex = -1;
            var stack = this.FindControl<StackPanel>("ItemsPanel")!;
            UpdateSelection(stack);

            // Now hide the panel
            HideSearchPanel();
        }


        private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
        {

            // CTRL + F opens search
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F)
            {
                ShowSearchPanel();
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N)
            {
                ShowInputPanel();
            }

            // ESC closes whichever is open
            if (e.Key == Key.Escape)
            {
                if (_isSearchPanelOpen)
                {
                    var searchBox = this.FindControl<TextBox>("SearchInputBox");
                    searchBox.Text = "";   // clear filter
                    FilterEntries("");     // reset list
                    HideSearchPanel();     // hide panel
                }
                else if (_isNewPanelOpen)
                {
                    HideInputPanel();
                }
            }

            var stack = this.FindControl<StackPanel>("ItemsPanel");
            if (stack.Children.Count == 0)
                return;

            if (_selectedPanel != null)
            {
                var editingBox = _selectedPanel.Children
                    .OfType<TextBox>()
                    .FirstOrDefault(tb => !tb.IsReadOnly);

                if (editingBox != null)
                {
                    // Prevent selection movement when editing
                    return;
                }
            }

            if (e.Key == Key.Down)
            {
                int currentIndex = _selectedPanel != null ? stack.Children.IndexOf(_selectedPanel) : -1;
                int nextIndex = Math.Min(currentIndex + 1, stack.Children.Count - 1);
                _selectedPanel = stack.Children[nextIndex] as StackPanel;
                UpdateSelection(stack);
            }
            else if (e.Key == Key.Up)
            {
                int currentIndex = _selectedPanel != null ? stack.Children.IndexOf(_selectedPanel) : stack.Children.Count;
                int prevIndex = Math.Max(currentIndex - 1, 0);
                _selectedPanel = stack.Children[prevIndex] as StackPanel;
                UpdateSelection(stack);
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
            {
                if (_selectedPanel != null)
                {
                    string text = "";

                    if (_selectedPanel.Children[1] is TextBox tb)
                        text = tb.Text ?? "";
                    else if (_selectedPanel.Children[1] is TextBlock tblock)
                        text = tblock.Text ?? "";

                    string combined = $"{text}";

                    // Copy to clipboard using Avalonia's IClipboard
                    this.Clipboard.SetTextAsync(combined);
                }
            }

            if (e.Key == Avalonia.Input.Key.F2 && _selectedPanel != null)
            {
                // find the editable TextBox (it is a direct child at index 1)
                var editableText = _selectedPanel.Children.OfType<TextBox>().FirstOrDefault();

                // find any Panel child (WrapPanel) that contains the buttons, then pick the Edit/Save button
                var buttonsContainer = _selectedPanel.Children.OfType<Panel>().FirstOrDefault(); // this will match the WrapPanel
                Button? editButton = null;
                if (buttonsContainer != null)
                {
                    editButton = buttonsContainer.Children
                        .OfType<Button>()
                        .FirstOrDefault(b => (b.Content?.ToString() == "Edit") || (b.Content?.ToString() == "Save"));
                }

                if (editableText != null && editButton != null)
                {
                    // Only *start* editing on F2 (don't toggle to Save). Shift+Enter will handle saving.
                    if (editableText.IsReadOnly)
                    {
                        editableText.IsReadOnly = false;
                        editableText.Focusable = true;
                        editableText.IsHitTestVisible = true;
                        editableText.Background = Brushes.White;
                        editableText.Foreground = Brushes.Black;

                        // put caret at the end after focus
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

        private bool CanSwitchPanels()
        {
            var titleBox = this.FindControl<TextBox>("TitleInputBox");
            var textBox = this.FindControl<TextBox>("TextInputBox");
            var searchBox = this.FindControl<TextBox>("SearchInputBox");

            bool inputHasText = !string.IsNullOrWhiteSpace(titleBox.Text)
                                || !string.IsNullOrWhiteSpace(textBox.Text);

            bool searchHasText = !string.IsNullOrWhiteSpace(searchBox.Text);

            // If New panel is open AND user typed something → cannot switch
            if (_isNewPanelOpen && inputHasText)
                return false;

            // If Search panel is open AND user typed something → cannot switch
            if (_isSearchPanelOpen && searchHasText)
                return false;

            return true;
        }

        private void UpdateSelection(StackPanel itemsPanel)
        {
            var highlightBrush = new SolidColorBrush(Color.Parse("#ADD8E6")); // Light blue
            var defaultBrush = new SolidColorBrush(Colors.Transparent);

            foreach (var child in itemsPanel.Children.OfType<StackPanel>())
            {
                child.Background = (child == _selectedPanel) ? highlightBrush : defaultBrush;
            }

            _selectedPanel?.BringIntoView();
        }

    }
}
