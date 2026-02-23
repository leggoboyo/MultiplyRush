# Maintenance Policy (Multiply Rush)

Last updated: 2026-02-23

This document defines how Multiply Rush is maintained so players and platform reviewers can see clear operational ownership.

## 1) Maintenance Commitment
Multiply Rush is maintained as an active side project with a lightweight but consistent process.

### Cadence targets
- Issue triage: within 7 days
- Critical gameplay/app-crash fixes: expedited patch path
- Routine quality/docs refresh: at least monthly
- App Store policy review: before every submission

## 2) Definition of a Release-Ready Build
A build is release-ready only when all are true:
- Unity project compiles cleanly.
- `ReleaseAudit.md` has no `[FAIL]` items.
- Device playtest passes core loop.
- Metadata and screenshots match real build behavior.
- Privacy/support URLs are live and accurate.
- Changelog is updated.

## 3) Support and Issue Handling
Primary channels:
- Player support and bug reports: GitHub Issues
- Public support hub: `docs/support.html` (GitHub Pages)

Triage labels (recommended):
- `critical`
- `bug`
- `ui/ux`
- `balance`
- `app-store`
- `docs`

## 4) Change Management
For each shipped version:
1. Update `CHANGELOG.md`.
2. Fill a release packet from `docs/RELEASE_PACKET_TEMPLATE.md`.
3. Verify App Store compliance matrix.
4. Tag release commit (`vX.Y.Z`).

## 5) Privacy and Data Posture
Current product posture:
- Offline-first
- No ads
- No analytics/telemetry
- No in-app purchases

Any change to that posture requires updates before release to:
- `docs/privacy.html`
- `docs/APP_STORE_COMPLIANCE_MATRIX.md`
- `docs/APP_STORE_METADATA_TEMPLATES.md`

## 6) Documentation Freshness Rule
Keep these files current as the canonical release set:
- `CHANGELOG.md`
- `docs/APP_STORE_SUBMISSION_RUNBOOK.md`
- `docs/APP_STORE_COMPLIANCE_MATRIX.md`
- `docs/APP_STORE_METADATA_TEMPLATES.md`
- `docs/RELEASE_PACKET_TEMPLATE.md`

If any file is stale relative to shipped behavior, update it in the same PR/release cycle.

## 7) End-of-Support Guardrail
If the project becomes inactive for an extended period, verify App Store compliance and app functionality before any new submission. Apple may remove apps that are no longer functioning or supported.
