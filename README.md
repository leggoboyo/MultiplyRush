# MultiplyRush
An actually-playable version of the clickbait gate-multiplier runner ads. Free, offline, no ads.

## Unity Project
Project root: `My project/`

## Step 1 Run (Editor)
1. Open `My project/` in Unity Hub (Unity `6000.3.9f1`).
2. Wait for script compilation.
3. In the Unity top menu, run: `Multiply Rush -> Step 1 -> Bootstrap Project`.
4. Open scene: `Assets/MultiplyRush/Scenes/MainMenu.unity`.
5. Press Play, click `Play`, and test:
   - drag left/right while moving forward,
   - pass through gates to change crowd count,
   - reach finish to WIN/LOSE,
   - use `Next Level` or `Retry`.

## Release-Readiness Foundation (current)
- Safe-area aware UI (notch support)
- First-run drag hint onboarding
- Input System-native controls and UI input module
- Pool prewarm for crowd/enemy/gates to reduce spawn hitches
- Stronger run/result state guards
- Mobile runtime defaults: frame cap + no sleep timeout

## Design Constraints
- Offline only
- No ads
- No analytics/telemetry
- No IAP
- No network requirement
