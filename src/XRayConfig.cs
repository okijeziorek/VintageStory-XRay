using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Vintagestory.API.Client;

namespace VintageStoryXRay;

public sealed class XRayConfig
{
    public float Opacity { get; set; } = 0.08f;
    public int MaxDistance { get; set; } = 128;
    public bool HighlightTargets { get; set; } = true;
    public bool ShowCaves { get; set; } = false;

    public HashSet<string> TargetBlocks { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "ore-",
        "ore",
        "geode"
    };

    public static XRayConfig Load(ICoreClientAPI api)
    {
        try
        {
            string path = Path.Combine(api.GetOrCreateDataPath("ModConfig"), "vsxray.json");
            if (!File.Exists(path))
            {
                var fresh = new XRayConfig();
                File.WriteAllText(path, JsonSerializer.Serialize(fresh, new JsonSerializerOptions { WriteIndented = true }));
                return fresh;
            }

            return JsonSerializer.Deserialize<XRayConfig>(File.ReadAllText(path)) ?? new XRayConfig();
        }
        catch (Exception e)
        {
            api.Logger.Warning("[VSXRay] Could not load config: {0}", e.Message);
            return new XRayConfig();
        }
    }
}
