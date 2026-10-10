using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Consolonia.ManagedWindows;
using Consolonia.Themes;
using Iciclecreek.Avalonia.WindowManager;

namespace Mcc.Cli.Tui.Hosting;

/// <summary>
/// The Consolonia <see cref="Application"/> for the TUI backend.
/// Bootstrapped by <see cref="TuiBackend"/> via <c>AppBuilder.Configure&lt;MccTuiApp&gt;().UseConsolonia()</c>.
/// On framework init it builds the single <see cref="MainTuiView"/>, hands it to the backend, and installs it as the window content.
/// No XAML (EnableAvaloniaCompilationByDefault is off in the csproj).
/// </summary>
public sealed class MccTuiApp : Application
{
    static MccTuiApp()
    {
        // Match the Consolonia Gallery setup: initialise the manager before app styles are composed so WindowsPanel does not inject a competing implicit style later in startup.
        _ = new WindowsPanel();
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        Styles.Add(new ModernTheme());
        Styles.Add(new AutoManagedWindowStyles());
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            TuiBackend? backend = TuiBackend.Instance;
            var view = new MainTuiView(
                backend?.LogScrollback ?? 3000, backend?.MaxDisplayedSuggestions ?? 10, backend?.DisplayIconBanner ?? true);
            backend?.SetView(view);

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
