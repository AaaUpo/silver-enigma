using System.IO;
using System.Windows.Media.Imaging;

namespace SurfaceTouchDeck.Services;

public sealed class StyleResolver
{
    private readonly string _basePath = AppContext.BaseDirectory;

    public IReadOnlyList<string> ListStyles()
    {
        return Directory.EnumerateDirectories(_basePath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    public BitmapImage? TryResolveIcon(string styleName, string buttonName, bool pressed)
    {
        var stylePath = Path.Combine(_basePath, styleName);
        if (!Directory.Exists(stylePath))
        {
            return null;
        }

        var pressedName = $"{buttonName}-pressed.ico";
        var defaultName = $"{buttonName}.ico";
        var preferred = pressed ? pressedName : defaultName;
        var fallback = pressed ? defaultName : null;

        var candidate = Path.Combine(stylePath, preferred);
        if (!File.Exists(candidate) && fallback is not null)
        {
            candidate = Path.Combine(stylePath, fallback);
        }

        if (!File.Exists(candidate))
        {
            return null;
        }

        return new BitmapImage(new Uri(candidate));
    }
}
