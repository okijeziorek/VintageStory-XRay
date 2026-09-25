using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageStoryXRay;

/// <summary>Applies flight flags to the local player's controls on the client only.</summary>
internal sealed class ClientFlightController : IDisposable
{
    private readonly ICoreClientAPI api;
    private EntityControls? controlledEntityControls;
    private bool previousIsFlying;
    private bool previousNoClip;

    public ClientFlightController(ICoreClientAPI api) => this.api = api;

    public void SetEnabled(bool enabled)
    {
        if (!enabled) RestoreControls();
    }

    public void OnGameTick(float deltaTime)
    {
        try
        {
            if (XRayRuntime.State?.Config.ClientFlightEnabled != true)
            {
                RestoreControls();
                return;
            }

            EntityControls? controls = api.World.Player?.Entity?.Controls;
            if (controls == null)
            {
                RestoreControls();
                return;
            }

            if (!ReferenceEquals(controls, controlledEntityControls))
            {
                RestoreControls();
                controlledEntityControls = controls;
                previousIsFlying = controls.IsFlying;
                previousNoClip = controls.NoClip;
            }

            controls.IsFlying = true;
            controls.NoClip = true;
        }
        catch (Exception ex)
        {
            if (XRayRuntime.State != null) XRayRuntime.State.Config.ClientFlightEnabled = false;
            XRaySafety.Report(api, ex, "Client flight could not update player controls");
            RestoreControls();
        }
    }

    private void RestoreControls()
    {
        if (controlledEntityControls == null) return;

        try
        {
            controlledEntityControls.IsFlying = previousIsFlying;
            controlledEntityControls.NoClip = previousNoClip;
        }
        catch (Exception ex)
        {
            XRaySafety.Report(api, ex, "Client flight could not restore player controls");
        }
        finally
        {
            controlledEntityControls = null;
        }
    }

    public void Dispose() => RestoreControls();
}
