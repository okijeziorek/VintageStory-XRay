using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace VintageStoryXRay;

public sealed class XRayModSystem : ModSystem
{
    public static XRayModSystem? Instance { get; private set; }
    public XRayConfig Config { get; private set; } = new();
    public bool Enabled { get; private set; }

    private ICoreClientAPI? capi;
    private KeyMapping? toggleKey;

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        Instance = this;
        capi = api;
        Config = XRayConfig.Load(api);

        toggleKey = api.Input.RegisterKey("vsxray-toggle", "Toggle X-Ray", GlKeys.X, HotkeyType.CharacterControls);
        api.Event.KeyDown += OnKeyDown;

        api.Logger.Notification("[VSXRay] Client-side X-Ray core loaded. Toggle: X");
    }

    private void OnKeyDown(KeyEvent args)
    {
        if (toggleKey != null && args.KeyCode == toggleKey.KeyCode)
        {
            Enabled = !Enabled;
            capi?.Logger.Notification($"[VSXRay] {(Enabled ? "enabled" : "disabled")}");
            XRayRendererController.SetEnabled(Enabled);
        }
    }

    public override void Dispose()
    {
        if (capi != null) capi.Event.KeyDown -= OnKeyDown;
        XRayRendererController.SetEnabled(false);
        Instance = null;
        base.Dispose();
    }
}
