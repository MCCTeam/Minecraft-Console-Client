using Mcc.Cli.Localization;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using DMCBK.Core;
using DMCBK.Core.Beacon;

namespace Mcc.Cli.Tui.Management;

internal sealed class ScriptSettingsPage : StackPanel, IManagementWorkspacePage
{
    private readonly ManagementWorkspace _workspace;
    private readonly Client _client;
    private readonly string _id;
    private readonly StackPanel _fields;
    private readonly List<SettingEditor> _editors = [];

    public ScriptSettingsPage(ManagementWorkspace workspace, Client client, string id)
    {
        _workspace = workspace;
        _client = client;
        _id = id;
        Spacing = 1;
        Children.Add(ManagementUi.Section(Strings.ScriptsUiSettingsTitle + " · " + id));
        _fields = new StackPanel { Spacing = 1 };
        Children.Add(_fields);
        Children.Add(ManagementUi.Actions(ManagementUi.Button(
            Strings.ScriptsUiSave, () => _ = SaveAsync(), ManagementButtonKind.Primary)));
        _ = LoadAsync();
    }

    public void Refresh() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            ScriptSettingsSnapshot snapshot = await _client.Scripts.GetSettingsAsync(_id, _workspace.Closed);
            _fields.Children.Clear();
            _editors.Clear();
            if (snapshot.Declarations.Count == 0)
            {
                _fields.Children.Add(ManagementUi.Body(Strings.ScriptsUiNoSettings, ManagementUi.Muted));
                return;
            }

            foreach (BeaconSettingDecl declaration in snapshot.Declarations)
            {
                snapshot.Values.TryGetValue(declaration.Name, out BeaconValue? current);
                current ??= declaration.Default;
                Control editor;
                if (current is BeaconYesNoValue yesNo)
                {
                    editor = new CheckBox
                    {
                        IsChecked = yesNo.Value,
                        HorizontalAlignment = HorizontalAlignment.Left,
                    };
                }
                else
                {
                    editor = new TextBox
                    {
                        Text = Display(current),
                        Width = 62,
                        HorizontalAlignment = HorizontalAlignment.Left,
                    };
                }

                var field = new StackPanel { Spacing = 0, HorizontalAlignment = HorizontalAlignment.Left };
                field.Children.Add(ManagementUi.Body(declaration.Name, ManagementUi.Soft));
                field.Children.Add(editor);
                if (!string.IsNullOrWhiteSpace(declaration.Comment))
                    field.Children.Add(ManagementUi.Body(declaration.Comment, ManagementUi.Muted));
                _fields.Children.Add(field);
                _editors.Add(new SettingEditor(declaration, editor));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
    }

    private async Task SaveAsync()
    {
        foreach (SettingEditor editor in _editors)
        {
            BeaconValue value = Read(editor);
            ScriptSettingSaveResult result = await _client.Scripts.SaveSettingAsync(
                _id, editor.Declaration.Name, value, _workspace.Closed);
            if (!result.Saved)
            {
                _workspace.SetStatus(Strings.ScriptsUiFailed(result.Error));
                return;
            }
        }

        _workspace.SetStatus(Strings.ScriptsUiSettingsSaved);
    }

    private static BeaconValue Read(SettingEditor editor)
    {
        if (editor.Control is CheckBox check)
            return BeaconValue.YesNo(check.IsChecked == true);

        string text = ((TextBox)editor.Control).Text ?? string.Empty;
        if (editor.Declaration.Default is BeaconNumberValue
            && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            return BeaconValue.Number(number);

        return BeaconValue.Text(text);
    }

    private static string Display(BeaconValue value) => value switch
    {
        BeaconTextValue text => text.Value,
        BeaconNumberValue number => number.Value.ToString("G", CultureInfo.InvariantCulture),
        BeaconYesNoValue yesNo => yesNo.Value.ToString(),
        _ => string.Empty,
    };

    private sealed record SettingEditor(BeaconSettingDecl Declaration, Control Control);
}
