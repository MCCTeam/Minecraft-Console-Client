using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace MinecraftClient.Dialogs;

public sealed class DialogManager
{
    private readonly McClient _client;
    private readonly Lock _lock = new();
    private readonly Dictionary<int, DialogDefinition> _registryById = new();
    private readonly Dictionary<string, DialogDefinition> _registryByName = new(StringComparer.Ordinal);
    private readonly List<DialogServerLink> _serverLinks = [];
    private DialogInstance? _current;
    private int _revision;

    public DialogManager(McClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    public event Action<DialogInstance>? DialogShown;
    public event Action<int>? DialogCleared;

    public DialogInstance? Current
    {
        get
        {
            lock (_lock)
                return _current;
        }
    }

    public void StoreRegistryDialog(int protocolId, string resourceId, DialogDefinition definition)
    {
        lock (_lock)
        {
            _registryById[protocolId] = definition;
            _registryByName[resourceId] = definition;
        }
    }

    public void ClearRegistry()
    {
        lock (_lock)
        {
            _registryById.Clear();
            _registryByName.Clear();
        }
    }

    public void SetServerLinks(IEnumerable<DialogServerLink> links)
    {
        lock (_lock)
        {
            _serverLinks.Clear();
            _serverLinks.AddRange(links);
        }
    }

    public DialogInstance Show(DialogDefinition definition, DialogPhase phase)
    {
        DialogInstance instance;
        lock (_lock)
        {
            var expanded = ExpandServerLinks(definition);
            var values = expanded.Inputs.ToDictionary(static input => input.Key, static input => input.InitialValue, StringComparer.Ordinal);
            instance = new DialogInstance(++_revision, phase, expanded, values, DateTimeOffset.UtcNow);
            _current = instance;
        }

        _client.Log.Info(string.Format(Translations.dialog_received, instance.Definition.DisplayTitle()));
        DialogShown?.Invoke(instance);
        return instance;
    }

    public DialogInstance ShowRegistryReference(int protocolId, DialogPhase phase)
    {
        DialogDefinition? definition;
        lock (_lock)
            _registryById.TryGetValue(protocolId, out definition);

        if (definition is not null)
            return Show(definition, phase);

        var unresolved = new DialogDefinition(
            "minecraft:unresolved",
            string.Format(CultureInfo.InvariantCulture, Translations.dialog_unresolved_title, protocolId),
            null,
            CanCloseWithEscape: true,
            Pause: false,
            DialogAfterAction.Close,
            [new DialogBody(DialogBodyKind.Unknown, string.Format(CultureInfo.InvariantCulture, Translations.dialog_unresolved_body, protocolId))],
            [],
            [],
            null,
            IsResolved: false,
            UnresolvedReference: protocolId.ToString(CultureInfo.InvariantCulture));
        return Show(unresolved, phase);
    }

    public void Clear()
    {
        int revision;
        lock (_lock)
        {
            revision = _current?.Revision ?? _revision;
            _current = null;
        }

        _client.Log.Info(Translations.dialog_cleared);
        DialogCleared?.Invoke(revision);
    }

    public DialogActionResult Dismiss()
    {
        int revision;
        lock (_lock)
        {
            if (_current is null)
                return new DialogActionResult(false, Translations.dialog_none);

            revision = _current.Revision;
            _current = null;
        }

        DialogCleared?.Invoke(revision);
        return new DialogActionResult(true, Translations.dialog_dismissed);
    }

    public DialogActionResult SetInput(string key, string value)
    {
        lock (_lock)
        {
            if (_current is null)
                return new DialogActionResult(false, Translations.dialog_none);

            var input = _current.Definition.Inputs.FirstOrDefault(input => input.Key.Equals(key, StringComparison.Ordinal));
            if (input is null)
                return new DialogActionResult(false, string.Format(Translations.dialog_input_unknown, key));

            var normalized = NormalizeInputValue(input, value, out var error);
            if (error is not null)
                return new DialogActionResult(false, error);

            var values = _current.Values.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
            values[key] = normalized;
            _current = _current with { Values = values };
            return new DialogActionResult(true, string.Format(Translations.dialog_input_set, key, normalized));
        }
    }

    public DialogActionResult Click(int index)
    {
        DialogButton? button;
        DialogInstance? instance;
        lock (_lock)
        {
            instance = _current;
            button = instance?.Definition.Actions.FirstOrDefault(action => action.Index == index);
        }

        if (instance is null)
            return new DialogActionResult(false, Translations.dialog_none);

        if (button is null)
            return new DialogActionResult(false, string.Format(Translations.dialog_action_unknown, index));

        return Execute(instance, button.Action, ShouldCloseAfterAction(instance.Definition.AfterAction));
    }

    public DialogActionResult ClickLabel(string label)
    {
        DialogButton[] matches;
        DialogInstance? instance;
        lock (_lock)
        {
            instance = _current;
            matches = instance?.Definition.Actions
                .Where(action => action.Label.Equals(label, StringComparison.OrdinalIgnoreCase))
                .ToArray() ?? [];
        }

        if (instance is null)
            return new DialogActionResult(false, Translations.dialog_none);

        return matches.Length switch
        {
            0 => new DialogActionResult(false, string.Format(Translations.dialog_action_label_unknown, label)),
            > 1 => new DialogActionResult(false, string.Format(Translations.dialog_action_label_ambiguous, label)),
            _ => Execute(instance, matches[0].Action, ShouldCloseAfterAction(instance.Definition.AfterAction))
        };
    }

    public DialogActionResult Cancel()
    {
        DialogInstance? instance;
        lock (_lock)
            instance = _current;

        if (instance is null)
            return new DialogActionResult(false, Translations.dialog_none);

        if (!instance.Definition.CanCloseWithEscape && instance.Definition.CancelAction is null)
            return new DialogActionResult(false, Translations.dialog_cannot_cancel);

        return Execute(instance, instance.Definition.CancelAction, closeWhenDone: true);
    }

    private DialogActionResult Execute(DialogInstance instance, DialogActionDefinition? action, bool closeWhenDone)
    {
        if (!instance.Definition.IsResolved)
            return new DialogActionResult(false, Translations.dialog_unresolved_action_disabled);

        if (action is null || action.Kind == DialogActionKind.None)
        {
            if (closeWhenDone)
                _ = Dismiss();
            return new DialogActionResult(true, Translations.dialog_action_closed);
        }

        var values = BuildActionValues(instance);
        switch (action.Kind)
        {
            case DialogActionKind.RunCommand:
                if (instance.Phase != DialogPhase.Play)
                    return new DialogActionResult(false, Translations.dialog_action_command_not_in_play);

                var command = ApplyTemplate(action.Value ?? string.Empty, values.TemplateValues);
                _client.SendText(command);
                if (closeWhenDone)
                    _ = Dismiss();
                return new DialogActionResult(true, string.Format(Translations.dialog_action_command_sent, command));

            case DialogActionKind.CustomClick:
                if (action.Id is null)
                    return new DialogActionResult(false, Translations.dialog_action_invalid);

                var payload = action.Type == "minecraft:custom" && action.Payload is null && values.TagValues.Count == 0
                    ? null
                    : MergePayload(action.Payload, values.TagValues);
                if (!_client.SendCustomClickAction(action.Id, payload))
                    return new DialogActionResult(false, Translations.dialog_action_custom_failed);

                if (closeWhenDone)
                    _ = Dismiss();
                return new DialogActionResult(true, string.Format(Translations.dialog_action_custom_sent, action.Id));

            case DialogActionKind.ShowDialog:
                if (action.NestedDialog is not null)
                {
                    Show(action.NestedDialog, instance.Phase);
                    return new DialogActionResult(true, Translations.dialog_action_nested_opened);
                }

                if (action.DialogReferenceId is int referenceId)
                {
                    ShowRegistryReference(referenceId, instance.Phase);
                    return new DialogActionResult(true, Translations.dialog_action_nested_opened);
                }

                if (action.Value is not null)
                {
                    DialogDefinition? referencedDialog;
                    lock (_lock)
                        _registryByName.TryGetValue(action.Value, out referencedDialog);

                    if (referencedDialog is not null)
                    {
                        Show(referencedDialog, instance.Phase);
                        return new DialogActionResult(true, Translations.dialog_action_nested_opened);
                    }
                }

                return new DialogActionResult(false, Translations.dialog_action_invalid);

            case DialogActionKind.OpenUrl:
                return new DialogActionResult(true, string.Format(Translations.dialog_action_open_url, action.Value ?? string.Empty));

            case DialogActionKind.SuggestCommand:
                return new DialogActionResult(true, string.Format(Translations.dialog_action_suggest_command, action.Value ?? string.Empty));

            case DialogActionKind.CopyToClipboard:
                return new DialogActionResult(true, string.Format(Translations.dialog_action_copy, action.Value ?? string.Empty));

            default:
                return new DialogActionResult(false, string.Format(Translations.dialog_action_unsupported, action.Type ?? action.Kind.ToString()));
        }
    }

    private static bool ShouldCloseAfterAction(DialogAfterAction afterAction)
    {
        return afterAction == DialogAfterAction.Close;
    }

    private DialogDefinition ExpandServerLinks(DialogDefinition definition)
    {
        if (!definition.Type.Equals("minecraft:server_links", StringComparison.Ordinal))
            return definition;

        var linkActions = _serverLinks
            .Select((link, index) => new DialogButton(
                index + 1,
                link.Label,
                new DialogActionDefinition(DialogActionKind.OpenUrl, Value: link.Url)))
            .ToList();

        if (definition.Actions.Count > 0)
            linkActions.AddRange(definition.Actions.Select((button, i) => button with { Index = linkActions.Count + i + 1 }));

        return definition with { Actions = linkActions };
    }

    private static DialogActionValues BuildActionValues(DialogInstance instance)
    {
        Dictionary<string, string> templateValues = new(StringComparer.Ordinal);
        Dictionary<string, object> tagValues = new(StringComparer.Ordinal);

        foreach (var input in instance.Definition.Inputs)
        {
            instance.Values.TryGetValue(input.Key, out var value);
            value ??= input.InitialValue;
            templateValues[input.Key] = ToTemplateValue(input, value);
            tagValues[input.Key] = ToNbtValue(input, value);
        }

        return new DialogActionValues(templateValues, tagValues);
    }

    private static Dictionary<string, object> MergePayload(Dictionary<string, object>? basePayload, Dictionary<string, object> inputTags)
    {
        Dictionary<string, object> payload = basePayload is null
            ? new(StringComparer.Ordinal)
            : new(basePayload, StringComparer.Ordinal);

        foreach (var (key, value) in inputTags)
            payload[key] = value;

        return payload;
    }

    private static string NormalizeInputValue(DialogInput input, string value, out string? error)
    {
        error = null;
        switch (input.Kind)
        {
            case DialogInputKind.Text:
                if (value.Length > input.MaxLength)
                {
                    error = string.Format(Translations.dialog_input_too_long, input.Key, input.MaxLength);
                    return input.InitialValue;
                }
                return value;

            case DialogInputKind.Boolean:
                if (bool.TryParse(value, out var boolValue))
                    return boolValue ? "true" : "false";

                if (value.Equals(input.OnTrue, StringComparison.OrdinalIgnoreCase))
                    return "true";

                if (value.Equals(input.OnFalse, StringComparison.OrdinalIgnoreCase))
                    return "false";

                error = string.Format(Translations.dialog_input_boolean_invalid, input.Key);
                return input.InitialValue;

            case DialogInputKind.SingleOption:
                if (input.Options?.Any(option => option.Id.Equals(value, StringComparison.Ordinal)) == true)
                    return value;

                error = string.Format(Translations.dialog_input_option_invalid, input.Key);
                return input.InitialValue;

            case DialogInputKind.NumberRange:
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    error = string.Format(Translations.dialog_input_number_invalid, input.Key);
                    return input.InitialValue;
                }

                var min = Math.Min(input.Start, input.End);
                var max = Math.Max(input.Start, input.End);
                if (number < min || number > max)
                {
                    error = string.Format(CultureInfo.InvariantCulture, Translations.dialog_input_number_range_invalid, input.Key, min, max);
                    return input.InitialValue;
                }

                return NumberToString(number);

            default:
                return value;
        }
    }

    private static string ToTemplateValue(DialogInput input, string value)
    {
        return input.Kind switch
        {
            DialogInputKind.Boolean => value.Equals("true", StringComparison.OrdinalIgnoreCase) ? input.OnTrue : input.OnFalse,
            DialogInputKind.Text => EscapeStringTagWithoutQuotes(value),
            _ => value
        };
    }

    private static object ToNbtValue(DialogInput input, string value)
    {
        return input.Kind switch
        {
            DialogInputKind.Boolean => (byte)(value.Equals("true", StringComparison.OrdinalIgnoreCase) ? 1 : 0),
            DialogInputKind.NumberRange when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
            _ => value
        };
    }

    private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace("$(" + key + ")", value, StringComparison.Ordinal);

        return result;
    }

    private static string EscapeStringTagWithoutQuotes(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string NumberToString(float value)
    {
        var integer = (int)value;
        return integer == value
            ? integer.ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record DialogActionValues(
        IReadOnlyDictionary<string, string> TemplateValues,
        Dictionary<string, object> TagValues);
}
