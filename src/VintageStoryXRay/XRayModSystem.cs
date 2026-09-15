using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Input;

namespace VintageStoryXRay;

public sealed class XRayModSystem : ModSystem
{
    private Harmony? harmony;
    private XRayMenuDialog? menu;
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
            SaveConfig(api);
            return true;
        });

        api.Input.RegisterHotKey(
            "vintagestoryxray.menu",
            "X-Ray Menu",
            GlKeys.F11,
            HotkeyType.GUIOrOtherControls
        );

        menu = new XRayMenuDialog(api);
        api.Gui.RegisterDialog(menu);
        api.Input.SetHotKeyHandler("vintagestoryxray.menu", _ =>
        {
            menu!.Toggle();
            return true;
        });

        harmony = new Harmony("okijeziorek.vintagestoryxray");
        XRayMeshPatch.Apply(harmony);
        SaveConfig(api);
    }

    public override void Dispose()
    {
        if (capi != null && XRayRuntime.State != null)
        {
            SaveConfig(capi);
        }

        if (harmony != null)
        {
            harmony.UnpatchAll(harmony.Id);
            harmony = null;
        }

        menu?.TryClose();
        menu = null;
        capi = null;
        XRayRuntime.State = null;
        base.Dispose();
    }

    private static void SaveConfig(ICoreClientAPI api)
    {
        try
        {
            api.StoreModConfig(XRayRuntime.State?.Config ?? XRayConfig.Default(), "vsxray.json");
        }
        catch
        {
            // Configuration persistence must never prevent the client from loading the mod.
        }
    }
}
