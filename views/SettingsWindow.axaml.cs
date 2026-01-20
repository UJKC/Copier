using Avalonia.Controls;
using Avalonia.Interactivity;
using copier.Helper;
using System;

namespace copier.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly string _configPath;
        private CopierConfig _config;

        public SettingsWindow(string configPath)
        {
            InitializeComponent();

            _configPath = configPath;

            // Load existing password
            _config = CopierConfigService.Load(_configPath);

            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            passwordBox.Text = _config.Password; // Prefill

            var errorText = this.FindControl<TextBlock>("ErrorText");
            errorText.Text = "";

            var saveButton = this.FindControl<Button>("SaveButton");
            saveButton.Click += OnSaveClicked;

            var backButton = this.FindControl<Button>("BackButton");
            backButton.Click += OnBackClicked;
        }

        private void OnSaveClicked(object? sender, RoutedEventArgs e)
        {
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            var errorText = this.FindControl<TextBlock>("ErrorText");

            if (string.IsNullOrWhiteSpace(passwordBox.Text))
            {
                errorText.Text = "Password cannot be empty";
                return;
            }

            try
            {
                _config.Password = passwordBox.Text;
                CopierConfigService.Save(_configPath, _config);

                errorText.Text = "Password saved!";
            }
            catch (Exception ex)
            {
                errorText.Text = "Error saving password: " + ex.Message;
            }
        }

        private void OnBackClicked(object? sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }
    }
}
