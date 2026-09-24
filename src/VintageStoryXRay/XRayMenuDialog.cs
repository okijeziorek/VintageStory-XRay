using Vintagestory.API.Client;

namespace VintageStoryXRay;

/// <summary>Client-side X-Ray configuration window.</summary>
public sealed class XRayMenuDialog : GuiDialog
{
    private readonly ICoreClientAPI capi;

    public XRayMenuDialog(ICoreClientAPI capi) : base(capi)
    {
        this.capi = capi;
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

        var composer = capi.Gui.CreateCompo("vintagestoryxray.menu", bounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar("Vintage Story X-Ray", OnClose)
            .BeginChildElements(bgBounds.FlatCopy().FixedGrow(0, -10));

        var state = XRayRuntime.State;
        if (state == null)
        {
            composer.AddStaticText("X-Ray runtime is not initialized.", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 0, 400, 30));
        }
        else
        {
            composer.AddStaticText("General", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 0, 400, 25));
            composer.AddSwitch(OnEnabledChanged, ElementBounds.Fixed(0, 30, 30, 30), "xray-enabled");
            composer.AddStaticText("X-Ray enabled", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 30, 250, 30));

            composer.AddStaticText("Wall transparency", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 70, 180, 25));
            composer.AddSlider(OnAlphaChanged, ElementBounds.Fixed(0, 100, 400, 30), "xray-alpha");

            composer.AddStaticText("Range", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 140, 180, 25));
            composer.AddSlider(OnRangeChanged, ElementBounds.Fixed(0, 170, 400, 30), "xray-range");

            composer.AddSwitch(OnTerrainChanged, ElementBounds.Fixed(0, 210, 30, 30), "xray-terrain");
            composer.AddStaticText("Process terrain", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 210, 250, 30));

            composer.AddSwitch(OnTransparentChanged, ElementBounds.Fixed(0, 250, 30, 30), "xray-transparent");
            composer.AddStaticText("Process transparent blocks", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 250, 300, 30));

            composer.AddSwitch(OnHudChanged, ElementBounds.Fixed(0, 290, 30, 30), "xray-hud");
            composer.AddStaticText("Show HUD status", CairoFont.WhiteSmallText(), ElementBounds.Fixed(45, 290, 250, 30));

            composer.AddButton("Reset defaults", OnReset, ElementBounds.Fixed(0, 335, 190, 40));
            composer.AddButton("Close", OnClose, ElementBounds.Fixed(210, 335, 190, 40));
        }

        composer.EndChildElements();
        SingleComposer = composer.Compose();
        RefreshControls();
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
        }
        catch (Exception ex)
        {
            XRaySafety.Report(capi, ex, "Could not refresh X-Ray menu controls");
        }
    }

    private bool OnEnabledChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.Enabled = value;
            SaveConfig();
            XRayMeshPatch.Invalidate(capi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(capi, ex, "Could not apply X-Ray enabled setting");
        }
        return true;
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

    private bool OnTerrainChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.IncludeTerrain = value;
            SaveConfig();
            XRayMeshPatch.Invalidate(capi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(capi, ex, "Could not apply terrain setting");
        }
        return true;
    }

    private bool OnTransparentChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.IncludeTransparentBlocks = value;
            SaveConfig();
            XRayMeshPatch.Invalidate(capi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(capi, ex, "Could not apply transparent-block setting");
        }
        return true;
    }

    private bool OnHudChanged(bool value)
    {
        try
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.ShowHud = value;
            SaveConfig();
        }
        catch (Exception ex)
        {
            XRaySafety.Report(capi, ex, "Could not apply HUD setting");
        }
        return true;
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
            }

            SaveConfig();
            RefreshControls();
            XRayMeshPatch.Invalidate(capi);
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(capi, ex, "Could not reset X-Ray settings");
        }
        return true;
    }

    private void SaveConfig()
    {
        if (XRayRuntime.State == null) return;
        try
        {
            capi.StoreModConfig(XRayRuntime.State.Config, "vsxray.json");
        }
        catch (Exception ex)
        {
            XRaySafety.Report(capi, ex, "Could not save X-Ray configuration");
        }
    }

    private void OnClose()
    {
        try
        {
            TryClose();
        }
        catch (Exception ex)
        {
            XRaySafety.Report(capi, ex, "Could not close X-Ray menu");
        }
    }

    public override bool OnEscapePressed()
    {
        OnClose();
        return true;
    }
}
