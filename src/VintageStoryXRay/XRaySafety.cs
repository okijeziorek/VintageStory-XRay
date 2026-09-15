using Vintagestory.API.Common;

namespace VintageStoryXRay;

/// <summary>Best-effort exception handling so optional X-Ray functionality cannot take down the client.</summary>
internal static class XRaySafety
{
    public static void Report(ICoreAPI? api, Exception exception, string context)
    {
        try
        {
            api?.Logger.Error("[VintageStory X-Ray] {0}: {1}", context, exception);
        }
        catch
        {
            // Logging must never become a second failure.
        }
    }

    public static void DisableAfterFailure(ICoreAPI? api, Exception exception, string context)
    {
        Report(api, exception, context);

        try
        {
            if (XRayRuntime.State != null)
            {
                XRayRuntime.State.Config.Enabled = false;
            }
        }
        catch
        {
            // Last-resort safety guard.
        }
    }
}
