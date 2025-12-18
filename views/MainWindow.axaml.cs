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
        private readonly KeyboardManager _keyboardManager;
        private System.Timers.Timer? _debounceTimer;
        private bool _isAutoSaveDone = false;

        private readonly string AutoSavePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CopierApp", "autosave.json");

        public MainWindow()
        {
            ShortcutHelper.CreateShortcutIfNeeded();
            InitializeComponent();
            AppFileLogger.AddText("Hello");

            // Create the EntryManager once and reuse it (shares the allEntryPanels list)
            entryManager = new EntryManager(allEntryPanels);
            uiManager = new UIManager(this, entryManager, allEntryPanels);
            autoSaveService = new AutoSaveService(AutoSavePath, this, entryManager, allEntryPanels);
            searchService = new SearchService(this, allEntryPanels, uiManager.CanSwitchPanels, uiManager.HideInputPanel, uiManager.HideSearchPanel, uiManager.SetSearchPanelOpen,
                uiManager.SetNewPanelOpen
            );

            AutoLoad();
            _keyboardManager = new KeyboardManager(this, uiManager, searchService, entryManager);
            this.KeyUp += _keyboardManager.HandleKeyUp;
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
                    uiManager.ClearSelection();
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

    }
}
