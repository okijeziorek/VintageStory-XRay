namespace VintageStoryXRay;

public sealed class XRayConfig
{
    public bool Enabled { get; set; }
    public byte WallAlpha { get; set; } = 35;
    public int Range { get; set; } = 96;
    public bool IncludeTerrain { get; set; } = true;
    public bool IncludeTransparentBlocks { get; set; } = true;
    public bool ShowHud { get; set; } = true;

    public static XRayConfig Default() => new();
}
