# MultiplyRush
An actually-playable version of the clickbait gate-multiplier runner ads. Free, offline, no ads.

## Privacy Policy + Support Website (GitHub Pages)
- Static site source: `docs/`
- Home: `docs/index.html`
- Privacy Policy: `docs/privacy.html`
- Support: `docs/support.html`

After enabling GitHub Pages for this repository, the site will be available at:
- `https://leggoboyo.github.io/MultiplyRush/`

### Enable GitHub Pages
1. Open repository **Settings** -> **Pages**.
2. Set **Source** to **Deploy from a branch**.
3. Set **Branch** to `main` and **Folder** to `/docs`.
4. Save and wait for the first publish to complete.

## Unity Project
- Project root: `My project/`
- Recommended Unity version: `6000.3.9f1`

## Run In Editor
1. Open `My project/` in Unity Hub.
2. Let scripts compile and asset import finish.
3. If this is a fresh clone, run `Multiply Rush -> Step 1 -> Bootstrap Project`.
4. Open `Assets/MultiplyRush/Scenes/MainMenu.unity`.
5. Press Play and verify:
   - Menu animates and difficulty selection works.
   - Tap `PLAY NOW` to transition into gameplay.
   - Drag left/right, hit gates, and confirm count updates.
   - Reach finish and observe the short battle phase before WIN/LOSE.
   - Pause button works and options persist (audio, camera motion, graphics, haptics).

## Production Features Implemented
- Offline-only architecture with no ads/analytics/IAP/network calls.
- Runtime procedural audio (menu/game/pause music + SFX).
- Device-tier runtime tuning (target FPS + render scale).
- Quality selector (Auto/Low/Medium/High) and camera-motion slider.
- Haptics toggle with persisted preference.
- App lifecycle safety:
  - Auto pause on app background/focus loss.
  - Low-memory cleanup hook.
- Result overlays, battle phase, miniboss cadence, and endless progression scaling.

## iOS Release Workflow (Unity)
1. Open Unity and run:
   - `Multiply Rush -> Release -> Prepare iOS Release Candidate`
2. Open generated audit report:
   - `Assets/MultiplyRush/Docs/ReleaseAudit.md`
3. Resolve all `[FAIL]` items before building.
4. Resolve `[WARN]` items that apply to your final submission (bundle ID, icons, metadata).
5. Build iOS and archive in Xcode for App Store Connect upload.

## CI + Hygiene
- Offline policy is enforced in CI via `.github/workflows/offline-policy.yml`.
- Local check command:
  - `python tools/offline_policy_check.py`
  - `python tools/repo_size_guard.py`
- Extended readiness guide:
  - `Assets/MultiplyRush/Docs/ProductionReadiness.md`

## Design Constraints
- Offline only.
- No ads.
- No analytics/telemetry.
- No IAP.
- No network requirement.

## Offline Verification Checklist
1. Enable Airplane Mode on your iPhone.
2. Launch the app and play multiple levels (menu -> gameplay -> pause -> win/lose).
3. Confirm no popups, no network prompts, no ad surfaces, and no gameplay interruptions.
4. In Unity, run `Multiply Rush -> Release -> Run Release Audit` and verify no `[FAIL]` in offline checks.
