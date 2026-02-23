# App Store Compliance Matrix (Multiply Rush)

Last updated: 2026-02-23

This maps Apple requirements to concrete project evidence and submission checks.

## 1) Review and Metadata Requirements

| Requirement Area | Apple Requirement | Project Evidence | Verification Gate |
|---|---|---|---|
| 2.1 App Completeness | App submissions should be final and functional; no placeholder content or broken URLs | Shipping scenes: `MainMenu.unity`, `Game.unity`; public docs site under `docs/` | Device smoke test + URL checks before submit |
| 2.3 Accurate Metadata | Metadata must accurately describe the app | `docs/APP_STORE_METADATA_TEMPLATES.md`; `CHANGELOG.md` | Manual metadata review gate |
| 2.3.12 What’s New | Update text must describe real changes | `CHANGELOG.md` is release notes source | Release packet signoff |
| App Information | Privacy Policy URL required for iOS apps | `docs/privacy.html` published via GitHub Pages | Open URL and verify content |
| App Information | Age rating required and must match content | Age rating questionnaire answers in ASC | Pre-submit ASC review |
| Platform Version Info | Screenshots required (1-10 per set), description required, keywords required | Screenshot capture workflow + templates | Submission checklist |
| Platform Version Info | Support URL required and must lead to support contact info | `docs/support.html` (must include valid contact channel) | Open URL and verify contact details |
| App Review Info | Contact name/email/phone required; notes and demo credentials when needed | Reviewer notes template in metadata doc | Pre-submit review info check |

## 2) Privacy, Data, and Policy Requirements

| Requirement Area | Apple Requirement | Project Evidence | Verification Gate |
|---|---|---|---|
| App Privacy Details | Privacy details are required for new apps and app updates | Offline/no-tracking design documented in privacy/support pages | App Privacy section completed each release |
| 5.1 Privacy Alignment | Privacy disclosures must match actual implementation | `tools/offline_policy_check.py`; no ads/analytics/IAP claim in docs | CI + release audit |
| Required Reasons APIs | If listed APIs are used, approved reasons are required (including third-party SDK usage) | Dependency review during release prep | Package audit before submit |

## 3) Build and Toolchain Requirements

| Requirement Area | Apple Requirement | Project Evidence | Verification Gate |
|---|---|---|---|
| Upcoming Requirement | Starting 2026-04-28, uploads require Xcode 26+ with iOS 26 SDK+ | Runbook date gate in `docs/APP_STORE_SUBMISSION_RUNBOOK.md` | Submission date + toolchain check |
| Build Integrity | App must be stable and reviewable | Unity release audit + physical device playtest | `ReleaseAudit.md` with 0 `[FAIL]` |

## 4) Ongoing Maintenance Expectations

| Requirement Area | Apple Requirement | Project Evidence | Verification Gate |
|---|---|---|---|
| Active Maintenance | Apps that are no longer supported or fail to function may be removed | `docs/MAINTENANCE_POLICY.md`; changelog discipline | Monthly maintenance check |
| Supportability | Support URL should keep player/reviewer contact routes live | GitHub Pages support hub | Broken-link check before each release |

## 5) Residual Risks (Re-check Every Submission)
- Metadata drift: screenshots/text no longer match gameplay.
- Support URL exists but does not include usable contact information.
- Privacy claim drift after future package additions.
- App Review notes too vague for reviewer to validate quickly.
- Toolchain deadline missed near submission date.

## 6) Official References
- App Store Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- Submit an app: https://developer.apple.com/help/app-store-connect/manage-submissions-to-app-review/submit-an-app
- Platform version information: https://developer.apple.com/help/app-store-connect/reference/app-information/platform-version-information
- App information: https://developer.apple.com/help/app-store-connect/reference/app-information/app-information
- App privacy details: https://developer.apple.com/app-store/app-privacy-details/
- Set an app age rating: https://developer.apple.com/help/app-store-connect/manage-app-information/set-an-app-age-rating
- Upcoming requirements: https://developer.apple.com/news/upcoming-requirements/
