# VintageStory-XRay

Client-side X-Ray mod for Vintage Story 1.22.3.

> Status: development scaffold. The renderer integration is intentionally isolated until it is verified against the exact 1.22.3 client assemblies.

## Planned features

- client-side toggle
- configurable transparent/hidden blocks
- ore whitelist
- configurable render distance
- ore highlighting / ESP
- optional cave/air-space highlighting
- JSON configuration
- in-game status indicator

## Compatibility

Target: Vintage Story 1.22.3

Target runtime: .NET 10

Server installation is not intended.

## Development

The project is structured so that the X-Ray policy/configuration is separated from the renderer hook. This makes it possible to update the rendering implementation when the exact 1.22.3 API/assemblies are available.

## Installation

Development build only. Copy the resulting client-side mod archive to the Vintage Story `Mods` directory.

## Configuration

Configuration will be stored as JSON and will expose the X-Ray toggle, block filters, highlight settings and distance limits.

## Disclaimer

Use only where permitted by the server/operator. This project is client-side rendering research for Vintage Story.
