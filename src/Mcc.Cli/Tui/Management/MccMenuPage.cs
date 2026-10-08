using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Management;

internal sealed record MccMenuActions(
    Action OpenCommands,
    Action OpenScripts,
    Action OpenRecipes,
    Action OpenEntities,
    Action OpenAdvancements,
    Action OpenChunks,
    Action OpenInventory,
    Action OpenMinimap,
    Action OpenPlayers,
    Action OpenScoreboard,
    Action OpenPlugins,
    Action OpenMarketplaces,
    Action OpenServers,
    Action ExitClient);

/// <summary>A responsive launcher for the rich management surfaces exposed by MCC.</summary>
internal sealed class MccMenuPage : ContentControl, IManagementWorkspacePage
{
    private readonly MccMenuActions _actions;
    private readonly List<Button> _buttons = [];
    private bool _compact;
    private Button? _firstButton;

    public MccMenuPage(MccMenuActions actions)
    {
        _actions = actions;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Top;
        Focusable = true;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        BuildLayout();
    }

    public string? Hints => Strings.MccMenuHints;

    public void SetCompact(bool compact)
    {
        if (_compact == compact)
            return;

        _compact = compact;
        BuildLayout();
        _firstButton?.Focus();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _firstButton?.Focus();
    }

    private void BuildLayout()
    {
        _firstButton = null;
        _buttons.Clear();
        Control learn = Group(
            Strings.MccMenuLearn,
            new MenuEntry(
                Strings.MccMenuCommands,
                Strings.MccMenuCommandsDescription,
                Strings.MccMenuCommandsHint,
                _actions.OpenCommands),
            new MenuEntry(
                Strings.MccMenuScripts,
                Strings.MccMenuScriptsDescription,
                Strings.MccMenuScriptsHint,
                _actions.OpenScripts));
        Control live = Group(
            Strings.MccMenuLive,
            new MenuEntry(
                Strings.MccMenuInventory,
                Strings.MccMenuInventoryDescription,
                Strings.MccMenuInventoryHint,
                _actions.OpenInventory),
            new MenuEntry(
                Strings.MccMenuRecipes,
                Strings.MccMenuRecipesDescription,
                Strings.MccMenuRecipesHint,
                _actions.OpenRecipes),
            new MenuEntry(
                Strings.MccMenuEntities,
                Strings.MccMenuEntitiesDescription,
                Strings.MccMenuEntitiesHint,
                _actions.OpenEntities),
            new MenuEntry(
                Strings.MccMenuAdvancements,
                Strings.MccMenuAdvancementsDescription,
                Strings.MccMenuAdvancementsHint,
                _actions.OpenAdvancements),
            new MenuEntry(
                Strings.MccMenuChunks,
                Strings.MccMenuChunksDescription,
                Strings.MccMenuChunksHint,
                _actions.OpenChunks),
            new MenuEntry(
                Strings.MccMenuMinimap,
                Strings.MccMenuMinimapDescription,
                Strings.MccMenuMinimapHint,
                _actions.OpenMinimap),
            new MenuEntry(
                Strings.MccMenuPlayers,
                Strings.MccMenuPlayersDescription,
                Strings.MccMenuPlayersHint,
                _actions.OpenPlayers),
            new MenuEntry(
                Strings.MccMenuScoreboard,
                Strings.MccMenuScoreboardDescription,
                Strings.MccMenuScoreboardHint,
                _actions.OpenScoreboard));
        Control extensions = Group(
            Strings.MccMenuExtend,
            new MenuEntry(
                Strings.MccMenuPlugins,
                Strings.MccMenuPluginsDescription,
                Strings.MccMenuPluginsHint,
                _actions.OpenPlugins),
            new MenuEntry(
                Strings.MccMenuMarketplaces,
                Strings.MccMenuMarketplacesDescription,
                Strings.MccMenuMarketplacesHint,
                _actions.OpenMarketplaces));
        Control client = Group(
            Strings.MccMenuClient,
            new MenuEntry(
                Strings.MccMenuServers,
                Strings.MccMenuServersDescription,
                Strings.MccMenuServersHint,
                _actions.OpenServers),
            new MenuEntry(
                Strings.MccMenuExit,
                Strings.MccMenuExitDescription,
                Strings.MccMenuExitHint,
                _actions.ExitClient,
                ManagementButtonKind.Danger));

        if (_compact)
        {
            var stack = new StackPanel { Spacing = 1 };
            stack.Children.Add(learn);
            stack.Children.Add(live);
            stack.Children.Add(extensions);
            stack.Children.Add(client);
            Content = stack;
            return;
        }

        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        var left = new StackPanel { Spacing = 1, Margin = new Thickness(0, 0, 1, 0) };
        left.Children.Add(learn);
        left.Children.Add(extensions);
        left.Children.Add(client);
        columns.Children.Add(left);
        Grid.SetColumn(live, 1);
        columns.Children.Add(live);
        Content = columns;
    }

    private Control Group(string heading, params MenuEntry[] entries)
    {
        var content = new StackPanel { Spacing = 1 };
        content.Children.Add(ManagementUi.Section(heading));
        foreach (MenuEntry entry in entries)
        {
            Button button = EntryButton(entry);
            _firstButton ??= button;
            _buttons.Add(button);
            content.Children.Add(button);
        }

        return ManagementUi.Card(content);
    }

    private static Button EntryButton(MenuEntry entry)
    {
        Button button = ManagementUi.Button(entry.Title, entry.Open, entry.Kind);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        button.Padding = new Thickness(1);

        var text = new StackPanel { Spacing = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        text.Children.Add(new TextBlock
        {
            Text = entry.Title,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
        });
        text.Children.Add(new TextBlock
        {
            Text = entry.Description,
            Foreground = ManagementUi.Soft,
            TextWrapping = TextWrapping.Wrap,
        });
        text.Children.Add(new TextBlock
        {
            Text = entry.Hint,
            Foreground = ManagementUi.Muted,
            TextWrapping = TextWrapping.Wrap,
        });
        button.Content = text;
        return button;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        int direction = e.Key switch
        {
            Key.Up or Key.Left => -1,
            Key.Down or Key.Right => 1,
            _ => 0,
        };
        if (direction == 0 || _buttons.Count == 0)
            return;

        int current = _buttons.FindIndex(static button => button.IsFocused);
        int next = current < 0
            ? 0
            : (current + direction + _buttons.Count) % _buttons.Count;
        _buttons[next].Focus();
        e.Handled = true;
    }

    private sealed record MenuEntry(
        string Title,
        string Description,
        string Hint,
        Action Open,
        ManagementButtonKind Kind = ManagementButtonKind.Secondary);
}
