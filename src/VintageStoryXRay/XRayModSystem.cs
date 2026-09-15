using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Input;

namespace VintageStoryXRay;

public sealed class XRayModSystem : ModSystem
{
    private Harmony? harmony;
    private ICoreClientAPI? capi;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        XRayRuntime.State = new XRayState(XRayConfig.Default());

        api.Input.RegisterHotKey(
            "vintagestoryxray.toggle",
            "Toggle X-Ray",
            GlKeys.F8,
            HotkeyType.CharacterControls
        );

        api.Input.SetHotKeyHandler("vintagestoryxray.toggle", _ =>
        {
            XRayRuntime.State!.Toggle(api);
            return true;
        });

        harmony = new Harmony("okijeziorek.vintagestoryxray");
        XRayMeshPatch.Apply(harmony);
    }

    public override void Dispose()
    {
        if (harmony != null)
        {
            harmony.UnpatchAll(harmony.Id);
            harmony = null;
        }

        XRayRuntime.State = null;
        capi = null;
        base.Dispose();
    }
}
