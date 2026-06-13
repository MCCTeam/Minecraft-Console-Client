using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MinecraftClient.Dialogs;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Dialogs;

public sealed class DialogNbtParser
{
    public DialogDefinition Parse(Dictionary<string, object> nbt)
    {
        var type = NormalizeType(GetString(nbt, "type") ?? "minecraft:notice");
        var common = ParseCommon(nbt, type);
        var actions = new List<DialogButton>();
        DialogActionDefinition? cancelAction = null;
        var columns = GetInt(nbt, "columns", 1);
        var buttonWidth = GetInt(nbt, "button_width", 150);

        switch (type)
        {
            case "minecraft:notice":
                var noticeAction = ParseButton(nbt, "action", 1);
                actions.Add(noticeAction ?? new DialogButton(1, Translations.dialog_action_ok, null));
                cancelAction = actions[0].Action;
                break;

            case "minecraft:confirmation":
                AddIfNotNull(actions, ParseButton(nbt, "yes", 1));
                AddIfNotNull(actions, ParseButton(nbt, "no", 2));
                cancelAction = actions.Count >= 2 ? actions[1].Action : null;
                break;

            case "minecraft:multi_action":
                actions.AddRange(ParseButtonList(GetValue(nbt, "actions")));
                cancelAction = ParseButton(nbt, "exit_action", 0)?.Action;
                break;

            case "minecraft:dialog_list":
                actions.AddRange(ParseDialogListActions(GetValue(nbt, "dialogs")));
                cancelAction = ParseButton(nbt, "exit_action", 0)?.Action;
                break;

            case "minecraft:server_links":
                cancelAction = ParseButton(nbt, "exit_action", 0)?.Action;
                break;
        }

        return new DialogDefinition(
            type,
            common.Title,
            common.ExternalTitle,
            common.CanCloseWithEscape,
            common.Pause,
            common.AfterAction,
            common.Body,
            common.Inputs,
            actions,
            cancelAction,
            columns,
            buttonWidth);
    }

    public DialogDefinition? TryParse(Dictionary<string, object>? nbt)
    {
        return nbt is null ? null : Parse(nbt);
    }

    private static DialogCommon ParseCommon(Dictionary<string, object> nbt, string type)
    {
        var title = ParseComponent(GetValue(nbt, "title"));
        var externalTitle = nbt.TryGetValue("external_title", out var externalTitleValue)
            ? ParseComponent(externalTitleValue)
            : null;
        var canCloseWithEscape = GetBool(nbt, "can_close_with_escape", true);
        var pause = GetBool(nbt, "pause", true);
        var afterAction = ParseAfterAction(GetString(nbt, "after_action") ?? "close");
        var body = ParseBody(GetValue(nbt, "body"));
        var inputs = ParseInputs(GetValue(nbt, "inputs"));

        if (string.IsNullOrWhiteSpace(title))
            title = type;

        return new DialogCommon(title, externalTitle, canCloseWithEscape, pause, afterAction, body, inputs);
    }

    private static IReadOnlyList<DialogBody> ParseBody(object? value)
    {
        if (value is null)
            return [];

        List<DialogBody> body = [];
        foreach (var item in Enumerate(value))
        {
            if (item is Dictionary<string, object> compound)
            {
                var type = NormalizeType(GetString(compound, "type") ?? "minecraft:plain_message");
                if (type == "minecraft:item")
                {
                    var description = compound.TryGetValue("description", out var desc)
                        ? ParsePlainMessage(desc)
                        : string.Empty;
                    body.Add(new DialogBody(DialogBodyKind.Item, string.IsNullOrWhiteSpace(description) ? Translations.dialog_item_body : description, type));
                    continue;
                }

                body.Add(new DialogBody(DialogBodyKind.PlainMessage, ParsePlainMessage(compound), type));
                continue;
            }

            body.Add(new DialogBody(DialogBodyKind.PlainMessage, ParseComponent(item), "minecraft:plain_message"));
        }

        return body;
    }

    private static string ParsePlainMessage(object? value)
    {
        if (value is Dictionary<string, object> compound && compound.TryGetValue("contents", out var contents))
            return ParseComponent(contents);

        return ParseComponent(value);
    }

    private static IReadOnlyList<DialogInput> ParseInputs(object? value)
    {
        if (value is null)
            return [];

        List<DialogInput> inputs = [];
        foreach (var item in Enumerate(value))
        {
            if (item is not Dictionary<string, object> inputData)
                continue;

            var key = GetString(inputData, "key");
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var control = inputData.TryGetValue("control", out var controlValue) && controlValue is Dictionary<string, object> controlData
                ? controlData
                : inputData;

            var type = NormalizeType(GetString(control, "type") ?? "minecraft:text");
            inputs.Add(type switch
            {
                "minecraft:boolean" => ParseBooleanInput(key, type, control),
                "minecraft:number_range" => ParseNumberInput(key, type, control),
                "minecraft:single_option" => ParseOptionInput(key, type, control),
                "minecraft:text" => ParseTextInput(key, type, control),
                _ => new DialogInput(key, DialogInputKind.Unknown, ParseComponent(GetValue(control, "label")), string.Empty, Type: type)
            });
        }

        return inputs;
    }

