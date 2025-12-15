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
        private readonly SearchService searchService;
        private System.Timers.Timer? _debounceTimer;
        private bool _isAutoSaveDone = false;

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
            searchService = new SearchService(
                this,
                allEntryPanels,
                uiManager.CanSwitchPanels,
                uiManager.HideInputPanel,
                uiManager.HideSearchPanel,
                uiManager.SetSearchPanelOpen,
                uiManager.SetNewPanelOpen
            );

            AutoLoad();
            this.KeyUp += MainWindow_KeyUp;
            this.Closing += OnWindowClosing;
            this.AddHandler(Button.ClickEvent, Remove_Click);
        }

        private void InitializeComponent() { AvaloniaXamlLoader.Load(this); }

        private void Add_Click(object? sender, RoutedEventArgs e)
        {
            if (uiManager.AddEntryAndClose())
                uiManager.SetNewPanelOpen(false);
        }

        private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
        {
            searchService.SearchBox_KeyUp(sender, e);
        }


        [Obsolete]
        private async void Export_Click(object? sender, RoutedEventArgs e)
        {
            await autoSaveService.ExportAsync();
        }

        [Obsolete]
        private async void Import_Click(object? sender, RoutedEventArgs e)
        {
            await autoSaveService.ImportAsync();
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

        private void New_Click(object? sender, RoutedEventArgs e)
        {
            uiManager.ShowInputPanel();
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            uiManager.HideInputPanel();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N)
            {
                uiManager.ShowInputPanel();
            }
        }

        private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            await autoSaveService.AutoSaveAsync();
        }

        private void Search_Click(object? sender, RoutedEventArgs e)
        {
            uiManager.ShowSearchPanel();
        }

        private void CancelSearch_Click(object? sender, RoutedEventArgs e)
        {
            searchService.CancelSearch_Click(sender, e);
        }


        private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
        {
            var selected = uiManager.SelectedPanel;

            // CTRL + F opens search
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F)
            {
                uiManager.ShowSearchPanel();
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N)
            {
                uiManager.ShowInputPanel();
            }

            // ESC closes whichever is open
            if (e.Key == Key.Escape)
            {
                if (uiManager.IsSearchPanelOpen)
                {
                    var searchBox = this.FindControl<TextBox>("SearchInputBox")!;
                    searchBox.Text = "";
                    searchService.FilterEntries("");
                    uiManager.HideSearchPanel();
                }
                else if (uiManager.IsNewPanelOpen)
                {
                    uiManager.HideInputPanel();
                }
            }


            var stack = this.FindControl<StackPanel>("ItemsPanel");
            if (stack.Children.Count == 0)
                return;

            if (uiManager.SelectedPanel != null)
            {
                var editingBox = uiManager.SelectedPanel.Children
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
                int currentIndex = uiManager.SelectedPanel != null ? stack.Children.IndexOf(uiManager.SelectedPanel) : -1;
                int nextIndex = Math.Min(currentIndex + 1, stack.Children.Count - 1);
                uiManager.SetSelectedPanel(stack.Children[nextIndex] as StackPanel);
                uiManager.UpdateSelection(stack);
            }
            else if (e.Key == Key.Up)
            {
                int currentIndex = uiManager.SelectedPanel != null ? stack.Children.IndexOf(uiManager.SelectedPanel) : stack.Children.Count;
                int prevIndex = Math.Max(currentIndex - 1, 0);
                uiManager.SetSelectedPanel(stack.Children[prevIndex] as StackPanel);
                uiManager.UpdateSelection(stack);
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
            {
                if (selected != null)
                {
                    string text = "";

                    if (selected.Children[1] is TextBox tb)
                        text = tb.Text ?? "";
                    else if (selected.Children[1] is TextBlock tblock)
                        text = tblock.Text ?? "";

                    string combined = $"{text}";

                    // Copy to clipboard using Avalonia's IClipboard
                    this.Clipboard.SetTextAsync(combined);
                }
            }

            if (e.Key == Avalonia.Input.Key.F2 && selected != null)
            {
                // find the editable TextBox (it is a direct child at index 1)
                var editableText = selected.Children.OfType<TextBox>().FirstOrDefault();

                // find any Panel child (WrapPanel) that contains the buttons, then pick the Edit/Save button
                var buttonsContainer = selected.Children.OfType<Panel>().FirstOrDefault(); // this will match the WrapPanel
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

    }
}
