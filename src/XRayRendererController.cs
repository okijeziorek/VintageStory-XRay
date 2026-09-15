using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace VintageStoryXRay;

/// <summary>
/// Runtime bridge for the client renderer. The game renderer is not part of the stable public API,
/// therefore the bridge discovers candidate renderer methods at runtime instead of hard-coding a
/// private engine type. The actual mesh/shader interception is intentionally kept in this layer.
/// </summary>
internal static class XRayRendererController
{
    private static readonly object Sync = new();
    private static Harmony? harmony;
    private static bool enabled;

    public static bool Enabled => enabled;

    public static void SetEnabled(bool value)
    {
        lock (Sync)
        {
            enabled = value;
            if (value) Install();
            else Uninstall();
        }
    }

    private static void Install()
    {
        if (harmony != null) return;

        harmony = new Harmony("okijeziorek.vsxray");
        foreach (MethodBase method in FindCandidateRenderMethods())
        {
            // Candidate discovery is separated from the patch implementation so the exact
            // 1.22.3 renderer can be verified without baking an unstable private type name in the API.
            // The prefix currently performs no mutation; it is the safe integration point for the
            // renderer-specific mesh filtering pass.
            try
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(XRayRendererController), nameof(RenderPrefix)));
            }
            catch
            {
                // A candidate may be overloaded, generic or otherwise unsuitable. Ignore it and
                // continue looking for the concrete chunk-render path.
            }
        }
    }

    private static void Uninstall()
    {
        harmony?.UnpatchAll("okijeziorek.vsxray");
        harmony = null;
    }

    private static bool RenderPrefix() => true;

    private static IEnumerable<MethodBase> FindCandidateRenderMethods()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.GetName().Name?.Contains("Vintagestory", StringComparison.OrdinalIgnoreCase) ?? true)
                continue;

            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).Cast<Type>().ToArray(); }

            foreach (var type in types)
            {
                string name = type.FullName ?? type.Name;
                if (!name.Contains("ChunkRenderer", StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.Name.Contains("Render", StringComparison.OrdinalIgnoreCase) &&
                        !method.IsAbstract && !method.ContainsGenericParameters)
                        yield return method;
                }
            }
        }
    }
}
