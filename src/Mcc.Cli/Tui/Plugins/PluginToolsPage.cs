using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DMCBK.Core.Plugins;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// The small tools: <c>doctor</c> and <c>validate</c> render their reports as read-only pages, while <c>new</c> and <c>load</c> are short forms (an id, a path) over the host calls.
/// Read-only output mirrors the text verbs; the forms mirror the install form.
/// </summary>
internal sealed class PluginToolsPage : StackPanel
{
    internal enum Tool
    {
        Doctor,
        Validate,
    }

    private readonly PluginManagerContext _ctx;
    private readonly Tool _tool;
    private readonly StackPanel _body = new();

    public PluginToolsPage(PluginManagerContext ctx, Tool tool)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        _ctx = ctx;
        _tool = tool;
        Spacing = 1;
        Focusable = true;

        Children.Add(PluginUi.PageHeader(Title(), Strings.PmReportsSubtitle));
        Children.Add(PluginUi.Card(_body));

        switch (tool)
        {
            case Tool.Doctor:
                RunDoctor();
                break;
            default:
                RunValidate();
                break;
        }
    }

    private string Title() => _tool switch
    {
        Tool.Doctor => Strings.PmDoctorTitle,
        _ => Strings.PmValidateTitle,
    };

    private void RunDoctor()
    {
        if (_ctx.Host is null)
        {
            Line(Strings.PmNoHost, PluginUi.Red);
            return;
        }

        PluginDoctorReport report = _ctx.Host.Doctor();
        Line(Strings.PmDoctorHeader(report.Discovered, report.Loaded), PluginUi.Gold);
        if (report.Notes.Count == 0)
        {
            Line(Strings.PmDoctorHealthy, PluginUi.Green);
            return;
        }

        foreach (PluginDoctorNote note in report.Notes)
        {
            Line($"{(note.Error ? Strings.PmSeverityError : Strings.PmSeverityNote)} {note.Id}: {note.Detail}",
                note.Error ? PluginUi.Red : PluginUi.Soft);
        }
    }

    private void RunValidate()
    {
        if (_ctx.Host is null)
        {
            Line(Strings.PmNoHost, PluginUi.Red);
            return;
        }

        IReadOnlyList<PluginCheckResult> results = _ctx.Host.Validate();
        int bad = results.Count(static r => !r.IsValid);
        Line(Strings.PmValidateHeader(results.Count, bad), PluginUi.Gold);

        foreach (PluginCheckResult result in results)
        {
            if (result.Problems.Count == 0)
            {
                Line(Strings.PmValidateSubjectOk(result.Id), PluginUi.Green);
                continue;
            }

            Line($"  {result.Id}:", PluginUi.White);
            foreach (PluginCheckProblem problem in result.Problems)
            {
                Line($"    {(problem.Error ? Strings.PmSeverityError : Strings.PmSeverityWarning)} {problem.File} {problem.Message}",
                    problem.Error ? PluginUi.Red : PluginUi.Soft);
            }
        }
    }

    private void Line(string text, Avalonia.Media.IBrush brush)
        => _body.Children.Add(new TextBlock { Text = text, Foreground = brush, TextWrapping = TextWrapping.Wrap });

    /// <summary>Takes focus once shown (reports have no controls of their own) so Escape works.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
    }
}

/// <summary>
/// The New and Load forms as centered modals, opened from the list footer.
/// Modals keep the forms narrow and side-step the fullscreen-page layout entirely.
/// </summary>
internal static class PluginToolsForms
{
    internal static void OpenNew(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var id = new TextBox();

        Border frame = PluginModal.Frame(ctx, Strings.PmNewTitle, out StackPanel content);
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldId, id));
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmNewHint,
            Foreground = PluginUi.Muted,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(PluginModal.ButtonRow(
            PluginModal.ActionButton(
                ctx,
                Strings.PmScaffold,
                () => Scaffold(ctx, id.Text ?? string.Empty),
                primary: true,
                closeModal: false)));
        PluginModal.EscToHide(ctx, id);
        ctx.Owner.ShowModal(frame);
        id.Focus();
    }

    internal static void OpenLoad(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var path = new TextBox();

        Border frame = PluginModal.Frame(ctx, Strings.PmLoadTitle, out StackPanel content);
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldPath, path));
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmLoadHint,
            Foreground = PluginUi.Muted,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(PluginModal.ButtonRow(
            PluginModal.ActionButton(
                ctx,
                Strings.PmLoad,
                () => _ = LoadAsync(ctx, path.Text ?? string.Empty),
                primary: true,
                closeModal: false)));
        PluginModal.EscToHide(ctx, path);
        ctx.Owner.ShowModal(frame);
        path.Focus();
    }

    private static void Scaffold(PluginManagerContext ctx, string id)
    {
        if (ctx.Host is null)
        {
            ctx.Owner.SetStatus(Strings.PmNoHost);
            return;
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            ctx.Owner.SetStatus(Strings.PmNewNeedsId);
            return;
        }

        ctx.Owner.HideModal();
        PluginActionResult result = ctx.Host.Scaffold(id.Trim());
        ctx.Owner.SetStatus(result.Message);
        ctx.Log(result.Message);
    }

    private static async Task LoadAsync(PluginManagerContext ctx, string path)
    {
        if (ctx.Host is null)
        {
            ctx.Owner.SetStatus(Strings.PmNoHost);
            return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            ctx.Owner.SetStatus(Strings.PmLoadNeedsPath);
            return;
        }

        ctx.Owner.HideModal();
        try
        {
            PluginActionResult result = await ctx.Host.LoadAsync(path.Trim()).ConfigureAwait(true);
            ctx.Owner.SetStatus(result.Message);
            ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ctx.Owner.SetStatus(ex.Message);
        }
    }
}
