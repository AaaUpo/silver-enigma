namespace SurfaceTouchDeck.Models;

public enum JoystickTask
{
    Controller,
    Wasd,
    Arrows
}

public enum JoystickMode
{
    Freestyle,
    Fixed
}

public sealed class JoystickConfig
{
    public JoystickTask Task { get; set; } = JoystickTask.Wasd;
    public JoystickMode Mode { get; set; } = JoystickMode.Freestyle;
    public bool DefinedAreaEnabled { get; set; }
    public double ActiveAreaSize { get; set; } = 200;
}
