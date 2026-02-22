# Multiply Rush Production Readiness

Last updated: 2026-02-22

## Product Promise
- Fully playable offline.
- No ads.
- No in-app purchases.
- No analytics or telemetry.
- No account/login requirement.

## Engineering Baseline
- Engine: Unity 6.3 LTS (`6000.3.9f1`).
- Primary target: iOS portrait.
- Runtime progression: endless procedural levels.
- Save system: local `PlayerPrefs` only.

## Release Gates

### Gate 1: Offline Compliance
- `tools/offline_policy_check.py` passes.
- `Multiply Rush -> Release -> Run Release Audit` has no `[FAIL]` entries.
- Device validation in Airplane Mode:
  - Launch app from cold start.
  - Play at least 5 levels.
  - Open pause/options/result screens.
  - Confirm no internet prompts, no ad surfaces, and no blocked gameplay.

### Gate 2: UI Coverage
- Main menu scales correctly in simulator presets:
  - iPhone SE (small)
  - iPhone 12/13 class
  - iPhone 14/15/16 Pro Max class
- Gameplay HUD readable at all tested aspect ratios.
- Pause and result panels have no clipping/overlap.

### Gate 3: Performance
- Target stable framerate on older supported iPhone hardware.
- No per-frame GC spikes from gameplay loop.
- No runaway particle/audio sources.

### Gate 4: Stability
- No compiler errors.
- No repeated runtime exceptions during a 15-minute play session.
- App pause/resume behavior is deterministic.

### Gate 5: Store Submission Prep
- Final bundle identifier set.
- Version and build number updated.
- App icon and launch images complete.
- App Store metadata/screenshots/privacy questionnaire filled.

## Operational Notes
- Local scene/prefab tuning is expected during iteration; avoid mixing those edits with tooling/config commits.
- Keep offline policy checks in CI green to prevent accidental SDK/package regressions.
