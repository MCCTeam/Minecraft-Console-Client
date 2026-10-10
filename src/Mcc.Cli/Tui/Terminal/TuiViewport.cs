using Avalonia.Controls;

namespace Mcc.Cli.Tui.Terminal;

/// <summary>Single owner for page viewport reparenting shared by the management and plugin shells.</summary>
internal static class TuiViewport
{
    internal static void SetPageScrollMode(
        ContentControl viewport,
        ScrollViewer scroll,
        ContentControl pageHost,
        bool useWorkspaceScroll)
    {
        if (useWorkspaceScroll)
        {
            if (ReferenceEquals(viewport.Content, scroll))
                return;

            viewport.Content = null;
            scroll.Content = pageHost;
            viewport.Content = scroll;
            return;
        }

        if (ReferenceEquals(viewport.Content, pageHost))
            return;

        scroll.Content = null;
        viewport.Content = null;
        viewport.Content = pageHost;
    }
}
