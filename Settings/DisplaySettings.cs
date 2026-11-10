
namespace GameProject.Settings;

public sealed class DisplaySettings
{
    private int _width = 1280;
    private int _height = 720;

    public int Width
    {
        get => _width;
        set => _width = ValidateDimension(value, nameof(Width));
    }

    public int Height
    {
        get => _height;
        set => _height = ValidateDimension(value, nameof(Height));
    }

    public bool Fullscreen { get; set; }

    public bool VSync { get; set; }

    public DisplaySettings CreateCopy()
    {
        return new DisplaySettings
        {
            Width = Width,
            Height = Height,
            Fullscreen = Fullscreen,
            VSync = VSync
        };
    }

    private static int ValidateDimension(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                name,
                "Display dimensions must be greater than zero.");
        }

        return value;
    }
}
