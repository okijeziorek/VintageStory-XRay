using System.Text.Json;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Input;
using Vintagestory.API.Datastructures;

namespace VintageStoryXRay;

public sealed class XRayModSystem : ModSystem
{
    private Harmony? harmony;

    public override void StartClientSide(ICoreClientAPI api)
    {
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

        try
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            api.StoreModConfig(JsonObject.FromJson(json), "vsxray.json");
        }
        catch
        {
            // A missing optional config file must not prevent the mod from loading.
        }
    }

    public override void Dispose()
    {
        if (harmony != null)
        {
            harmony.UnpatchAll(harmony.Id);
            harmony = null;
        }

        XRayRuntime.State = null;
        base.Dispose();
    }
}
