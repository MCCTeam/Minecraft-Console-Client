using System;
using System.Linq;
using System.Text;

namespace MinecraftClient.Dialogs;

public static class DialogFormatter
{
    public static string DisplayTitle(this DialogDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.ExternalTitle))
            return definition.ExternalTitle!;

        return string.IsNullOrWhiteSpace(definition.Title) ? definition.Type : definition.Title;
    }

    public static string Render(DialogInstance instance)
    {
        StringBuilder builder = new();
        builder.AppendLine(string.Format(Translations.dialog_render_header, instance.Revision, instance.Phase, instance.Definition.DisplayTitle()));
        builder.AppendLine(string.Format(Translations.dialog_render_type, DisplayType(instance.Definition.Type)));

        foreach (var body in instance.Definition.Body.Where(static body => !string.IsNullOrWhiteSpace(body.Text)))
            builder.AppendLine(string.Format(Translations.dialog_render_body, body.Text));

        if (instance.Definition.Inputs.Count > 0)
        {
            builder.AppendLine(Translations.dialog_render_inputs);
            foreach (var input in instance.Definition.Inputs)
            {
                instance.Values.TryGetValue(input.Key, out var value);
                value ??= input.InitialValue;
                builder.AppendLine(string.Format(Translations.dialog_render_input, input.Key, input.Kind, input.Label, value, DescribeInput(input)));
            }
        }

        if (instance.Definition.Actions.Count > 0)
        {
            builder.AppendLine(Translations.dialog_render_actions);
            foreach (var action in instance.Definition.Actions)
                builder.AppendLine(string.Format(Translations.dialog_render_action, action.Index, action.Label, DescribeAction(action.Action)));
        }

        if (instance.Definition.CancelAction is not null || instance.Definition.CanCloseWithEscape)
            builder.AppendLine(Translations.dialog_render_cancel_hint);

        return builder.ToString();
    }

    public static string DisplayType(string rawType)
    {
        return rawType switch
        {
            "minecraft:notice" => Translations.dialog_type_notice,
            "minecraft:confirmation" => Translations.dialog_type_confirmation,
            "minecraft:multi_action" => Translations.dialog_type_multi_action,
            "minecraft:dialog_list" => Translations.dialog_type_dialog_list,
            "minecraft:server_links" => Translations.dialog_type_server_links,
            _ => string.IsNullOrEmpty(rawType) ? Translations.dialog_type_unknown : rawType
        };
    }

    private static string DescribeInput(DialogInput input)
    {
        return input.Kind switch
        {
            DialogInputKind.Text => string.Format(Translations.dialog_input_desc_text, input.MaxLength),
            DialogInputKind.Boolean => string.Format(Translations.dialog_input_desc_boolean, input.OnTrue, input.OnFalse),
            DialogInputKind.SingleOption => string.Format(Translations.dialog_input_desc_options,
                string.Join(", ", input.Options?.Select(static option => option.Id) ?? [])),
            DialogInputKind.NumberRange => string.Format(Translations.dialog_input_desc_number, input.Start, input.End),
            _ => input.Type ?? Translations.dialog_input_desc_unknown
        };
    }

    private static string DescribeAction(DialogActionDefinition? action)
    {
        if (action is null)
            return Translations.dialog_action_desc_close;

        return action.Kind switch
        {
            DialogActionKind.RunCommand => Translations.dialog_action_desc_command,
            DialogActionKind.CustomClick => Translations.dialog_action_desc_custom,
            DialogActionKind.ShowDialog => Translations.dialog_action_desc_show_dialog,
            DialogActionKind.OpenUrl => Translations.dialog_action_desc_open_url,
            DialogActionKind.SuggestCommand => Translations.dialog_action_desc_suggest,
            DialogActionKind.CopyToClipboard => Translations.dialog_action_desc_copy,
            _ => action.Type ?? Translations.dialog_action_desc_unknown
        };
    }
}
