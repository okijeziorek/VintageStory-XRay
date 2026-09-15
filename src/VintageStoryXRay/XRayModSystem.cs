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

        XRayConfig config;
        try
        {
            config = api.LoadModConfig<XRayConfig>("vsxray.json") ?? XRayConfig.Default();
        }
        catch
        {
            config = XRayConfig.Default();
        }

        XRayRuntime.State = new XRayState(config);

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

        api.StoreModConfig(new Vintagestory.API.Datastructures.JsonObject(config), "vsxray.json");
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
