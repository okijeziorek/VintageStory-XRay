# Roadmap

## Phase 1 — transparent terrain

- [x] client-only loader
- [x] F8 toggle
- [x] persistent config
- [x] Harmony bridge
- [x] experimental terrain mesh alpha/pass rewrite

## Phase 2 — real X-Ray targets

- [ ] identify exact 1.22.3 `TerrainChunkTesselator` method
- [ ] intercept block ID + block position before mesh combination
- [ ] separate target blocks from ordinary terrain
- [ ] keep ores/whitelisted blocks opaque
- [ ] make non-target terrain transparent or discard it

## Phase 3 — ESP

- [ ] ore bounding boxes / outlines
- [ ] distance labels
- [ ] configurable colors
- [ ] occlusion-independent target rendering

## Phase 4 — advanced modes

- [ ] cave/air-space detection
- [ ] mob visibility
- [ ] full-bright target mode
- [ ] distance-based optimization
- [ ] chunk cache

## Verification gate

No release should be labelled `stable` until the mod has been run against the exact 1.22.3 client assemblies and the runtime test in `docs/TESTING-1.22.3.md` passes.
