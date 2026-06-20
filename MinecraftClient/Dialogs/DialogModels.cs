using System;
using System.Collections.Generic;

namespace MinecraftClient.Dialogs;

public enum DialogPhase
{
    Configuration,
    Play
}

public enum DialogAfterAction
{
    Close,
    None,
    WaitForResponse
}

public enum DialogBodyKind
{
    PlainMessage,
    Item,
    Unknown
}

public enum DialogInputKind
{
    Text,
    Boolean,
    SingleOption,
    NumberRange,
    Unknown
}

public enum DialogActionKind
{
    None,
    RunCommand,
    CustomClick,
    ShowDialog,
    OpenUrl,
    SuggestCommand,
    CopyToClipboard,
    Unknown
}

public sealed record DialogBody(DialogBodyKind Kind, string Text, string? Type = null);

public sealed record DialogOption(string Id, string Display, bool Initial)
{
    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Display) ? Id : Display;
    }
}

public sealed record DialogInput(
    string Key,
    DialogInputKind Kind,
    string Label,
    string InitialValue,
    int MaxLength = 32,
    bool LabelVisible = true,
    bool Multiline = false,
    IReadOnlyList<DialogOption>? Options = null,
    string OnTrue = "true",
    string OnFalse = "false",
    float Start = 0,
    float End = 1,
    float? InitialNumber = null,
    float? Step = null,
    string? Type = null);

public sealed record DialogActionDefinition(
    DialogActionKind Kind,
    string? Value = null,
    string? Id = null,
    Dictionary<string, object>? Payload = null,
    DialogDefinition? NestedDialog = null,
    int? DialogReferenceId = null,
    string? Type = null);

public sealed record DialogButton(int Index, string Label, DialogActionDefinition? Action, bool IsCancel = false);

public sealed record DialogServerLink(string Label, string Url);

public sealed record DialogDefinition(
    string Type,
    string Title,
    string? ExternalTitle,
    bool CanCloseWithEscape,
    bool Pause,
    DialogAfterAction AfterAction,
    IReadOnlyList<DialogBody> Body,
    IReadOnlyList<DialogInput> Inputs,
    IReadOnlyList<DialogButton> Actions,
    DialogActionDefinition? CancelAction,
    int Columns = 1,
    int ButtonWidth = 150,
    bool IsResolved = true,
    string? UnresolvedReference = null);

public sealed record DialogInstance(
    int Revision,
    DialogPhase Phase,
    DialogDefinition Definition,
    IReadOnlyDictionary<string, string> Values,
    DateTimeOffset ReceivedAt);

public sealed record DialogActionResult(bool Success, string Message);
