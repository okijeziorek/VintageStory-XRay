using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VintageStoryXRay;

internal static class XRayMeshPatch
{
    private const int RenderPassMask = 0x03ff;

    public static void Apply(Harmony harmony)
    {
        try
        {
            var methods = typeof(MeshData)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name == "AddMeshData")
                .Where(m => m.GetParameters().Length > 0)
                .Where(m => m.GetParameters()[0].ParameterType == typeof(MeshData))
                .ToArray();

            foreach (var method in methods)
            {
                try
                {
                    harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(XRayMeshPatch), nameof(Prefix)),
                        postfix: new HarmonyMethod(typeof(XRayMeshPatch), nameof(Postfix))
                    );
                }
                catch (Exception ex)
                {
                    // One incompatible overload must not prevent the rest of the mod from loading.
                    XRaySafety.Report(null, ex, $"Could not patch MeshData.{method.Name} overload");
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not enumerate MeshData.AddMeshData methods", ex);
        }
    }

    private static void Prefix(MeshData __instance, ref PatchState? __state)
    {
        __state = null;

        try
        {
            if (XRayRuntime.State?.Enabled != true) return;
            if (!LooksLikeTerrainTessellation()) return;
            if (__instance.Rgba == null || __instance.RenderPassesAndExtraBits == null) return;

            __state = new PatchState(
                (byte[])__instance.Rgba.Clone(),
                (short[])__instance.RenderPassesAndExtraBits.Clone()
            );

            byte alpha = XRayRuntime.Config.WallAlpha;
            int rgbaLength = Math.Min(__instance.Rgba.Length, Math.Max(0, __instance.VerticesCount * 4));
            for (int i = 3; i < rgbaLength; i += 4)
            {
                __instance.Rgba[i] = alpha;
            }

            int passLength = Math.Min(__instance.RenderPassesAndExtraBits.Length, Math.Max(0, __instance.VerticesCount / 4 + 1));
            int transparent = (int)EnumChunkRenderPass.Transparent;
            for (int i = 0; i < passLength; i++)
            {
                __instance.RenderPassesAndExtraBits[i] = (short)(
                    (__instance.RenderPassesAndExtraBits[i] & ~RenderPassMask) | transparent
                );
            }
        }
        catch (Exception ex)
        {
            // Rendering hooks run on the render path: never allow an exception to escape into the game loop.
            __state = null;
            XRaySafety.DisableAfterFailure(null, ex, "X-Ray mesh prefix failed; disabling X-Ray");
        }
    }

    private static void Postfix(MeshData __instance, PatchState? __state)
    {
        if (__state == null) return;

        try
        {
            __instance.Rgba = __state.Rgba;
            __instance.RenderPassesAndExtraBits = __state.RenderPassesAndExtraBits;
        }
        catch (Exception ex)
        {
            // Do not let restoration errors crash the render loop.
            XRaySafety.DisableAfterFailure(null, ex, "X-Ray mesh restoration failed; disabling X-Ray");
        }
    }

    private static bool LooksLikeTerrainTessellation()
    {
        try
        {
            foreach (var frame in new StackTrace(false).GetFrames() ?? Array.Empty<StackFrame>())
            {
                string? declaring = frame.GetMethod()?.DeclaringType?.FullName;
                if (declaring == null) continue;

                if (declaring.Contains("TerrainChunkTesselator", StringComparison.Ordinal) ||
                    declaring.Contains("ChunkTesselator", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            XRaySafety.Report(null, ex, "Could not inspect terrain tessellation call stack");
        }

        return false;
    }

    public static void Invalidate(ICoreClientAPI api)
    {
        try
        {
            var player = api.World.Player;
            if (player == null) return;

            var pos = player.Entity.Pos.AsBlockPos;
            int radius = Math.Clamp(XRayRuntime.Config.Range, 32, 256);
            const int step = 32;
            int mapY = api.World.BlockAccessor.MapSizeY;

            for (int x = pos.X - radius; x <= pos.X + radius; x += step)
            {
                for (int z = pos.Z - radius; z <= pos.Z + radius; z += step)
                {
                    for (int y = 0; y < mapY; y += step)
                    {
                        api.World.BlockAccessor.MarkBlockDirty(new BlockPos(x, y, z));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            XRaySafety.Report(api, ex, "Could not invalidate terrain after X-Ray setting change");
        }
    }

    private sealed record PatchState(byte[] Rgba, short[] RenderPassesAndExtraBits);
}
