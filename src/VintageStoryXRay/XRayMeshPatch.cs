using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageStoryXRay;

/// <summary>
/// Intercepts the concrete terrain mesh-pool AddMeshData methods rather than
/// MeshData.AddMeshData. The latter modifies the destination buffer before the
/// incoming block mesh is copied, so it cannot reliably affect the newly added
/// terrain geometry.
/// </summary>
internal static class XRayMeshPatch
{
    private const int RenderPassMask = 0x03ff;

    public static void Apply(Harmony harmony)
    {
        try
        {
            int patched = 0;
            var seen = new HashSet<MethodInfo>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    try
                    {
                        if (!typeof(ITerrainMeshPool).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                        {
                            continue;
                        }

                        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (method.Name != "AddMeshData" || !seen.Add(method)) continue;

                            var parameters = method.GetParameters();
                            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(MeshData)) continue;

                            try
                            {
                                harmony.Patch(
                                    method,
                                    prefix: new HarmonyMethod(typeof(XRayMeshPatch), nameof(Prefix)),
                                    postfix: new HarmonyMethod(typeof(XRayMeshPatch), nameof(Postfix))
                                );
                                patched++;
                            }
                            catch (Exception ex)
                            {
                                XRaySafety.Report(null, ex, $"Could not patch terrain mesh pool method {type.FullName}.{method.Name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        XRaySafety.Report(null, ex, $"Could not inspect terrain mesh pool type {type.FullName}");
                    }
                }
            }

            if (patched == 0)
            {
                throw new InvalidOperationException("No concrete ITerrainMeshPool.AddMeshData method could be patched.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not install terrain mesh pool patches", ex);
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null).Select(type => type!);
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static void Prefix(object[] __args, ref PatchState? __state)
    {
        __state = null;

        try
        {
            if (XRayRuntime.State?.Enabled != true) return;
            if (!XRayRuntime.Config.ProcessTerrain) return;
            if (!LooksLikeTerrainTessellation()) return;

            MeshData? mesh = null;
            foreach (var arg in __args)
            {
                if (arg is MeshData candidate)
                {
                    mesh = candidate;
                    break;
                }
            }

            if (mesh == null || mesh.Rgba == null || mesh.RenderPassesAndExtraBits == null)
            {
                return;
            }

            __state = new PatchState(
                mesh,
                (byte[])mesh.Rgba.Clone(),
                (short[])mesh.RenderPassesAndExtraBits.Clone()
            );

            byte alpha = XRayRuntime.Config.WallAlpha;
            int rgbaLength = Math.Min(mesh.Rgba.Length, Math.Max(0, mesh.VerticesCount * 4));
            for (int i = 3; i < rgbaLength; i += 4)
            {
                mesh.Rgba[i] = alpha;
            }

            int passLength = mesh.RenderPassesAndExtraBits.Length;
            int transparent = (int)EnumChunkRenderPass.Transparent;
            for (int i = 0; i < passLength; i++)
            {
                mesh.RenderPassesAndExtraBits[i] = (short)(
                    (mesh.RenderPassesAndExtraBits[i] & ~RenderPassMask) | transparent
                );
            }
        }
        catch (Exception ex)
        {
            __state = null;
            XRaySafety.DisableAfterFailure(null, ex, "X-Ray terrain mesh prefix failed; disabling X-Ray");
        }
    }

    private static void Postfix(PatchState? __state)
    {
        if (__state == null) return;

        try
        {
            __state.Mesh.Rgba = __state.Rgba;
            __state.Mesh.RenderPassesAndExtraBits = __state.RenderPassesAndExtraBits;
        }
        catch (Exception ex)
        {
            XRaySafety.DisableAfterFailure(null, ex, "X-Ray terrain mesh restoration failed; disabling X-Ray");
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

    private sealed record PatchState(
        MeshData Mesh,
        byte[] Rgba,
        short[] RenderPassesAndExtraBits
    );
}
