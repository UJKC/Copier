using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using copier.Helper;
using copier.Views;

namespace copier;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configPath = CopierConfigPath.GetRootConfigPath();

            if (!File.Exists(configPath))
            {
                // No config → Create PIN
                desktop.MainWindow = new CreatePinWindow(configPath);
            }
            else
            {
                // Config exists → Ask password
                desktop.MainWindow = new PasswordWindow(configPath);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}