    private static DialogInput ParseTextInput(string key, string type, Dictionary<string, object> control)
    {
        return new DialogInput(
            key,
            DialogInputKind.Text,
            ParseComponent(GetValue(control, "label")),
            GetString(control, "initial") ?? string.Empty,
            MaxLength: GetInt(control, "max_length", 32),
            LabelVisible: GetBool(control, "label_visible", true),
            Multiline: control.ContainsKey("multiline"),
            Type: type);
    }

    private static DialogInput ParseBooleanInput(string key, string type, Dictionary<string, object> control)
    {
        var initial = GetBool(control, "initial", false);
        return new DialogInput(
            key,
            DialogInputKind.Boolean,
            ParseComponent(GetValue(control, "label")),
            initial ? "true" : "false",
            OnTrue: GetString(control, "on_true") ?? "true",
            OnFalse: GetString(control, "on_false") ?? "false",
            Type: type);
    }

    private static DialogInput ParseOptionInput(string key, string type, Dictionary<string, object> control)
    {
        var options = ParseOptions(GetValue(control, "options"));
        var initial = options.FirstOrDefault(static option => option.Initial)?.Id
            ?? options.FirstOrDefault()?.Id
            ?? string.Empty;
        return new DialogInput(
            key,
            DialogInputKind.SingleOption,
            ParseComponent(GetValue(control, "label")),
            initial,
            LabelVisible: GetBool(control, "label_visible", true),
            Options: options,
            Type: type);
    }

    private static DialogInput ParseNumberInput(string key, string type, Dictionary<string, object> control)
    {
        var range = control.TryGetValue("range_info", out var rangeValue) && rangeValue is Dictionary<string, object> rangeData
            ? rangeData
            : control;
        var start = GetFloat(range, "start", 0);
        var end = GetFloat(range, "end", 1);
        var initial = TryGetFloat(range, "initial") ?? ((start + end) / 2F);
        return new DialogInput(
            key,
            DialogInputKind.NumberRange,
            ParseComponent(GetValue(control, "label")),
            NumberToString(initial),
            Start: start,
            End: end,
            InitialNumber: initial,
            Step: TryGetFloat(range, "step"),
            Type: type);
    }

    private static IReadOnlyList<DialogOption> ParseOptions(object? value)
    {
        if (value is null)
            return [];

        List<DialogOption> options = [];
        foreach (var item in Enumerate(value))
        {
            if (item is string id)
            {
                options.Add(new DialogOption(id, id, false));
                continue;
            }

            if (item is Dictionary<string, object> option)
            {
                var optionId = GetString(option, "id");
                if (optionId is null)
                    continue;

                var display = option.TryGetValue("display", out var displayValue)
                    ? ParseComponent(displayValue)
                    : optionId;
                options.Add(new DialogOption(optionId, display, GetBool(option, "initial", false)));
            }
        }

        return options;
    }

    private static List<DialogButton> ParseButtonList(object? value)
    {
        List<DialogButton> buttons = [];
        var index = 1;
        foreach (var item in Enumerate(value))
        {
            if (item is Dictionary<string, object> buttonData)
                buttons.Add(ParseButton(buttonData, index++) ?? new DialogButton(index - 1, Translations.dialog_action_unnamed, null));
        }

        return buttons;
    }

    private static IEnumerable<DialogButton> ParseDialogListActions(object? value)
    {
        List<DialogButton> buttons = [];
        var index = 1;
        foreach (var item in Enumerate(value))
        {
            switch (item)
            {
                case string tag when tag.StartsWith('#'):
                    buttons.Add(new DialogButton(index++, tag, new DialogActionDefinition(DialogActionKind.Unknown, Type: "dialog_tag")));
                    break;
                case string resource:
                    buttons.Add(new DialogButton(index++, resource, new DialogActionDefinition(DialogActionKind.ShowDialog, Value: resource, Type: "dialog_reference_name")));
                    break;
                case Dictionary<string, object> dialog:
                    var nested = new DialogNbtParser().Parse(dialog);
                    buttons.Add(new DialogButton(index++, nested.DisplayTitle(), new DialogActionDefinition(DialogActionKind.ShowDialog, NestedDialog: nested)));
                    break;
            }
        }

        return buttons;
    }

    private static DialogButton? ParseButton(Dictionary<string, object> owner, string key, int index)
    {
        return owner.TryGetValue(key, out var value) && value is Dictionary<string, object> data
            ? ParseButton(data, index)
            : null;
    }

    private static DialogButton? ParseButton(Dictionary<string, object> data, int index)
    {
        var label = data.TryGetValue("label", out var labelValue)
            ? ParseComponent(labelValue)
            : Translations.dialog_action_unnamed;
        var action = data.TryGetValue("action", out var actionValue) && actionValue is Dictionary<string, object> actionData
            ? ParseAction(actionData)
            : null;
        return new DialogButton(index, label, action);
    }

