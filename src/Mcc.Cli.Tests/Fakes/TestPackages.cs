using DMCBK.PluginSdk;
namespace Mcc.Cli.Tests.Fakes;

internal static class TestPackages
{
    internal static string UserFolder(string package) => Path.Combine(Path.GetDirectoryName(package)!, "userdata", Path.GetFileName(package));
    internal static IReadOnlyList<PluginInstallation> Read(string root)
    {
        if (!Directory.Exists(root)) return [];
        var packages = new List<PluginInstallation>();
        foreach (string folder in Directory.EnumerateDirectories(root))
        {
            if (Path.GetFileName(folder).StartsWith('.')) continue;
            string path = Path.Combine(folder, PluginManifest.FileName);
            if (!File.Exists(path)) continue;
            string text = File.ReadAllText(path);
            if (!PluginManifest.TryParse(text, out PluginManifest manifest, out _)) continue;
            bool enabled = !System.Text.RegularExpressions.Regex.IsMatch(text, @"(?m)^\s*enabled\s*=\s*false");
            packages.Add(new(manifest, folder, Path.Combine(root, "userdata", manifest.Id), enabled));
        }
        PluginDependencyGraph.Result graph = PluginDependencyGraph.Build(packages.Select(package => package.Manifest));
        return graph.Order.Select(id => packages.Single(package => package.Manifest.Id == id)).ToArray();
    }
}
