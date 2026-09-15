# Architecture

## Goal

The mod is designed as a client-side rendering modification. It does not alter world state or require server-side code.

## Components

- `XRayConfig`: user configuration and block filtering.
- `XRayState`: runtime toggle and current mode.
- `XRayRenderer`: isolated renderer integration point.
- `XRayHud`: optional status indicator.

## Rendering strategy

The renderer integration must be compiled and tested against the exact Vintage Story 1.22.3 client assemblies. The project deliberately avoids claiming API compatibility for undocumented/internal renderer hooks until those assemblies are available for verification.

## Modes

1. Normal — vanilla rendering.
2. Transparent — non-target blocks are rendered with reduced visibility.
3. Hidden — non-target blocks are omitted from the X-Ray view.
4. Highlight — configured target blocks receive an outline/overlay.

## Block filtering

The configuration should support an allow-list for targets and an optional deny-list for blocks that must remain visible. Filtering is performed client-side on the render representation; it does not modify blocks on the server.

## Performance

The implementation should avoid rebuilding all chunk meshes every frame. State changes should invalidate only the affected render data, with debouncing where appropriate.

## Verification checklist

- Build against .NET 10.
- Reference the exact 1.22.3 client API/assemblies.
- Verify client-only loading on a vanilla server.
- Verify chunk rebuild cost while toggling modes.
- Verify that server world data is not modified.