    private static DialogActionDefinition ParseAction(Dictionary<string, object> action)
    {
        var type = NormalizeType(GetString(action, "type") ?? GetString(action, "action") ?? "minecraft:none");
        return type switch
        {
            "minecraft:run_command" => new DialogActionDefinition(DialogActionKind.RunCommand, Value: GetString(action, "command"), Type: type),
            "minecraft:dynamic/run_command" => new DialogActionDefinition(DialogActionKind.RunCommand, Value: GetString(action, "template"), Type: type),
            "minecraft:custom" => new DialogActionDefinition(DialogActionKind.CustomClick, Id: GetString(action, "id"), Payload: GetCompound(action, "payload"), Type: type),
            "minecraft:dynamic/custom" => new DialogActionDefinition(DialogActionKind.CustomClick, Id: GetString(action, "id"), Payload: GetCompound(action, "additions"), Type: type),
            "minecraft:open_url" => new DialogActionDefinition(DialogActionKind.OpenUrl, Value: GetString(action, "url"), Type: type),
            "minecraft:suggest_command" => new DialogActionDefinition(DialogActionKind.SuggestCommand, Value: GetString(action, "command"), Type: type),
            "minecraft:copy_to_clipboard" => new DialogActionDefinition(DialogActionKind.CopyToClipboard, Value: GetString(action, "value"), Type: type),
            "minecraft:show_dialog" => ParseShowDialogAction(action, type),
            _ => new DialogActionDefinition(DialogActionKind.Unknown, Type: type)
        };
    }

    private static DialogActionDefinition ParseShowDialogAction(Dictionary<string, object> action, string type)
    {
        if (!action.TryGetValue("dialog", out var value))
            return new DialogActionDefinition(DialogActionKind.ShowDialog, Type: type);

        if (value is Dictionary<string, object> dialogData)
            return new DialogActionDefinition(DialogActionKind.ShowDialog, NestedDialog: new DialogNbtParser().Parse(dialogData), Type: type);

        if (value is int protocolId)
            return new DialogActionDefinition(DialogActionKind.ShowDialog, DialogReferenceId: protocolId, Type: type);

        return new DialogActionDefinition(DialogActionKind.ShowDialog, Value: value.ToString(), Type: type);
    }

    private static DialogAfterAction ParseAfterAction(string value)
    {
        return value switch
        {
            "none" => DialogAfterAction.None,
            "wait_for_response" => DialogAfterAction.WaitForResponse,
            _ => DialogAfterAction.Close
        };
    }

    private static string ParseComponent(object? value)
    {
        if (value is null)
            return string.Empty;

        try
        {
            return value switch
            {
                Dictionary<string, object> compound => ChatParser.ParseText(compound),
                string text when text.StartsWith('{') || text.StartsWith('[') => ChatParser.ParseText(text),
                string text => text,
                _ => value.ToString() ?? string.Empty
            };
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    private static IEnumerable<object> Enumerate(object? value)
    {
        if (value is null)
            yield break;

        if (value is object[] array)
        {
            foreach (var item in array)
                yield return item;
            yield break;
        }

        yield return value;
    }

    private static object? GetValue(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value : null;
    }

    private static string? GetString(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value as string ?? value.ToString() : null;
    }

    private static Dictionary<string, object>? GetCompound(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var value) && value is Dictionary<string, object> compound ? compound : null;
    }

    private static bool GetBool(Dictionary<string, object> data, string key, bool fallback)
    {
        if (!data.TryGetValue(key, out var value))
            return fallback;

        return value switch
        {
            bool boolean => boolean,
            byte number => number != 0,
            sbyte number => number != 0,
            int number => number != 0,
            string text when bool.TryParse(text, out var parsed) => parsed,
            _ => fallback
        };
    }

    private static int GetInt(Dictionary<string, object> data, string key, int fallback)
    {
        if (!data.TryGetValue(key, out var value))
            return fallback;

        return value switch
        {
            byte number => number,
            short number => number,
            int number => number,
            long number => (int)number,
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => fallback
        };
    }

    private static float GetFloat(Dictionary<string, object> data, string key, float fallback)
    {
        return TryGetFloat(data, key) ?? fallback;
    }

    private static float? TryGetFloat(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            float number => number,
            double number => (float)number,
            int number => number,
            long number => number,
            string text when float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static string NormalizeType(string type)
    {
        return type.Contains(':', StringComparison.Ordinal) ? type : "minecraft:" + type;
    }

    private static void AddIfNotNull(List<DialogButton> buttons, DialogButton? button)
    {
        if (button is not null)
            buttons.Add(button);
    }

    private static string NumberToString(float value)
    {
        var integer = (int)value;
        return integer == value
            ? integer.ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record DialogCommon(
        string Title,
        string? ExternalTitle,
        bool CanCloseWithEscape,
        bool Pause,
        DialogAfterAction AfterAction,
        IReadOnlyList<DialogBody> Body,
        IReadOnlyList<DialogInput> Inputs);
}
