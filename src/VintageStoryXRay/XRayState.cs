using Vintagestory.API.Client;

namespace VintageStoryXRay;

public sealed class XRayState
{
    public bool Enabled { get; private set; }
    public XRayConfig Config { get; }

    public XRayState(XRayConfig config)
    {
        Config = config;
        Enabled = config.Enabled;
    }

    public void Toggle(ICoreClientAPI api)
    {
        Enabled = !Enabled;
        Config.Enabled = Enabled;
        XRayMeshPatch.Invalidate(api);
    }
}
