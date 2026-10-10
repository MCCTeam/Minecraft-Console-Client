using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Overlays;
using Mcc.Cli.Tui.Presentation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Input;

/// <summary>A labeled text field in a local prompt dialog.</summary>
/// <param name="Key">The key the entered value is reported under.</param>
/// <param name="Label">The label shown beside the editor.</param>
/// <param name="Secret">When true the editor masks its content (passwords, pasted codes stay readable).</param>
/// <param name="Initial">The editor's starting text.</param>
internal sealed record TuiPromptField(string Key, string Label, bool Secret = false, string Initial = "");

/// <summary>The answered local prompt dialog: which button was pressed and every field's value.</summary>
/// <param name="ButtonIndex">The pressed button's index in the asked list.</param>
/// <param name="Values">Field values by <see cref="TuiPromptField.Key"/>.</param>
internal sealed record TuiPromptResult(int ButtonIndex, IReadOnlyDictionary<string, string> Values);

/// <summary>The visual emphasis of an action in a local prompt.</summary>
internal enum TuiPromptButtonKind
{
    Primary,
    Secondary,
    Quiet,
    Danger,
}

/// <summary>
/// A local (client-side) prompt dialog: title, body lines, labeled fields, buttons.
/// The overlay analogue of the console's print-a-question/read-a-line, for everything that must happen inside the TUI: first-run login, auth credentials, confirmations.
/// <para>
/// Unlike <see cref="DialogOverlay"/>, which renders a SERVER dialog through the game APIs, this one answers locally: pressing a button resolves the task with the button index and the field values, Escape (or cancellation) resolves null.
/// At least one button is required, since a dialog nobody can answer is a trap; Escape is always the way out.
/// </para>
/// </summary>
internal static class TuiPrompt
{
    // A fixed editor width keeps labels and fields aligned without letting one long value make a compact dialog consume the terminal.
    // TextBox scrolls its contents when the value is longer.
    private const double DefaultFieldWidth = 40;
    private static readonly IBrush Canvas = new SolidColorBrush(Color.FromRgb(15, 15, 25));
    private static readonly IBrush Raised = new SolidColorBrush(Color.FromRgb(31, 42, 48));
    private static readonly IBrush Cyan = new SolidColorBrush(Color.FromRgb(0, 229, 229));
    private static readonly IBrush CyanDark = new SolidColorBrush(Color.FromRgb(0, 139, 139));
    private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(119, 119, 119));
    private static readonly IBrush Soft = new SolidColorBrush(Color.FromRgb(170, 170, 170));
    private static readonly IBrush Red = new SolidColorBrush(Color.FromRgb(255, 85, 85));
    private static readonly IBrush RedPanel = new SolidColorBrush(Color.FromRgb(58, 25, 32));
    /// <summary>
    /// Shows the dialog and completes with the answer, or null when it was cancelled, the view is not up, or <paramref name="ct"/> fired.
    /// Must not be called before the backend view is ready.
    /// </summary>
    /// <param name="heroText">
    /// Optional hero line (a device code): shown letter-spaced, bold and yellow inside its own box between the body and the fields.
    /// Terminal cells cannot scale a font, so size comes from spacing and framing instead.
    /// </param>
    /// <param name="buttonKinds">
    /// Optional emphasis for each button.
    /// When omitted, the first action is primary and the rest are secondary.
    /// </param>
    /// <param name="stackedButtonCount">
    /// Number of leading buttons to render as a full-width vertical list before the remaining action row.
    /// </param>
    internal static Task<TuiPromptResult?> AskAsync(
        TuiBackend backend,
        string title,
        IReadOnlyList<string> body,
        IReadOnlyList<TuiPromptField> fields,
        IReadOnlyList<string> buttons,
        CancellationToken ct = default,
        string? heroText = null,
        IReadOnlyList<TuiPromptButtonKind>? buttonKinds = null,
        int stackedButtonCount = 0)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(buttons);

        if (buttons.Count == 0)
            throw new ArgumentException("A prompt dialog needs at least one button.", nameof(buttons));

        if (buttonKinds is not null && buttonKinds.Count != buttons.Count)
            throw new ArgumentException("Prompt button styles must match the button count.", nameof(buttonKinds));

        if (stackedButtonCount < 0 || stackedButtonCount > buttons.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stackedButtonCount), "The stacked button count must fit within the button list.");
        }

        if (ct.IsCancellationRequested)
            return Task.FromCanceled<TuiPromptResult?>(ct);

        var completion = new TaskCompletionSource<TuiPromptResult?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        backend.Post(() =>
        {
            MainTuiView? view = backend.View;
            if (view is null)
            {
                completion.TrySetResult(null);
                return;
            }

            var editors = new Dictionary<string, TextBox>(StringComparer.Ordinal);
            var stack = new StackPanel { Spacing = 1 };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Cyan,
                FontWeight = FontWeight.Bold,
            });

            foreach (string line in body)
            {
                // Same rule as the TUI log (TuiBackend.WriteLine): a line carrying legacy section codes renders them, anything else is plain text.
                // Lets callers highlight the one token that matters (a device code) the way the classic host does with SGR.
                stack.Children.Add(LegacyTextInlines.HasCodes(line)
                    ? LegacyTextInlines.Render(line)
                    : new TextBlock { Text = line, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });
            }

            if (heroText is { Length: > 0 })
            {
                stack.Children.Add(new Border
                {
                    BorderBrush = Brushes.Gold,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(2, 0),
                    Margin = new Thickness(0, 1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = string.Join(' ', heroText.ToCharArray()),
                        Foreground = Brushes.Yellow,
                        FontWeight = FontWeight.Bold,
                    },
                });
            }

            foreach (TuiPromptField field in fields)
            {
                var editor = new TextBox
                {
                    Text = field.Initial,
                    Width = DefaultFieldWidth,
                    MinWidth = 0,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                if (field.Secret)
                    editor.PasswordChar = '*';

                editors[field.Key] = editor;

                var fieldStack = new StackPanel
                {
                    Spacing = 0,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                fieldStack.Children.Add(new TextBlock { Text = field.Label, Foreground = Soft });
                fieldStack.Children.Add(editor);
                stack.Children.Add(fieldStack);
            }

            // Answering closes the dialog; cancelling (Escape) leaves no answer.
            // Either way the overlay comes down first so a fault completing the task can never strand it on screen.
            void Answer(int index)
            {
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, TextBox> entry in editors)
                    values[entry.Key] = entry.Value.Text ?? string.Empty;

                view.HideOverlay();
                completion.TrySetResult(new TuiPromptResult(index, values));
            }

            void Cancel()
            {
                view.HideOverlay();
                completion.TrySetResult(null);
            }

            Button? firstButton = null;
            var stackedActions = new StackPanel { Spacing = 0 };
            var actions = new WrapPanel { Margin = new Thickness(0, 1, 0, 0) };
            for (int i = 0; i < buttons.Count; i++)
            {
                int index = i;
                TuiPromptButtonKind kind = buttonKinds?[index]
                    ?? (index == 0 ? TuiPromptButtonKind.Primary : TuiPromptButtonKind.Secondary);
                Button button = CreateButton(buttons[index], kind, isDefault: index == 0);
                button.Click += (_, _) => Answer(index);
                button.Margin = new Thickness(0, 0, 1, 1);
                if (index < stackedButtonCount)
                {
                    button.HorizontalAlignment = HorizontalAlignment.Stretch;
                    button.HorizontalContentAlignment = HorizontalAlignment.Left;
                    stackedActions.Children.Add(button);
                }
                else
                    actions.Children.Add(button);
                firstButton ??= button;

                if (index == 0)
                {
                    // Enter anywhere (a field, the first button) presses the default button, so a pasted code plus Enter never needs the mouse.
                    foreach (TextBox editor in editors.Values)
                    {
                        editor.KeyDown += (_, e) =>
                        {
                            if (e.Key is Key.Enter)
                            {
                                Answer(0);
                                e.Handled = true;
                            }
                        };
                    }
                }
            }
            if (stackedActions.Children.Count > 0)
                stack.Children.Add(stackedActions);

            if (actions.Children.Count > 0)
                stack.Children.Add(actions);

            var overlay = new Border
            {
                BorderBrush = Brushes.DarkCyan,
                BorderThickness = new Thickness(1),
                Background = Canvas,
                Padding = new Thickness(2, 1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new ScrollViewer
                {
                    Content = stack,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                },
                Focusable = true,
            };
            overlay.KeyDown += (_, e) =>
            {
                if (e.Key is Key.Escape)
                {
                    Cancel();
                    e.Handled = true;
                }
            };

            view.ShowOverlay(overlay);

            // Focus where the fingers already are: the first field, else the default button.
            if (editors.Count > 0)
                editors[fields[0].Key].Focus();
            else if (firstButton is not null)
                firstButton.Focus();
        });

        if (ct.CanBeCanceled)
        {
            ct.Register(() =>
            {
                backend.Post(() => backend.View?.HideOverlay());
                completion.TrySetCanceled(ct);
            });
        }

        return completion.Task;
    }

    private static Button CreateButton(string label, TuiPromptButtonKind kind, bool isDefault)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(1, 0),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            IsDefault = isDefault,
        };

        switch (kind)
        {
            case TuiPromptButtonKind.Primary:
                button.Background = Cyan;
                button.Foreground = Brushes.Black;
                button.BorderBrush = Cyan;
                break;
            case TuiPromptButtonKind.Danger:
                button.Background = RedPanel;
                button.Foreground = Red;
                button.BorderBrush = Red;
                break;
            case TuiPromptButtonKind.Quiet:
                button.Background = Canvas;
                button.Foreground = Soft;
                button.BorderBrush = Muted;
                break;
            default:
                button.Background = Raised;
                button.Foreground = Cyan;
                button.BorderBrush = CyanDark;
                break;
        }

        IBrush restingForeground = button.Foreground;
        void UpdateForeground()
            => button.Foreground = !button.IsEnabled
                ? Muted
                : button.IsFocused ? Brushes.White : restingForeground;

        button.GotFocus += (_, _) => UpdateForeground();
        button.LostFocus += (_, _) => UpdateForeground();
        button.PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsEnabledProperty)
                UpdateForeground();
        };

        return button;
    }
}
