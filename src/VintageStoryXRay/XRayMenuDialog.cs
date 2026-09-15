using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageStoryXRay;

/// <summary>
/// In-game client-side configuration window for X-Ray.
/// API signatures are intentionally kept in one class so they can be adjusted
/// against the exact Vintage Story 1.22.3 assemblies when they are supplied.
/// </summary>
public sealed class XRayMenuDialog : GuiDialog
{
    private readonly ICoreClientAPI capi;

    public XRayMenuDialog(ICoreClientAPI capi) : base(capi)
    {
        this.capi = capi;
        Compose();
    }

    public override string ToggleKeyCombinationCode => null!;

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
            var cfg = state.Config;

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

    private bool OnEnabledChanged(bool value)
    {
        if (XRayRuntime.State != null) XRayRuntime.State.Config.Enabled = value;
        XRayMeshPatch.Invalidate(capi);
        return true;
    }

    private bool OnAlphaChanged(int value)
    {
        if (XRayRuntime.State != null) XRayRuntime.State.Config.WallAlpha = (byte)Math.Clamp(value, 0, 100);
        XRayMeshPatch.Invalidate(capi);
        return true;
    }

    private bool OnRangeChanged(int value)
    {
        if (XRayRuntime.State != null) XRayRuntime.State.Config.Range = Math.Clamp(value, 16, 256);
        XRayMeshPatch.Invalidate(capi);
        return true;
    }

    private bool OnTerrainChanged(bool value)
    {
        if (XRayRuntime.State != null) XRayRuntime.State.Config.IncludeTerrain = value;
        XRayMeshPatch.Invalidate(capi);
        return true;
    }

    private bool OnTransparentChanged(bool value)
    {
        if (XRayRuntime.State != null) XRayRuntime.State.Config.IncludeTransparentBlocks = value;
        XRayMeshPatch.Invalidate(capi);
        return true;
    }

    private bool OnHudChanged(bool value)
    {
        if (XRayRuntime.State != null) XRayRuntime.State.Config.ShowHud = value;
        return true;
    }

    private bool OnReset()
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

        RefreshControls();
        XRayMeshPatch.Invalidate(capi);
        return true;
    }

    private void OnClose()
    {
        TryClose();
    }

    public override bool OnEscapePressed() => TryClose();
}
