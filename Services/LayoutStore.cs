using System.IO;
using System.Text.Json;
using SurfaceTouchDeck.Models;

namespace SurfaceTouchDeck.Services;

public sealed class LayoutStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string BasePath { get; } = AppContext.BaseDirectory;

    public IReadOnlyList<string> ListLayoutNames()
    {
        return Directory.EnumerateFiles(BasePath, "layout*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public LayoutDefinition LoadOrCreate(string name)
    {
        var path = LayoutPath(name);
        if (!File.Exists(path))
        {
            var created = CreateDefault(name);
            Save(created);
            return created;
        }

        var text = File.ReadAllText(path);
        return JsonSerializer.Deserialize<LayoutDefinition>(text) ?? CreateDefault(name);
    }

    public void Save(LayoutDefinition layout)
    {
        File.WriteAllText(LayoutPath(layout.Name), JsonSerializer.Serialize(layout, JsonOptions));
    }

    public void Delete(string name)
    {
        var path = LayoutPath(name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string LayoutPath(string name) => Path.Combine(BasePath, $"{name}.json");

    private static LayoutDefinition CreateDefault(string name)
    {
        return new LayoutDefinition
        {
            Name = name,
            Controls =
            [
                new() { Id = "home", ButtonName = "single-home", Kind = ControlKind.Home, X = 900, Y = 500, Size = 1.0 },
                new() { Id = "a", ButtonName = "single-A", Kind = ControlKind.Button, X = 1350, Y = 430, Size = 1.0, IsClassicClusterMember = true },
                new() { Id = "b", ButtonName = "single-B", Kind = ControlKind.Button, X = 1430, Y = 360, Size = 1.0, IsClassicClusterMember = true },
                new() { Id = "x", ButtonName = "single-X", Kind = ControlKind.Button, X = 1270, Y = 360, Size = 1.0, IsClassicClusterMember = true },
                new() { Id = "y", ButtonName = "single-Y", Kind = ControlKind.Button, X = 1350, Y = 290, Size = 1.0, IsClassicClusterMember = true }
            ]
        };
    }
}
