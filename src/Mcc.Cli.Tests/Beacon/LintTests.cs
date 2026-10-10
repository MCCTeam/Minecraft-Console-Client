using Mcc.Cli.BeaconTooling;
using Mcc.Cli;
using System.Text.Json;
using DMCBK.Core.Beacon;
using Xunit;

namespace Mcc.Cli.Tests.Beacon;

/// <summary>
/// Lint engine tests: JSON goldens (clean file, B1001 manifest error, usage failure), the 0, 1, and 2 exit-code matrix, strict escalation, fix preview-first plus meaning preservation, and the stdin generate, lint, and repair loop.
/// </summary>
public sealed class LintTests : IDisposable
{
    private readonly List<string> _roots = [];

    public void Dispose()
    {
        foreach (string root in _roots)
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
            }
        }
    }

    private string NewRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-lint-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _roots.Add(root);
        return root;
    }

    private string WriteFile(string root, string name, string source)
    {
        string path = Path.Combine(root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, source);
        return path;
    }

    private const string CleanSource =
        "# beacon 1\n# needs: chat.send\n\non join:\n  say \"Welcome, {player}!\"\nend on\n";

    private const string ManifestErrorSource =
        "# beacon 1\n# needs: server.send\n\non join:\n  say \"hi\"\nend on\n";

    private static JsonDocument ParseJson(string stdout)
        => JsonDocument.Parse(stdout);

    private static JsonElement SingleDiagnostic(JsonDocument json)
    {
        Assert.True(json.RootElement.TryGetProperty("diagnostics", out JsonElement diagnostics));
        Assert.Equal(JsonValueKind.Array, diagnostics.ValueKind);
        return Assert.Single(diagnostics.EnumerateArray().ToList());
    }

    #region JSON goldens

    [Fact]
    public void CleanFile_JsonGolden_OkWithZeroCounts()
    {
        string root = NewRoot();
        string path = WriteFile(root, "welcome.bcn", CleanSource);

        IReadOnlyList<BeaconLintReport> reports = BeaconLint.LintFiles([path]);
        BeaconLintReport report = Assert.Single(reports);
        Assert.True(report.Ok);
        Assert.Empty(report.Diagnostics);
        Assert.Equal(["chat.send"], report.Permissions.OrderBy(p => p, StringComparer.Ordinal).ToArray());

        string json = BeaconLint.ToJson(reports);
        using JsonDocument parsed = ParseJson(json);
        Assert.True(parsed.RootElement.GetProperty("files")[0].GetProperty("ok").GetBoolean());
        Assert.Equal(0, parsed.RootElement.GetProperty("summary").GetProperty("errors").GetInt32());
        Assert.Equal(0, parsed.RootElement.GetProperty("summary").GetProperty("warnings").GetInt32());
    }

    [Fact]
    public void ManifestError_JsonGolden_CodeSpanSuggestionShape()
    {
        // A manifest refusal maps to B1001 in the shipped registry, so the golden pins the B1001 shape.
        string root = NewRoot();
        string path = WriteFile(root, "quiz.bcn", ManifestErrorSource);

        IReadOnlyList<BeaconLintReport> reports = BeaconLint.LintFiles([path]);
        BeaconLintReport report = Assert.Single(reports);
        Assert.False(report.Ok);
        BeaconDiagnostic error = Assert.Single(report.Diagnostics, d => d.Severity == BeaconSeverity.Error);
        Assert.Equal(BeaconDiagnosticCodes.ManifestNeedsMismatch, error.Code);
        Assert.Equal("quiz.bcn", error.Span.File);
        Assert.Equal(1, error.Span.Line);
        Assert.Contains("chat.send", error.Suggestion ?? string.Empty, StringComparison.Ordinal);

        string json = BeaconLint.ToJson(reports);
        using JsonDocument parsed = ParseJson(json);
        JsonElement diag = SingleDiagnostic(parsed);
        Assert.Equal("B1001", diag.GetProperty("code").GetString());
        Assert.Equal("error", diag.GetProperty("severity").GetString());
        Assert.Equal("quiz.bcn", diag.GetProperty("file").GetString());
        Assert.Equal(1, diag.GetProperty("line").GetInt32());
        Assert.Equal(1, diag.GetProperty("col").GetInt32());
        Assert.Contains("chat.send", diag.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.Contains("chat.send", diag.GetProperty("suggestion").GetString(), StringComparison.Ordinal);
        Assert.Equal(1, parsed.RootElement.GetProperty("summary").GetProperty("errors").GetInt32());
    }

    [Fact]
    public void UsageFailure_JsonGolden_ExitTwoWithStderr()
    {
        Assert.False(BeaconLintCli.TryParse(["--format", "yaml", "a.bcn"], out _, out string error));
        Assert.NotEmpty(error);

        var parsed = new BeaconLintRequest([], "text", null, Strict: false, Fix: false, UseStdin: false, null);
        int exit = BeaconLintCli.Execute(parsed, stdinText: null, out _, out string stderr);
        Assert.Equal(BeaconLint.ExitCodes.Usage, exit);
        Assert.NotEmpty(stderr);
    }

    #endregion
    #region Exit-code matrix

    [Fact]
    public void ExitCodeMatrix_CleanIsZero_ErrorsAreOne_UsageIsTwo()
    {
        string root = NewRoot();
        string clean = WriteFile(root, "clean.bcn", CleanSource);
        string broken = WriteFile(root, "broken.bcn", ManifestErrorSource);

        Assert.True(BeaconLintCli.TryParse(["--format", "json", clean], out BeaconLintRequest? cleanArgs, out _));
        Assert.Equal(BeaconLint.ExitCodes.Clean, BeaconLintCli.Execute(cleanArgs!, stdinText: null, out _, out _));

        Assert.True(BeaconLintCli.TryParse([broken], out BeaconLintRequest? brokenArgs, out _));
        Assert.Equal(BeaconLint.ExitCodes.Errors, BeaconLintCli.Execute(brokenArgs!, stdinText: null, out _, out _));

        var noFiles = new BeaconLintRequest([], "text", null, Strict: false, Fix: false, UseStdin: false, null);
        Assert.Equal(BeaconLint.ExitCodes.Usage, BeaconLintCli.Execute(noFiles, stdinText: null, out _, out _));
    }

    [Fact]
    public void WarningsAlone_StillExitClean()
    {
        string root = NewRoot();
        string path = WriteFile(root, "wants.bcn", "# beacon 1\n# wants: shop.buy\n\non join:\n  show \"hi\"\nend on\n");

        Assert.True(BeaconLintCli.TryParse([path], out BeaconLintRequest? parsed, out string _parseError));
        int exit = BeaconLintCli.Execute(parsed!, stdinText: null, out string stdout, out string _executeStderr);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        using JsonDocument _doc = ParseJson(BeaconLint.ToJson(BeaconLint.LintFiles([path])));
    }

    #endregion
    #region strict

    [Fact]
    public void Strict_EscalatesUnresolvableExtern_FromWarningToError()
    {
        string root = NewRoot();
        string path = WriteFile(
            root, "shop.bcn", "# beacon 1\n\nextern price_of from \"shop\"\nshow price_of(\"bread\")\n");

        BeaconLintReport loose = Assert.Single(BeaconLint.LintFiles([path]));
        BeaconDiagnostic warning = Assert.Single(
            loose.Diagnostics, d => d.Code == BeaconDiagnosticCodes.UnresolvedBridge);
        Assert.Equal(BeaconSeverity.Warning, warning.Severity);
        Assert.True(loose.Ok);

        var strict = new BeaconLintOptions(Strict: true);
        BeaconLintReport gated = Assert.Single(BeaconLint.LintFiles([path], strict));
        BeaconDiagnostic escalated = Assert.Single(
            gated.Diagnostics, d => d.Code == BeaconDiagnosticCodes.UnresolvedBridge);
        Assert.Equal(BeaconSeverity.Error, escalated.Severity);
        Assert.Contains("strict", escalated.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(gated.Ok);
    }

    [Fact]
    public void Strict_EscalatesMissingProviders()
    {
        string root = NewRoot();
        string path = WriteFile(
            root, "wants.bcn", "# beacon 1\n# wants: shop.buy\n\non join:\n  show \"hi\"\nend on\n");

        BeaconLintReport loose = Assert.Single(BeaconLint.LintFiles([path]));
        Assert.Contains(loose.Diagnostics, d =>
            d.Code == BeaconDiagnosticCodes.ManifestWantsUnavailable && d.Severity == BeaconSeverity.Warning);

        var strict = new BeaconLintOptions(Strict: true);
        BeaconLintReport gated = Assert.Single(BeaconLint.LintFiles([path], strict));
        Assert.Contains(gated.Diagnostics, d =>
            d.Code == BeaconDiagnosticCodes.ManifestWantsUnavailable && d.Severity == BeaconSeverity.Error);
        Assert.False(gated.Ok);
    }

    #endregion
    #region target-lib

    [Fact]
    public void TargetLib_FlagsInteropSurfaceNewerThanTarget()
    {
        string root = NewRoot();
        string path = WriteFile(
            root, "price.bcn", "# beacon 1\n\ncommand \"/price <item>\":\n  show \"x\"\nend command\n");

        BeaconLintReport current = Assert.Single(BeaconLint.LintFiles([path]));
        Assert.DoesNotContain(current.Diagnostics, d => d.Code == BeaconDiagnosticCodes.LibNovelty);

        var targeted = new BeaconLintOptions(TargetLib: 1);
        BeaconLintReport gated = Assert.Single(BeaconLint.LintFiles([path], targeted));
        BeaconDiagnostic novelty = Assert.Single(
            gated.Diagnostics, d => d.Code == BeaconDiagnosticCodes.LibNovelty);
        Assert.Equal(BeaconSeverity.Error, novelty.Severity);
        Assert.Contains("lib 2", novelty.Message, StringComparison.Ordinal);
        Assert.False(gated.Ok);
    }

    #endregion
    #region imports / call chasing

    [Fact]
    public void ImportChase_UnionsPermissionsIntoRootManifestCheck()
    {
        string root = NewRoot();
        WriteFile(root, Path.Combine("lib", "econ.bcn"),
            "# beacon 1\n\nfunction price(item)\n  server \"/price {item}\"\n  return 3\nend function\n");
        string path = WriteFile(root, "shop.bcn",
            "# beacon 1\n# needs: chat.send\nimport \"lib/econ.bcn\" as econ\n\non chat when message contains \"!p\":\n  say \"ok\"\nend on\n");

        BeaconLintReport report = Assert.Single(BeaconLint.LintFiles([path]));
        Assert.False(report.Ok);
        BeaconDiagnostic missing = Assert.Single(
            report.Diagnostics, d => d.Code == BeaconDiagnosticCodes.ManifestNeedsMismatch);
        Assert.Contains("server.send", missing.Message, StringComparison.Ordinal);
        Assert.Equal(
            ["chat.send", "server.send"],
            report.Permissions.OrderBy(p => p, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void ImportChase_CoveringUnion_LintsClean()
    {
        string root = NewRoot();
        WriteFile(root, Path.Combine("lib", "econ.bcn"),
            "# beacon 1\n# needs: server.send\n\nfunction price(item)\n  server \"/price {item}\"\n  return 3\nend function\n");
        string path = WriteFile(root, "shop.bcn",
            "# beacon 1\n# needs: chat.send server.send\nimport \"lib/econ.bcn\" as econ\n\non chat when message contains \"!p\":\n  say \"ok\"\nend on\n");

        BeaconLintReport report = Assert.Single(BeaconLint.LintFiles([path]));
        Assert.DoesNotContain(report.Diagnostics, d => d.Severity == BeaconSeverity.Error);
    }

    [Fact]
    public void ImportChase_MissingFile_IsFailClosedError()
    {
        string root = NewRoot();
        string path = WriteFile(root, "root.bcn",
            "# beacon 1\nimport \"lib/missing.bcn\" as econ\n\non start:\n  show \"hi\"\nend on\n");

        BeaconLintReport report = Assert.Single(BeaconLint.LintFiles([path]));
        BeaconDiagnostic missing = Assert.Single(
            report.Diagnostics, d => d.Code == BeaconDiagnosticCodes.ImportNotFound);
        Assert.Equal(BeaconSeverity.Error, missing.Severity);
        Assert.Contains("lib/missing.bcn", missing.Message, StringComparison.Ordinal);
        Assert.False(report.Ok);
    }

    [Fact]
    public void ImportChase_Cycle_NamesTheCycle()
    {
        string root = NewRoot();
        WriteFile(root, "a.bcn", "# beacon 1\nimport \"b.bcn\" as b\n\non start:\n  show \"a\"\nend on\n");
        string path = WriteFile(root, "b.bcn", "# beacon 1\nimport \"a.bcn\" as a\n\non start:\n  show \"b\"\nend on\n");

        BeaconLintReport report = Assert.Single(BeaconLint.LintFiles([path]));
        BeaconDiagnostic cycle = Assert.Single(
            report.Diagnostics, d => d.Code == BeaconDiagnosticCodes.ImportCycle);
        Assert.Equal(BeaconSeverity.Error, cycle.Severity);
        Assert.Contains("a.bcn", cycle.Message, StringComparison.Ordinal);
        Assert.Contains("b.bcn", cycle.Message, StringComparison.Ordinal);
        Assert.False(report.Ok);
    }

    [Fact]
    public void CallChase_MissingTarget_WarnsLoose_ErrorsStrict()
    {
        string root = NewRoot();
        string path = WriteFile(root, "caller.bcn",
            "# beacon 1\n\non start:\n  set r to call \"shop.daily_report\"()\n  show \"{r}\"\nend on\n");

        BeaconLintReport loose = Assert.Single(BeaconLint.LintFiles([path]));
        Assert.Contains(loose.Diagnostics, d =>
            d.Code == BeaconDiagnosticCodes.UnresolvedBridge && d.Severity == BeaconSeverity.Warning);

        var strict = new BeaconLintOptions(Strict: true);
        BeaconLintReport gated = Assert.Single(BeaconLint.LintFiles([path], strict));
        Assert.Contains(gated.Diagnostics, d =>
            d.Code == BeaconDiagnosticCodes.UnresolvedBridge && d.Severity == BeaconSeverity.Error);
    }

    [Fact]
    public void CallChase_PresentTarget_UnionsItsNeeds()
    {
        string root = NewRoot();
        WriteFile(root, "shop.bcn",
            "# beacon 1\n# needs: server.send\n\non start:\n  server \"/open\"\nend on\n");
        string path = WriteFile(root, "caller.bcn",
            "# beacon 1\n# needs: chat.send\n\non start:\n  set r to call \"shop.daily_report\"()\n  show \"{r}\"\nend on\n");

        BeaconLintReport report = Assert.Single(BeaconLint.LintFiles([path]));
        BeaconDiagnostic missing = Assert.Single(
            report.Diagnostics, d => d.Code == BeaconDiagnosticCodes.ManifestNeedsMismatch);
        Assert.Contains("server.send", missing.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(report.Diagnostics, d => d.Code == BeaconDiagnosticCodes.UnresolvedBridge);
    }

    #endregion
    #region fix

    [Fact]
    public void FixPreview_AppliesOnlyEndLabelsAndCancelEvent()
    {
        const string source =
            "# beacon 1\n\non chat when message contains \"x\"\n  cancel event\nend\n";
        BeaconFixPreview preview = BeaconFix.Preview("fix.bcn", source);

        Assert.True(preview.HasFixes);
        Assert.Equal(2, preview.Edits.Count);
        Assert.Contains("stop event", preview.FixedSource, StringComparison.Ordinal);
        Assert.Contains("end on", preview.FixedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("cancel event", preview.FixedSource, StringComparison.Ordinal);
        Assert.Contains("- ", preview.Diff, StringComparison.Ordinal);
        Assert.Contains("+ ", preview.Diff, StringComparison.Ordinal);
    }

    [Fact]
    public void FixPreview_NeverTouchesMeaning()
    {
        // `=` versus `is` and a missing `wait` stay suggestions: the fixed source keeps both lines byte-identical while the diagnostics still point at them.
        const string source =
            "# beacon 1\n\non chat when message contains \"x\"\n  cancel event\nend on\n"
            + "on join:\n  if player = \"Steve\"\n    say \"hi\"\n  end if\nend on\n";
        BeaconFixPreview preview = BeaconFix.Preview("meaning.bcn", source);

        Assert.True(preview.HasFixes);
        Assert.Contains("stop event", preview.FixedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("cancel event", preview.FixedSource, StringComparison.Ordinal);
        Assert.Contains("if player = \"Steve\"", preview.FixedSource, StringComparison.Ordinal);

        BeaconLintReport relinted = BeaconLint.LintSource("meaning.bcn", preview.FixedSource);
        Assert.DoesNotContain(relinted.Diagnostics, d => d.Code == BeaconDiagnosticCodes.ForgivenForm);
    }

    [Fact]
    public void FixPreview_SelfValidates_RelintIntroducesNoNewErrors()
    {
        const string source =
            "# beacon 1\n\non chat when message contains \"x\"\n  cancel event\nend\n";
        BeaconFixPreview preview = BeaconFix.Preview("fix.bcn", source);
        Assert.True(preview.HasFixes);

        BeaconLintReport before = BeaconLint.LintSource("fix.bcn", source);
        BeaconLintReport after = BeaconLint.LintSource("fix.bcn", preview.FixedSource);
        int errorsBefore = before.Diagnostics.Count(d => d.Severity == BeaconSeverity.Error);
        int errorsAfter = after.Diagnostics.Count(d => d.Severity == BeaconSeverity.Error);
        Assert.True(errorsAfter <= errorsBefore);
    }

    [Fact]
    public void FixCli_PrintsDiffFirst_ThenApplies()
    {
        string root = NewRoot();
        string path = WriteFile(root, "fix.bcn",
            "# beacon 1\n\non chat when message contains \"x\"\n  cancel event\nend\n");

        Assert.True(BeaconLintCli.TryParse(["--fix", path], out BeaconLintRequest? parsed, out _));
        int exit = BeaconLintCli.Execute(parsed!, stdinText: null, out string stdout, out _);

        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        string fixedSource = File.ReadAllText(path);
        Assert.Contains("stop event", fixedSource, StringComparison.Ordinal);
        Assert.Contains("end on", fixedSource, StringComparison.Ordinal);
        int diffAt = stdout.IndexOf("cancel event", StringComparison.Ordinal);
        int appliedAt = stdout.IndexOf("applied", StringComparison.OrdinalIgnoreCase);
        Assert.True(diffAt >= 0 && appliedAt > diffAt);
    }

    #endregion
    #region stdin loop

    [Fact]
    public void StdinLoop_LintsPipedSourceUnderGivenName_ThenRepairs()
    {
        const string piped = "# beacon 1\n# needs: server.send\n\non join:\n  say \"hi\"\nend on\n";
        var parsed = new BeaconLintRequest(
            [], "json", TargetLib: null, Strict: false, Fix: false, UseStdin: true, StdinName: "quiz.bcn");

        int exit = BeaconLintCli.Execute(parsed, stdinText: piped, out string stdout, out _);
        Assert.Equal(BeaconLint.ExitCodes.Errors, exit);
        using JsonDocument json = ParseJson(stdout);
        JsonElement diag = SingleDiagnostic(json);
        Assert.Equal("quiz.bcn", diag.GetProperty("file").GetString());
        Assert.Equal("B1001", diag.GetProperty("code").GetString());
        string suggestion = diag.GetProperty("suggestion").GetString() ?? string.Empty;

        // The agent loop closes: the paste-ready suggestion repairs the source, which re-lints clean.
        string repaired = "# beacon 1\n" + suggestion + "\n\non join:\n  say \"hi\"\nend on\n";
        var relint = new BeaconLintRequest(
            [], "json", TargetLib: null, Strict: false, Fix: false, UseStdin: true, StdinName: "quiz.bcn");
        int exit2 = BeaconLintCli.Execute(relint, stdinText: repaired, out string stdout2, out _);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit2);
        using JsonDocument json2 = ParseJson(stdout2);
        Assert.Equal(0, json2.RootElement.GetProperty("summary").GetProperty("errors").GetInt32());
    }
    #endregion
}
