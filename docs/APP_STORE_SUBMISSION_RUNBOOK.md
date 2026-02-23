# Multiply Rush App Store Submission Runbook

Last updated: 2026-02-23
Owner: ZoKorp Games

## 1) Goal
- Ship a stable iOS build that passes App Review on first submission.
- Preserve product promise: fully offline, no ads, no analytics/telemetry, no account required.

## 1.1) Time-Sensitive Apple Requirement
- Starting **April 28, 2026**, Apple requires iOS apps uploaded to App Store Connect to be built with **Xcode 26 / iOS 26 SDK or later**.
- Before that date, your current toolchain may still upload successfully.
- Action: schedule a Unity + Xcode validation pass ahead of that deadline.

## 2) Source of Truth
- Changelog: `CHANGELOG.md`
- Production checklist: `My project/Assets/MultiplyRush/Docs/ProductionReadiness.md`
- QA checklist: `My project/Assets/MultiplyRush/Docs/QAPlaytestChecklist.md`
- Metadata templates: `docs/APP_STORE_METADATA_TEMPLATES.md`
- Compliance matrix: `docs/APP_STORE_COMPLIANCE_MATRIX.md`

## 3) Release Gate (Must Pass Before Upload)
1. Unity compile clean (no errors).
2. `Multiply Rush -> Release -> Prepare iOS Release Candidate` completed.
3. `Assets/MultiplyRush/Docs/ReleaseAudit.md` has zero `[FAIL]`.
4. Airplane-mode playtest completed on physical iPhone.
5. QA checklist completed.
6. App Store metadata and screenshots finalized.
7. Privacy policy and support URLs live and reachable.

## 4) Build and Upload Workflow
1. Open Unity project (`My project/`) in Unity 6.3 LTS (`6000.3.9f1`).
2. Run `Multiply Rush -> Release -> Prepare iOS Release Candidate`.
3. Set final values in Unity Player Settings:
   - Bundle Identifier
   - Version (`CFBundleShortVersionString`)
   - Build Number (`CFBundleVersion`)
4. Build iOS project from Unity.
5. Open generated Xcode project.
6. Archive in Xcode (`Product -> Archive`).
7. Validate and upload to App Store Connect.
8. In App Store Connect:
   - Fill metadata and screenshots.
   - Fill App Privacy questionnaire.
   - Add review notes and contact.
   - Submit for review.

## 5) App Store Connect Checklist

### App Information
- App name, subtitle, category.
- Content rights declaration.

### Pricing and Availability
- Free pricing tier.
- Regions selected.

### App Privacy
- If no data is collected, declare accurately and keep implementation aligned.

### App Review Information
- Contact name/email/phone.
- Demo credentials: `N/A` (no login/account).
- Notes for reviewer:
  - App works fully offline.
  - No ad SDKs, no IAP, no analytics.
  - Portrait-only gameplay.

### Version Information
- Description, keywords, support URL, marketing URL (optional), privacy policy URL.
- iPhone screenshots for required sizes.
- What’s New text aligned to latest shipped changes.

## 6) Rejection Prevention for This Project
- Metadata must exactly match the real game behavior (no fake features, no misleading claims).
- Ensure no placeholder text/assets remain.
- Confirm no hidden paywalls, ad surfaces, or dead links.
- Confirm the game is playable from first launch with no internet.
- Confirm pause/menu/result overlays fit notch devices (safe area).
- Confirm no recurring runtime exceptions.

## 7) Post-Submission Operations
1. Record submitted version/build/date in internal notes.
2. Tag release commit (`vX.Y.Z`) and push tag.
3. Update `CHANGELOG.md`:
   - Move release items from `Unreleased` to the new version section.
4. Prepare hotfix path:
   - If rejection, add a dedicated changelog entry and resubmit quickly.

## 8) Apple References
- App Store Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- Submit an app for review: https://developer.apple.com/help/app-store-connect/manage-submissions-to-app-review/submit-an-app-for-review
- Screenshot specifications: https://developer.apple.com/help/app-store-connect/reference/app-store-screenshot-specifications
- App icons (HIG): https://developer.apple.com/design/human-interface-guidelines/app-icons
- Privacy manifests update: https://developer.apple.com/news/?id=z6fu1dcu
- Upcoming requirements: https://developer.apple.com/news/upcoming-requirements/
