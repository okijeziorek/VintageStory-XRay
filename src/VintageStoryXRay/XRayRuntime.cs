namespace VintageStoryXRay;

internal static class XRayRuntime
{
    public static XRayState? State { get; set; }
    public static ClientFlightController? Flight { get; set; }

    public static XRayConfig Config => State?.Config ?? XRayConfig.Default();
}
