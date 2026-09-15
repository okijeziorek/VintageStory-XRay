# VintageStory-XRay

Client-side X-Ray rendering project for **Vintage Story 1.22.3**.

## Current state

The repository contains a .NET 10 client-only mod, persistent configuration, an F8 toggle and an experimental Harmony patch around `MeshData.AddMeshData(...)`.

The current prototype changes terrain mesh alpha and routes the intercepted terrain mesh data toward the transparent chunk render pass. This is the first real rendering implementation, but it is **not yet verified against a locally executed 1.22.3 client**.

The patch is deliberately restricted to call stacks containing `TerrainChunkTesselator` or `ChunkTesselator` so inventory/item meshes are not blindly made transparent.

## What works in the prototype

- client-only mod loading
- F8 X-Ray toggle
- persistent `ModConfig/vsxray.json`
- configurable wall alpha
- configurable redraw range
- chunk redraw on toggle
- experimental transparent terrain rendering path

## Still to implement

- true target/ore whitelist at block-tessellation level
- opaque ore ESP / outlines
- hidden terrain mode
- cave/air-space detection
- mob/entity visibility modes
- performance-optimized chunk invalidation
- automated 1.22.3 integration test

## Compatibility

- Vintage Story: **1.22.3**
- .NET: **10**
- side: **Client**
- server installation: **not required**

Vintage Story 1.22 uses .NET 10. The public API exposes client rendering through `ICoreClientAPI.Render` and renderer registration through `IClientEventAPI.RegisterRenderer`; this project additionally uses Harmony because the desired X-Ray effect needs to affect terrain mesh data before it reaches the engine's normal chunk render pools.

## Build

Set `VINTAGE_STORY` to the directory containing the game's DLLs, then run:

```bash
dotnet build -c Release
```

The project expects:

- `VintagestoryAPI.dll`
- `VintagestoryLib.dll`
- `Lib/0Harmony.dll`

The game DLLs are intentionally **not** stored in this repository.

## Configuration

The mod uses the normal Vintage Story mod configuration API and creates:

`ModConfig/vsxray.json`

Current fields include:

- `Enabled`
- `WallAlpha`
- `Range`
- `IncludeTerrain`
- `IncludeTransparentBlocks`
- `ShowHud`

## Architecture

```text
XRayModSystem
    |
    +-- XRayConfig / XRayState
    |
    +-- F8 input handler
    |
    +-- Harmony
           |
           +-- MeshData.AddMeshData
                    |
                    +-- terrain call-stack filter
                    +-- alpha rewrite
                    +-- Transparent render-pass rewrite
```

The original `Rgba` and `RenderPassesAndExtraBits` buffers are cloned before modification and restored after the patched call returns. This prevents the temporary X-Ray transformation from permanently corrupting the source `MeshData` object.

## Why the ore whitelist is a separate stage

A terrain chunk mesh contains already-combined geometry. At `MeshData.AddMeshData(...)` level, individual faces are not reliably represented as high-level `Block` objects. Therefore a robust ore-only mode needs to intercept the terrain tessellation stage earlier, where the block position and block ID are still known.

That is the next renderer stage rather than something that should be faked with a texture-name test.

## References

- [Vintage Story API](https://github.com/anegostudios/vsapi)
- [Vintage Story mod examples](https://github.com/anegostudios/vsmodexamples)

## Disclaimer

Use only where permitted by the server/operator. This project changes client rendering and does not modify server world state.
