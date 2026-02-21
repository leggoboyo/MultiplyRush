# Multiply Rush QA Playtest Checklist

Use this checklist before every App Store submission build.

## 1) Core Gameplay
- Launch app -> menu renders and buttons are clickable.
- Select each difficulty (`Easy`, `Normal`, `Hard`) and verify persistence after relaunch.
- Start run, drag left/right, and confirm free X movement (not lane snap).
- Verify only one gate can be consumed per row.
- Verify gate labels are readable and not mirrored.
- Verify count changes match gate operation (`+`, `-`, `x`, `/`).
- Reach finish, observe battle phase, then result overlay.

## 2) Progression
- Win a level -> next level starts immediately.
- Lose a level -> retry restarts same level.
- Confirm enemy count and speed increase over time.
- Confirm mini-boss occurs every 10th level.
- Confirm challenge modifiers apply at intended milestones.
- Confirm reward kits/shields are granted on mini-boss wins.

## 3) UI / UX
- Main menu animation remains responsive at 60 FPS target.
- Pause button opens pause menu reliably.
- Pause menu controls work:
  - Resume
  - Restart Level
  - Main Menu
  - Master Volume slider
  - Camera Motion slider
  - Graphics fidelity buttons
  - Haptics toggle
- Win and lose overlays fit safely on iPhone notch and non-notch devices.

## 4) Audio / Haptics
- Main menu, gameplay, and pause music transitions are smooth.
- Button taps and gate SFX trigger appropriately.
- Win/Lose SFX trigger once per result.
- Haptics fire on gates/pause/result when enabled.
- Haptics stop firing when disabled in pause options.

## 5) Device Safety
- Lock/unfocus app during a run -> game pauses when returning.
- App does not crash on repeated pause/resume.
- Low graphics mode is playable on older devices.
- High graphics mode remains stable on newer devices.

## 6) Offline Compliance
- Airplane mode: app launches and plays fully.
- No network prompts, no ads, no analytics dialogs.

## 7) Release Candidate Gate
- Run `Multiply Rush -> Release -> Prepare iOS Release Candidate`.
- Open `Assets/MultiplyRush/Docs/ReleaseAudit.md`.
- All `[FAIL]` checks resolved.
- Remaining `[WARN]` checks intentionally accepted or fixed.
- Increment app version + build number before final archive.
