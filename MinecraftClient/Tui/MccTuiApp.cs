using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Consolonia.Themes;

namespace MinecraftClient.Tui
{
    public class MccTuiApp : Application
    {
        public override void Initialize()
        {
            Styles.Add(new ModernTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var view = new MainTuiView();
                TuiConsoleBackend.Instance?.SetView(view);

                desktop.MainWindow = new Window
                {
                    Content = view,
                    Title = "Minecraft Console Client",
                    Background = Brushes.Black,
                    Padding = new Thickness(0),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
