# App Store Metadata Templates (Multiply Rush)

Use this as the source of truth for App Store Connect copy.

Last updated: 2026-02-23

## 1) Field Limits and Constraints (Apple)

| Field | Limit / Requirement |
|---|---|
| App Name | 2-30 characters |
| Subtitle | Up to 30 characters |
| Promotional Text | Up to 170 characters |
| Description | Required, plain text, up to 4000 characters |
| Keywords | Required, up to 100 bytes |
| Screenshots | Required, 1-10 images (`.jpeg`, `.jpg`, `.png`) |
| Support URL | Required; must lead to support contact info |
| Privacy Policy URL | Required for iOS apps |
| What’s New | Required for updates, up to 4000 characters |
| Age Rating | Required questionnaire |

## 2) Core Identity Drafts

### App Name
`Multiply Rush`

### Subtitle Options
- `Offline Crowd Runner`
- `No Ads. Endless Levels.`

### Promotional Text
`Swipe through smart gates, build your crowd, and overwhelm enemy lines in a pure offline runner with no ads, no login, and no internet requirement.`

### Keywords (comma-separated)
`runner,crowd,offline,hypercasual,arcade,math,gates,strategy,endless,free`

## 3) Description Template

```text
Multiply Rush is a fast, satisfying crowd-runner built around the gameplay people see in viral ads, but fully real and fully playable.

FEATURES
- Fully offline gameplay (airplane mode friendly)
- No ads, no tracking, no paywalls
- Endless procedural levels with scaling challenge
- Swipe movement, gate strategy, and finish-line battles
- Mini-boss levels and replayable progression
- Music track selection, quality settings, and haptics controls

HOW IT PLAYS
1. Swipe left/right to steer your crowd.
2. Pick smart gates to grow your unit count.
3. Reach the finish and defeat enemy formations.
4. Win, improve, and push deeper into progression.

Built for smooth play across modern iPhones.
```

## 4) What’s New Template

Use only real shipped changes.

```text
What’s New in vX.Y.Z
- [Feature] ...
- [Improvement] ...
- [Fix] ...
```

Example:

```text
What’s New in v0.1.0
- Added polished win/lose sequences and improved end-of-level combat presentation.
- Added progression panel and replay flow improvements.
- Improved pause menu layout and in-game options reliability.
- Tuned performance and offline safety checks for iPhone release.
```

## 5) App Review Notes Template

```text
Reviewer Notes:
- This app is fully playable offline.
- No account/login required.
- No ad SDK, no in-app purchases, no analytics/telemetry.
- Portrait orientation only.
- Main flow: Main Menu -> Play -> Gameplay -> Finish Battle -> Win/Lose -> Next/Retry.
```

## 6) Support + Privacy URLs

- Marketing URL (optional): `https://leggoboyo.github.io/MultiplyRush/`
- Support URL: `https://leggoboyo.github.io/MultiplyRush/support.html`
- Privacy Policy URL: `https://leggoboyo.github.io/MultiplyRush/privacy.html`

### Support URL content checklist
Before submission, confirm the Support URL page includes:
- Working support contact route (issue tracker and/or email form)
- Clear instructions for bug reports
- Contact details appropriate for your distribution regions

## 7) Screenshot Plan (iPhone)
Capture real gameplay and current UI only:
1. Main menu
2. Mid-run gate decision
3. End battle moment
4. Result panel (win/lose)
5. Progression/replay panel

Tips:
- Use safe area framing to avoid notch clipping.
- Do not use placeholder text or non-representative UI.

## 8) Age Rating Draft Notes
Current content profile for questionnaire review:
- Cartoon/fantasy combat (crowd battle presentation)
- No gambling
- No user-generated chat/content
- No unrestricted web access

Re-validate answers every release if content changes.

## 9) Official References
- App information: https://developer.apple.com/help/app-store-connect/reference/app-information/app-information
- Platform version information: https://developer.apple.com/help/app-store-connect/reference/app-information/platform-version-information
- Submit an app: https://developer.apple.com/help/app-store-connect/manage-submissions-to-app-review/submit-an-app
- Screenshot specifications: https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications
- Age rating: https://developer.apple.com/help/app-store-connect/manage-app-information/set-an-app-age-rating
