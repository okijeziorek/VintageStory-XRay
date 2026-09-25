namespace VintageStoryXRay;

public sealed class XRayConfig
{
    public bool Enabled { get; set; }
    public byte WallAlpha { get; set; } = 35;
    public int Range { get; set; } = 96;
    public bool IncludeTerrain { get; set; } = true;
    public bool IncludeTransparentBlocks { get; set; } = true;
    public bool ShowHud { get; set; } = true;
    public bool ClientFlightEnabled { get; set; }

    /// <summary>Keep blocks classified as ores fully visible while surrounding terrain is faded.</summary>
    public bool KeepOresVisible { get; set; } = true;

    /// <summary>Additional block-code fragments that should always remain fully visible.</summary>
    public List<string> VisibleBlockCodes { get; set; } = new();

    /// <summary>Case-insensitive path fragments used to recognize ore blocks.</summary>
    public List<string> OreCodePatterns { get; set; } = new() { "ore-", "-ore" };

    public static XRayConfig Default() => new();
}
