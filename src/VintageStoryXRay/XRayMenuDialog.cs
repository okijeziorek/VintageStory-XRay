using Vintagestory.API.Client;

namespace VintageStoryXRay;

/// <summary>Client-side X-Ray configuration window.</summary>
public sealed class XRayMenuDialog : GuiDialog
{
    private enum MenuCategory { Render, Movement, Interface }

    private readonly ICoreClientAPI clientApi;
    private MenuCategory selectedCategory;

    public XRayMenuDialog(ICoreClientAPI capi) : base(capi)
    {
        clientApi = capi;
        try
        {
            Compose();
        }
        catch (Exception ex)
        {
            XRaySafety.Report(capi, ex, "Could not compose X-Ray menu");
        }
    }

    public override string ToggleKeyCombinationCode => "vintagestoryxray.menu";

    private void Compose()
    {
        var bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(12);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var composer = clientApi.Gui.CreateCompo("vintagestoryxray.menu", bounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar("VSX ClickGUI", () => OnClose())
            .BeginChildElements(bgBounds.FlatCopy().FixedGrow(0, -10));

        composer
            .AddButton("Render", () => OnCategoryClick(MenuCategory.Render), ElementBounds.Fixed(0, 0, 125, 34))
            .AddButton("Movement", () => OnCategoryClick(MenuCategory.Movement), ElementBounds.Fixed(135, 0, 125, 34))
            .AddButton("Interface", () => OnCategoryClick(MenuCategory.Interface), ElementBounds.Fixed(270, 0, 125, 34));

        var state = XRayRuntime.State;
        if (state == null)
        {
            composer.AddStaticText("Client modules did not initialize.", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 45, 400, 30));
        }
        else if (selectedCategory == MenuCategory.Render)
        {
            composer.AddStaticText("RENDER MODULES", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 45, 400, 25));
            composer.AddSwitch(OnEnabledChanged, ElementBounds.Fixed(0, 75, 30, 30), "xray-enabled");
            composer.AddStaticText("X-Ray  [F8]", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 75, 250, 30));

