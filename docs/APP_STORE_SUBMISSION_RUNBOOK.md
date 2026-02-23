# Multiply Rush App Store Submission Runbook

Last updated: 2026-02-23  
Owner: ZoKorp Games

## 1) Purpose
Use this runbook to submit iOS builds with a high first-pass App Review success rate while preserving project promises:
- Offline-first gameplay
- No ads
- No analytics/telemetry
- No in-app purchases

## 2) Current Apple Time-Sensitive Gates
Validate these before every upload:

1. **SDK minimum requirement (begins April 28, 2026):** uploads must be built with Xcode 26+ and iOS 26 SDK (or later).  
2. **Age rating updates (since January 31, 2026):** Apple moved to the updated age-rating system; complete the updated questionnaire in App Store Connect.  
3. **Required reasons APIs (since May 1, 2024):** if your app or third-party SDKs use listed APIs, include approved reasons.

## 3) Required Metadata and Review Inputs (iOS)
Before clicking Submit for Review, confirm all required fields are complete and current.

### 3.1 App Information
- App name: 2-30 characters.
- Subtitle: max 30 characters.
- Privacy Policy URL: required for iOS apps.
- Age Rating: required (via questionnaire).

### 3.2 Platform Version Information
- Screenshots: required (1-10, `.jpeg`/`.jpg`/`.png`).
- Description: required, plain text, up to 4000 characters.
- Keywords: required, up to 100 bytes.
- Support URL: required and must point to real support contact info.
- What’s New: required for updates (not first version), up to 4000 characters.

### 3.3 App Review Information
- Contact: name, email, phone number.
- Review Notes: include test instructions for non-obvious behavior.
- Sign-in credentials: only if login is required (not applicable for current Multiply Rush).

## 4) Submission Workflow (Operational)
1. In Unity, run `Multiply Rush -> Release -> Prepare iOS Release Candidate`.
2. Open `My project/Assets/MultiplyRush/Docs/ReleaseAudit.md` and resolve all `[FAIL]` items.
3. Complete a physical-device airplane-mode test.
4. Update metadata using `docs/APP_STORE_METADATA_TEMPLATES.md`.
5. Upload build to App Store Connect.
6. In App Store Connect, attach the correct build and add required metadata.
7. Click **Add for Review**, then **Submit for Review**.
8. Monitor App Review messages and respond quickly if clarification is requested.

## 5) Rejection-Prevention Quality Bar
Align with App Review guidance every release:
- Final binaries only: no placeholder text, no empty URLs, no temporary content.
- Metadata must accurately reflect gameplay, features, and monetization.
- Keep support and privacy URLs live and correct.
- Test on device for crashes and obvious defects before submission.
- Keep app actively maintained; unsupported/broken apps can be removed.

## 6) Multiply Rush-Specific Release Checklist
Mark each item per release packet.

- [ ] Unity compile clean (0 errors).
- [ ] `ReleaseAudit.md` has 0 `[FAIL]`.
- [ ] Airplane mode test passed (menu -> gameplay -> finish -> result).
- [ ] No ad/analytics/IAP/network SDKs present.
- [ ] Metadata copied from templates and manually proofread.
- [ ] Screenshots reflect current UI and real gameplay.
- [ ] Support URL contains working contact channel and legal contact details as required in your target regions.
- [ ] Privacy policy URL is reachable and aligned with implementation.
- [ ] Age rating questionnaire answers reviewed for current content.
- [ ] “What’s New” text matches real shipped changes.
- [ ] Release packet created from `docs/RELEASE_PACKET_TEMPLATE.md`.

## 7) Release Artifacts (Source of Truth)
- Changelog: `CHANGELOG.md`
- Compliance matrix: `docs/APP_STORE_COMPLIANCE_MATRIX.md`
- Metadata templates: `docs/APP_STORE_METADATA_TEMPLATES.md`
- Release packet template: `docs/RELEASE_PACKET_TEMPLATE.md`
- Maintenance policy: `docs/MAINTENANCE_POLICY.md`

## 8) References (Official Apple)
- App Store Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- Submit an app: https://developer.apple.com/help/app-store-connect/manage-submissions-to-app-review/submit-an-app
- Platform version information: https://developer.apple.com/help/app-store-connect/reference/app-information/platform-version-information
- App information: https://developer.apple.com/help/app-store-connect/reference/app-information/app-information
- App privacy details: https://developer.apple.com/app-store/app-privacy-details/
- App privacy (App Store Connect reference): https://developer.apple.com/help/app-store-connect/reference/app-information/app-privacy/
- Set an app age rating: https://developer.apple.com/help/app-store-connect/manage-app-information/set-an-app-age-rating
- Screenshot specifications: https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications
- Upcoming requirements: https://developer.apple.com/news/upcoming-requirements/
