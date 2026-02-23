# App Store Compliance Matrix (Multiply Rush)

Last updated: 2026-02-23

This maps key Apple requirements to concrete project evidence and checks.

## Review and Policy Coverage

| Apple Area | Requirement | Project Evidence | Verification |
|---|---|---|---|
| 2.1 App Completeness | App must be complete and functional at review | Main menu, gameplay, pause, result loops are implemented in shipped scenes | QA checklist + release audit |
| 2.3 Accurate Metadata | Store metadata must match app behavior | Metadata templates reflect real features only | Metadata review before submission |
| 2.3.12 What’s New | What’s New must describe actual changes | `CHANGELOG.md` drives release notes | Release checklist signoff |
| 4.0 Design | App should meet quality and usability standards | Safe-area aware UI, pause options, progression flow | iPhone simulator + device pass |
| 5.1 Data Collection | Privacy disclosure must be accurate | Offline architecture, no telemetry/analytics SDK usage | `offline_policy_check.py` + release audit |
| 1.5 Developer Info | Support and contact info required | Public support and privacy pages in `docs/` | Verify URL availability pre-submit |
| Upcoming Requirements | Toolchain deadlines must be met | Runbook tracks Xcode 26 / iOS 26 SDK deadline (2026-04-28) | Submission date gate in release checklist |

## Offline/No-Ads/No-Tracking Promise

| Promise | Implementation | Check |
|---|---|---|
| No internet required | No runtime network logic in gameplay scripts | Airplane-mode test run |
| No ads | No ad SDK dependencies in manifest | Release audit package scan |
| No analytics/telemetry | Unity Connect defaults forced off in release tool | Release audit + CI policy script |
| No IAP/paywalls | No purchasing package integration | Manifest + code scan |

## Build Integrity

| Area | Requirement | Check |
|---|---|---|
| iOS backend | IL2CPP | Release audit |
| iOS architecture | ARM64 | Release audit |
| Build scenes | MainMenu + Game included and enabled | Release audit |
| Versioning | Valid bundle version + build number | Release audit + manual gate |

## Residual Risks to Re-check Every Submission
- App Store metadata drift (text/screenshots not matching current build).
- Placeholder art/text accidentally left in production build.
- UI clipping on new iPhone aspect ratios.
- Future package additions violating offline/no-tracking policy.

## References
- App Store Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- App Store Connect Submission: https://developer.apple.com/help/app-store-connect/manage-submissions-to-app-review/submit-an-app-for-review