            composer.AddStaticText("Wall alpha", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 115, 180, 25));
            composer.AddSlider(OnAlphaChanged, ElementBounds.Fixed(0, 145, 400, 30), "xray-alpha");

            composer.AddStaticText("Range", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 180, 180, 25));
            composer.AddSlider(OnRangeChanged, ElementBounds.Fixed(0, 210, 400, 30), "xray-range");

            composer.AddSwitch(OnTerrainChanged, ElementBounds.Fixed(0, 250, 30, 30), "xray-terrain");
            composer.AddStaticText("Process terrain", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 250, 250, 30));

            composer.AddSwitch(OnTransparentChanged, ElementBounds.Fixed(0, 290, 30, 30), "xray-transparent");
            composer.AddStaticText("Process transparent blocks", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 290, 300, 30));
        }
        else if (selectedCategory == MenuCategory.Movement)
        {
            composer.AddStaticText("MOVEMENT MODULES", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 45, 400, 25));
            composer.AddSwitch(OnFlightChanged, ElementBounds.Fixed(0, 75, 30, 30), "xray-flight");
            composer.AddStaticText("Client flight  [F7]", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 75, 300, 30));
            composer.AddStaticText("Single-player works locally. A multiplayer server can reject flight.", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 115, 490, 45));
        }
        else
        {
            composer.AddStaticText("INTERFACE MODULES", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 45, 400, 25));

            composer.AddSwitch(OnHudChanged, ElementBounds.Fixed(0, 75, 30, 30), "xray-hud");
            composer.AddStaticText("HUD status", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 75, 250, 30));
        }

        composer.AddButton("Reset defaults", OnReset, ElementBounds.Fixed(0, 355, 190, 40));
        composer.AddButton("Close", OnClose, ElementBounds.Fixed(210, 355, 190, 40));

        composer.EndChildElements();
        SingleComposer = composer.Compose();
        RefreshControls();
    }

    private bool OnCategoryClick(MenuCategory category)
    {
        if (selectedCategory == category) return true;
        selectedCategory = category;
        ClearComposers();
        Compose();
        return true;
    }

    private void RefreshControls()
    {
        try
        {
            var state = XRayRuntime.State;
            if (state == null || SingleComposer == null) return;

            var cfg = state.Config;
            SingleComposer.GetSwitch("xray-enabled")?.SetValue(cfg.Enabled);
            SingleComposer.GetSlider("xray-alpha")?.SetValues(cfg.WallAlpha, 0, 100, 1);
            SingleComposer.GetSlider("xray-range")?.SetValues(cfg.Range, 16, 256, 16);
            SingleComposer.GetSwitch("xray-terrain")?.SetValue(cfg.IncludeTerrain);
            SingleComposer.GetSwitch("xray-transparent")?.SetValue(cfg.IncludeTransparentBlocks);
            SingleComposer.GetSwitch("xray-hud")?.SetValue(cfg.ShowHud);
            SingleComposer.GetSwitch("xray-flight")?.SetValue(cfg.ClientFlightEnabled);
        }
        catch (Exception ex)
        {
            XRaySafety.Report(clientApi, ex, "Could not refresh X-Ray menu controls");
        }
    }

    private void OnEnabledChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.Enabled = value;
            SaveConfig();
            XRayMeshPatch.Invalidate(clientApi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(clientApi, ex, "Could not apply X-Ray enabled setting");
        }
    }

    private bool OnAlphaChanged(int value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.WallAlpha = (byte)Math.Clamp(value, 0, 100);
            SaveConfig();
            XRayMeshPatch.Invalidate(capi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(capi, ex, "Could not apply wall transparency setting");
        }
        return true;
    }

    private bool OnRangeChanged(int value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.Range = Math.Clamp(value, 16, 256);
            SaveConfig();
            XRayMeshPatch.Invalidate(capi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(capi, ex, "Could not apply X-Ray range setting");
        }
        return true;
    }

    private void OnTerrainChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.IncludeTerrain = value;
            SaveConfig();
            XRayMeshPatch.Invalidate(clientApi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(clientApi, ex, "Could not apply terrain setting");
        }
    }

    private void OnTransparentChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.IncludeTransparentBlocks = value;
            SaveConfig();
            XRayMeshPatch.Invalidate(clientApi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(clientApi, ex, "Could not apply transparent-block setting");
        }
    }

    private void OnHudChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.ShowHud = value;
            SaveConfig();
        }
        catch (Exception ex)
        {
            XRaySafety.Report(clientApi, ex, "Could not apply HUD setting");
        }
    }

    private void OnFlightChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.ClientFlightEnabled = value;
            XRayRuntime.Flight?.SetEnabled(value);
            SaveConfig();
        }
        catch (Exception ex)
        {
            XRaySafety.Report(clientApi, ex, "Could not change client flight setting");
        }
    }

    private bool OnReset()
    {
        try
        {
            if (XRayRuntime.State != null)
            {
                var defaults = XRayConfig.Default();
                XRayRuntime.State.Config.Enabled = defaults.Enabled;
                XRayRuntime.State.Config.WallAlpha = defaults.WallAlpha;
                XRayRuntime.State.Config.Range = defaults.Range;
                XRayRuntime.State.Config.IncludeTerrain = defaults.IncludeTerrain;
                XRayRuntime.State.Config.IncludeTransparentBlocks = defaults.IncludeTransparentBlocks;
                XRayRuntime.State.Config.ShowHud = defaults.ShowHud;
                XRayRuntime.State.Config.ClientFlightEnabled = defaults.ClientFlightEnabled;
            }

            XRayRuntime.Flight?.SetEnabled(false);
            SaveConfig();
            RefreshControls();
            XRayMeshPatch.Invalidate(clientApi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(clientApi, ex, "Could not reset X-Ray settings");
        }
        return true;
    }

    private void SaveConfig()
    {
        if (XRayRuntime.State == null) return;
        try
        {
            clientApi.StoreModConfig(XRayRuntime.State.Config, "vsxray.json");
        }
        catch (Exception ex)
        {
            XRaySafety.Report(clientApi, ex, "Could not save X-Ray configuration");
        }
    }

    private bool OnClose()
    {
        try
        {
            TryClose();
        }
        catch (Exception ex)
        {
            XRaySafety.Report(clientApi, ex, "Could not close X-Ray menu");
        }
        return true;
    }

    public override bool OnEscapePressed()
    {
        OnClose();
        return true;
    }
}
