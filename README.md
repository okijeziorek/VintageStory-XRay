# VintageStory-XRay

Client-side X-Ray rendering project for **Vintage Story 1.22.3**.

## Current state

The repository now contains the .NET 10 project, client-only mod metadata, configuration system, hotkey handling and an isolated Harmony renderer bridge.

**Important:** the renderer bridge is not yet the final X-Ray implementation. It deliberately does not pretend that a private 1.22.3 engine method has been verified. The remaining work is to bind the bridge to the exact 1.22.3 chunk-mesh/shader path and implement target-block preservation.

## Planned X-Ray modes

- transparent terrain
- hidden terrain
- ore/target whitelist
- target highlighting
- cave/air-space view
- configurable render distance
- hotkey toggle
- JSON configuration

## Compatibility

- Vintage Story: 1.22.3
- .NET: 10
- side: client
- server installation: not required

Vintage Story's 1.22 development line migrated the source projects from .NET 8 to .NET 10. The public API exposes client rendering through `ICoreClientAPI.Render` and renderer registration through `IClientEventAPI.RegisterRenderer`; the deeper chunk renderer remains an engine implementation detail. See the official API source and API update notes.

## Build

Set `VINTAGE_STORY` to the directory containing the game's DLLs, then run:

```bash
dotnet build -c Release
```

The project expects:

- `VintagestoryAPI.dll`
- `VintagestoryLib.dll`
- `Lib/0Harmony.dll`

## Configuration

The mod creates `ModConfig/vsxray.json` on first client load. It contains opacity, maximum distance, highlighting and target block filters.

## Architecture

`XRayModSystem` is client-only and owns the toggle. `XRayConfig` owns persistent settings. `XRayRendererController` isolates the private renderer interception so it can be updated independently when the exact 1.22.3 render path is verified.

## References

- Vintage Story API: https://github.com/anegostudios/vsapi
- Vintage Story mod examples: https://github.com/anegostudios/vsmodexamples

## Disclaimer

Use only where permitted by the server/operator. This project changes client rendering and does not modify server world state.
