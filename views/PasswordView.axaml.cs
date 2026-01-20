using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.Input;

namespace copier.Views
{
    public partial class PasswordWindow : Window
    {
        private readonly string _storedPassword;
        private int _failedAttempts = 0;
        private const int MaxAttempts = 3;

        public PasswordWindow(string configPath)
        {
            InitializeComponent();

            var _passwordBox = this.FindControl<TextBox>("PasswordBox");
            Dispatcher.UIThread.Post(() =>
            {
                _passwordBox.Focus();
                _passwordBox.CaretIndex = _passwordBox.Text?.Length ?? 0;
            });

            var config = CopierConfigService.Load(configPath);
            _storedPassword = config.Password;

            LoginButton.Click += OnLoginClicked;
            _passwordBox.KeyUp += PasswordBox_KeyUp;
        }

        private void OnLoginClicked(object? sender, RoutedEventArgs e)
        {
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            var errorText = this.FindControl<TextBlock>("ErrorText");

            if (string.IsNullOrWhiteSpace(passwordBox.Text))
            {
                errorText.Text = "Please enter a password";
                return;
            }

            if (passwordBox.Text != _storedPassword)
            {
                _failedAttempts++;

                if (_failedAttempts >= MaxAttempts)
                {
                    errorText.Text = "Too many failed attempts. Exiting...";

                    if (Application.Current?.ApplicationLifetime
                        is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.Shutdown();
                    }

                    return;
                }

                errorText.Text = $"Incorrect password ({_failedAttempts}/{MaxAttempts})";
                passwordBox.Text = "";
                return;
            }

            // ✅ Correct password
            var mainWindow = new MainWindow();
            mainWindow.Show();

            Close();
        }

        private void PasswordBox_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnLoginClicked(sender, e);
            }
        }
    }
}
