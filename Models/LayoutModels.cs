namespace SurfaceTouchDeck.Models;

public enum EditMode
{
    Green,
    Purple
}

public enum ControlKind
{
    Button,
    Joystick,
    Home,
    Utility
}

public sealed class ControlDefinition
{
    public required string Id { get; init; }
    public required string ButtonName { get; init; }
    public required ControlKind Kind { get; init; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Size { get; set; } = 1.0;
    public bool Enabled { get; set; } = true;
    public bool IsClassicClusterMember { get; set; }
    public string AssignedKey { get; set; } = "NULL";
}

public sealed class LayoutDefinition
{
    public required string Name { get; init; }
    public string StyleName { get; set; } = "default";
    public bool UseClassicAbxyCluster { get; set; }
    public double UiOpacity { get; set; } = 1;
    public double UiScale { get; set; } = 1;
    public List<ControlDefinition> Controls { get; set; } = [];
}
