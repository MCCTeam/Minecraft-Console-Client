using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Consolonia.Themes;
using MinecraftClient.Inventory;

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
                var handler = InventoryTuiHost.ActiveHandler!;
                var windowId = InventoryTuiHost.ActiveWindowId;
                var container = handler.GetInventory(windowId);
                var containerType = container?.Type ?? ContainerType.PlayerInventory;
                var view = ContainerViewBase.CreateView(containerType, handler, windowId);

                desktop.MainWindow = new Window
                {
                    Content = view,
                    Title = "MCC Inventory"
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
