using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
            InitializeComponent();
            AppFileLogger.AddText("create Pin Initialised");

            _configPath = configPath;

            // ✅ THIS IS HOW AVALONIA WORKS
            _pinBox = this.FindControl<TextBox>("PinBox");
            AppFileLogger.AddText("Pin clicked: " + _pinBox.Text);
            _saveButton = this.FindControl<Button>("SaveButton");
            AppFileLogger.AddText("Save Button Initialised");

            _saveButton.Click += OnSaveClicked;
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
