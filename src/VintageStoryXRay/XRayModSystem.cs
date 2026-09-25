using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageStoryXRay;

public sealed class XRayModSystem : ModSystem
{
    private Harmony? harmony;
    private XRayMenuDialog? menu;
    private ClientFlightController? flight;
    private long? flightTickListenerId;
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
            flight = new ClientFlightController(api);
            XRayRuntime.Flight = flight;
            flightTickListenerId = api.Event.RegisterGameTickListener(flight.OnGameTick, 20);

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
                ToggleMenu(api, "F11");
                return true;
            });

            api.Input.RegisterHotKey(
                "vintagestoryxray.menu.alt",
                "Toggle X-Ray ClickGUI",
                GlKeys.RShift,
                HotkeyType.GUIOrOtherControls
            );
            api.Input.SetHotKeyHandler("vintagestoryxray.menu.alt", _ =>
            {
                ToggleMenu(api, "Right Shift");
                return true;
            });

            api.ChatCommands
                .Create("xraymenu")
                .WithDescription("Open or close the X-Ray ClickGUI.")
                .HandleWith(_ =>
                {
                    ToggleMenu(api, "/xraymenu");
                    return TextCommandResult.Success("X-Ray ClickGUI command handled.", null);
                });

            api.Input.RegisterHotKey(
                "vintagestoryxray.flight",
                "Toggle client flight",
                GlKeys.F7,
                HotkeyType.CharacterControls
            );
            api.Input.SetHotKeyHandler("vintagestoryxray.flight", _ =>
            {
                try
                {
                    if (XRayRuntime.State == null) return true;
                    bool enabled = !XRayRuntime.State.Config.ClientFlightEnabled;
                    XRayRuntime.State.Config.ClientFlightEnabled = enabled;
                    flight?.SetEnabled(enabled);
                    SaveConfig(api);
                }
                catch (Exception ex)
                {
                    XRaySafety.Report(api, ex, "Could not toggle client flight");
                }

                return true;
            });
        }
        catch (Exception ex)
        {
            XRaySafety.Report(api, ex, "Could not register X-Ray hotkeys");
        }
    }

    private void ToggleMenu(ICoreClientAPI api, string keyName)
    {
        try
        {
            if (menu == null)
            {
                api.ShowChatMessage("X-Ray settings did not initialize. Check the client log for the XRay mod error.");
            }
            else if (menu.IsOpened())
            {
                menu.TryClose();
            }
            else if (!menu.TryOpen())
            {
                api.ShowChatMessage($"Vintage Story could not open the X-Ray settings dialog ({keyName}).");
            }
        }
        catch (Exception ex)
        {
            XRaySafety.Report(api, ex, $"{keyName} menu toggle failed");
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
        if (flightTickListenerId.HasValue && capi != null)
        {
            try { capi.Event.UnregisterGameTickListener(flightTickListenerId.Value); }
            catch (Exception ex) { XRaySafety.Report(capi, ex, "Could not unregister client flight listener"); }
        }
        flight?.Dispose();
        flight = null;
        XRayRuntime.Flight = null;
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
