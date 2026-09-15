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

        try
        {
            XRayConfig config;
            try
            {
                config = api.LoadModConfig<XRayConfig>("vsxray.json") ?? XRayConfig.Default();
            }
            catch (Exception ex)
            {
                XRaySafety.Report(api, ex, "Could not load configuration; using defaults");
                config = XRayConfig.Default();
            }

            XRayRuntime.State = new XRayState(config);

            RegisterHotkeys(api);
            CreateMenu(api);

            try
            {
                harmony = new Harmony("okijeziorek.vintagestoryxray");
                XRayMeshPatch.Apply(harmony);
            }
            catch (Exception ex)
            {
                XRaySafety.DisableAfterFailure(api, ex, "Could not install rendering patches; X-Ray was disabled");
                harmony = null;
            }

            SaveConfig(api);
        }
        catch (Exception ex)
        {
            // A mod initialization failure must not propagate into the game client.
            XRaySafety.DisableAfterFailure(api, ex, "Fatal mod initialization error; disabling X-Ray");
            SafeDisposeMenu();
            SafeUnpatch();
        }
    }

    private void RegisterHotkeys(ICoreClientAPI api)
    {
        try
        {
            api.Input.RegisterHotKey(
                "vintagestoryxray.toggle",
                "Toggle X-Ray",
                GlKeys.F8,
                HotkeyType.CharacterControls
            );
            api.Input.SetHotKeyHandler("vintagestoryxray.toggle", _ =>
            {
                try
                {
                    if (XRayRuntime.State == null) return true;
                    XRayRuntime.State.Toggle(api);
                    SaveConfig(api);
                }
                catch (Exception ex)
                {
                    XRaySafety.DisableAfterFailure(api, ex, "F8 toggle failed");
                }

                return true;
            });

            api.Input.RegisterHotKey(
                "vintagestoryxray.menu",
                "X-Ray Menu",
                GlKeys.F11,
                HotkeyType.GUIOrOtherControls
            );

            api.Input.SetHotKeyHandler("vintagestoryxray.menu", _ =>
            {
                try
                {
                    menu?.Toggle();
                }
                catch (Exception ex)
                {
                    XRaySafety.Report(api, ex, "F11 menu toggle failed");
                }

                return true;
            });
        }
        catch (Exception ex)
        {
            XRaySafety.Report(api, ex, "Could not register X-Ray hotkeys");
        }
    }

    private void CreateMenu(ICoreClientAPI api)
    {
        try
        {
            menu = new XRayMenuDialog(api);
            api.Gui.RegisterDialog(menu);
        }
        catch (Exception ex)
        {
            menu = null;
            XRaySafety.Report(api, ex, "Could not create X-Ray menu");
        }
    }

    public override void Dispose()
    {
        try
        {
            if (capi != null && XRayRuntime.State != null)
            {
                SaveConfig(capi);
            }
        }
        catch (Exception ex)
        {
            XRaySafety.Report(capi, ex, "Could not save configuration during shutdown");
        }

        SafeUnpatch();
        SafeDisposeMenu();
        capi = null;
        XRayRuntime.State = null;
        base.Dispose();
    }

    private void SafeUnpatch()
    {
        if (harmony == null) return;

        try
        {
            harmony.UnpatchAll(harmony.Id);
        }
        catch (Exception ex)
        {
            XRaySafety.Report(capi, ex, "Could not remove Harmony patches");
        }
        finally
        {
            harmony = null;
        }
    }

    private void SafeDisposeMenu()
    {
        if (menu == null) return;

        try
        {
            menu.TryClose();
        }
        catch (Exception ex)
        {
            XRaySafety.Report(capi, ex, "Could not close X-Ray menu");
        }
        finally
        {
            menu = null;
        }
    }

    private static void SaveConfig(ICoreClientAPI api)
    {
        try
        {
            api.StoreModConfig(XRayRuntime.State?.Config ?? XRayConfig.Default(), "vsxray.json");
        }
        catch (Exception ex)
        {
            XRaySafety.Report(api, ex, "Could not persist configuration");
        }
    }
}
