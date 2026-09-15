using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Input;

namespace VintageStoryXRay;

public sealed class XRayModSystem : ModSystem
{
    private Harmony? harmony;
    private ICoreClientAPI? capi;
    private XRayState? state;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        state = new XRayState(XRayConfig.Default());

        api.Input.RegisterHotKey(
            "vintagestoryxray.toggle",
            "Toggle X-Ray",
            GlKeys.F8,
            HotkeyType.CharacterControls
        );

        api.Input.SetHotKeyHandler("vintagestoryxray.toggle", _ =>
        {
            state.Toggle(api);
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

        capi = null;
        state = null;
        base.Dispose();
    }

    internal static XRayState? CurrentState { get; private set; }
}
