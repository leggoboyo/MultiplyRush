# Multiply Rush Production Readiness

Last updated: 2026-02-23

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
- If submitting on or after 2026-04-28, archive must use Xcode 26 / iOS 26 SDK or newer.

### Gate 6: Documentation + Traceability
- `CHANGELOG.md` updated for this release.
- `docs/APP_STORE_METADATA_TEMPLATES.md` used for final copy.
- `docs/APP_STORE_COMPLIANCE_MATRIX.md` reviewed against current build behavior.
- A filled release packet exists for this submission cycle (from `docs/RELEASE_PACKET_TEMPLATE.md`).

## Operational Notes
- Local scene/prefab tuning is expected during iteration; avoid mixing those edits with tooling/config commits.
- Keep offline policy checks in CI green to prevent accidental SDK/package regressions.

## Apple References
- App Store Review Guidelines:
  - https://developer.apple.com/app-store/review/guidelines/
- App submission flow:
  - https://developer.apple.com/help/app-store-connect/manage-submissions-to-app-review/submit-an-app-for-review
- Screenshot specifications:
  - https://developer.apple.com/help/app-store-connect/reference/app-store-screenshot-specifications
