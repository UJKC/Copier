using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using copier.Helper;

namespace copier.Views
{
    public partial class CreatePinWindow : Window
    {
        private readonly string _configPath = CopierConfigPath.GetRootConfigPath();

        private TextBox _pinBox;
        private Button _saveButton;

        public CreatePinWindow(string configPath)
        {
            ShortcutHelper.CreateShortcutIfNeeded();
            InitializeComponent();
            AppFileLogger.AddText("create Pin Initialised");

            _configPath = configPath;

            // ✅ THIS IS HOW AVALONIA WORKS
            _pinBox = this.FindControl<TextBox>("PinBox");
            Dispatcher.UIThread.Post(() =>
            {
                _pinBox.Focus();
                _pinBox.CaretIndex = _pinBox.Text?.Length ?? 0;
            });
            AppFileLogger.AddText("Pin clicked: " + _pinBox.Text);
            _saveButton = this.FindControl<Button>("SaveButton");
            AppFileLogger.AddText("Save Button Initialised");

            _saveButton.Click += OnSaveClicked;
            _pinBox.KeyUp += PinBox_KeyUp;
        }

        private void PinBox_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnSaveClicked(sender, e);
            }
        }

        private void OnSaveClicked(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_pinBox.Text))
            {
                AppFileLogger.AddText("Empty Text");
                return;
            }

            try
            {
                var config = new CopierConfig
                {
                    Password = _pinBox.Text
                };
                AppFileLogger.AddText("New Copier Config Object");

                CopierConfigService.Save(_configPath, config);
                AppFileLogger.AddText("Saved the path");

                new PasswordWindow(_configPath).Show();
                AppFileLogger.AddText("Password Window Open");

                Close();
            }
            catch (Exception ex)
            {
                AppFileLogger.AddText("ERROR: " + ex.Message);
            }
        }


        // ✅ REQUIRED IN AVALONIA
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
