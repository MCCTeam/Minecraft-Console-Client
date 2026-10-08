using DMCBK.Core;
using DMCBK.Core.Configuration;
using DMCBK.PluginSdk;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mcc.Cli.Tests.Fakes;

/// <summary>
/// A temp plugins root with a non-started client and a host over it, for the tests that check what the host reports about folders on disk rather than what a running plugin does.
/// </summary>
internal sealed class PluginRootFixture : IDisposable
{
    public PluginRootFixture(PluginsConfig? limits = null)
    {
        Root = Path.Combine(Path.GetTempPath(), "mcc-plugin-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Client = new ClientBuilder().UseUsername("Tester").UseServer("localhost").UseCommands().UseBeacon().Build();
        Host = new PluginHost(
            Client, Root, NullLoggerFactory.Instance, Client.Translations, Client.Variables, null, limits);
        Host.InstallationSource = _ => Task.FromResult(TestPackages.Read(Root));
    }

    public string Root { get; }

    public Client Client { get; }

    public PluginHost Host { get; }

    /// <summary>Writes a folder with a manifest and an entry file, and returns the folder.</summary>
    public string WritePlugin(
        string id,
        string entry,
        string source,
        bool enabled = true,
        string? apiVersion = null,
        string extraKeys = "")
    {
        string folder = Path.Combine(Root, id);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, entry), source);
        File.WriteAllText(
            Path.Combine(folder, PluginManifest.FileName), TestManifest.Upgrade($"id = \"{id}\"\nversion = \"1.0.0\"\nentry = \"{entry}\"\n"
            + $"api-version = \"{apiVersion ?? PluginApiVersion.Current}\"\n"
            + $"enabled = {(enabled ? "true" : "false")}\n{extraKeys}"));
        return folder;
    }

    /// <summary>Writes one <c>lang/&lt;tag&gt;.toml</c> into a plugin folder.</summary>
    public static void WriteLang(string folder, string tag, string content)
    {
        string lang = Path.Combine(folder, PluginLocalization.FolderName);
        Directory.CreateDirectory(lang);
        File.WriteAllText(Path.Combine(lang, tag + ".toml"), content);
    }

    public void Dispose()
    {
        Client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
