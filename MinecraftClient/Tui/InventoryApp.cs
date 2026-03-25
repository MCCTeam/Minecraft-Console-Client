using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Consolonia.Themes;

namespace MinecraftClient.Tui
{
    public class InventoryApp : Application
    {
        public override void Initialize()
        {
            Styles.Add(new ModernTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Window
                {
                    Content = new InventoryMainView(),
                    Title = "MCC Inventory"
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
