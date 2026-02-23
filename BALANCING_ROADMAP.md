# Multiply Rush Balancing Roadmap

This roadmap is focused on perpetual progression, fair difficulty scaling, and stable performance on iOS devices.

## Current State (What Exists Now)

- Infinite level index already exists (`LevelIndex` is unbounded).
- Difficulty currently scales through:
  - forward speed per level
  - gate value generation and gate type mix
  - moving gates / tempo / hazards / adaptive lane pressure
  - enemy count derived from route reference + best-case path estimate
  - mini-boss cadence every 10 levels
- Core balancing systems live in:
  - `My project/Assets/MultiplyRush/Scripts/Gameplay/LevelGenerator.cs`
  - `My project/Assets/MultiplyRush/Scripts/Core/DifficultyRules.cs`
  - `My project/Assets/MultiplyRush/Scripts/Gameplay/GameManager.cs`

## Immediate Decision Applied

- Survivor carryover between rounds is disabled.
- Next level now starts from the level-generated start count (plus optional consumable buff effects only).
- This prevents runaway army inflation from level chaining and keeps each level's economy self-contained.

## Key Risks Remaining

1. Forward speed can become unplayable at very high levels if uncapped for long enough.
2. Gate-shot upgrade mechanics can over-inflate value growth on long/high-density levels.
3. Enemy scaling is coupled to expected-best path, which can drift if gate economy grows too quickly.
4. Row count and pattern caps protect performance but can flatten perceived challenge variety late-game.

## Recommended Balancing Targets

Use these as hard targets during tuning:

- Non-boss win margin target:
  - Easy: 1.10x to 1.35x enemy count at finish for average successful run.
  - Normal: 1.02x to 1.18x.
  - Hard: 1.00x to 1.10x.
- Mini-boss win rate target:
  - Easy: 70-80%
  - Normal: 55-65%
  - Hard: 40-50%
- End-of-level count stability:
  - Avoid regular finishes above low-thousands except late game.
  - Keep normal/hard median in low-hundreds for readability and UX.

## Roadmap Phases

## Phase 1: Stability Baseline (Now)

- Keep carryover off (done).
- Validate these 3 checkpoints for each difficulty:
  - Level 10
  - Level 50
  - Level 100
- Measure:
  - start count
  - reference route count
  - enemy count
  - average survivors on win

## Phase 2: Economy Compression

- Apply soft growth limits to gate economy while preserving progression feel:
  - lower additive growth slope at high levels
  - tighten multiplier budget at high levels
  - slow shot-upgrade scaling beyond midgame
- Goal: linear-feeling difficulty without exponential count spikes.

## Phase 3: Difficulty Curve Normalization

- Move to piecewise curves:
  - early game: onboarding (low punishment, clear wins)
  - mid game: skill expression (precision + movement)
  - late game: stable hard plateau + periodic spikes (boss/modifier levels)
- Keep “harder forever” by increasing pressure mix and precision demand, not just raw numbers.

## Phase 4: Encounter Quality

- Ensure every 10th level mini-boss has:
  - distinctive timing pattern
  - predictable telegraph
  - damage windows readable on phone screens
- Add additional boss archetypes later without changing core economy.

## Phase 5: Release Hardening

- Final balancing sweep with deterministic test seeds.
- Lock parameter bands for release branch.
- Document all tunables with suggested min/max ranges.

## Suggested Next Engineering Task

Build a small editor/debug report that exports balancing snapshots for selected levels and all 3 difficulties in one click.

This gives fast confidence before each release and prevents accidental curve regressions.
