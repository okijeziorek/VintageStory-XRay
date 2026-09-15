# Bugfix review

## Fixed in this branch

- Removed the duplicate root `.csproj` which caused the SDK project to include both the legacy `src/*.cs` files and the current `src/VintageStoryXRay/*.cs` files.
- Removed the duplicate legacy `XRayConfig`, `XRayModSystem`, and inert `XRayRendererController` implementations.
- Removed duplicate `mod/modinfo.json`; the package now has a single root `modinfo.json`.
- Integrated the F11 dialog through `ICoreClientAPI.Gui.RegisterDialog()` and the input hotkey system.
- Kept F8 as the quick X-Ray toggle.
- Changed config persistence to the official `StoreModConfig<T>` API.
- Release packaging now places the built DLL and `modinfo.json` together in the generated archive.
- Added an explicit build-time check for `VintagestoryLib.dll` in addition to the API and Harmony assemblies.

## Verification status

The public Vintage Story API documents confirm the `GuiDialog`/`GuiAPI` integration and `StoreModConfig<T>` APIs. Exact runtime compilation and game testing against the user's Vintage Story 1.22.3 installation is still pending until the matching local DLLs are supplied.
