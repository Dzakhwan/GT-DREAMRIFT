# Slime Enemy AI System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a modular, production-ready top-down Slime Enemy AI with FSM (Spawn/Patrol/Chase/Attack/Dead), billboard health UI, and NavMesh spawner for Unity 6 + com.unity.ai.navigation.

**Architecture:** Monolithic enum FSM in `SlimeAI.cs`. Integrates with existing `IDamageable` (int) and extends `EnemyHealth` with C# events. World-space billboard is optional; screen-space pool remains default.

**Tech Stack:** Unity 6 (6000.3), C#, `com.unity.ai.navigation` 2.0.13, NavMeshAgent / NavMesh / OffMeshLink.

## Global Constraints

- Keep `IDamageable.TakeDamage(int damage)` — do not change to float.
- Lock slime facing to Y-axis only (XZ plane).
- Throttle chase `SetDestination` to ~0.15s for mobile CPU.
- Follow existing project conventions under `Assets/Script/`.

---

### Task 1: Extend EnemyHealth with C# events

**Files:**
- Modify: `Assets/Script/Health System/EnemyHealth.cs`

**Interfaces:**
- Produces: `event Action<float, float> OnHealthChanged`, `event Action OnDeath`
- Also disables `SlimeAI` on death

- [x] **Step 1:** Add `OnHealthChanged` and `OnDeath` C# events
- [x] **Step 2:** Invoke `OnHealthChanged` in `Start` and `TakeDamage`
- [x] **Step 3:** Invoke `OnDeath` in `Die` and disable `SlimeAI`

---

### Task 2: BillboardUI (world-space fallback)

**Files:**
- Create: `Assets/Script/Health System/BillboardUI.cs`

- [x] **Step 1:** Implement camera-facing billboard (Y-lock friendly)
- [x] **Step 2:** Subscribe to `EnemyHealth.OnHealthChanged` for Slider/Image fill

---

### Task 3: SlimeAI FSM controller

**Files:**
- Create: `Assets/Script/AI Enemy/SlimeAI.cs`

- [x] **Step 1:** Enum FSM: Spawn, Patrol, Chase, Attack, Dead
- [x] **Step 2:** Spawn pop-up scale spring (0 → 1.3 → 1.0)
- [x] **Step 3:** Patrol with NavMesh sample + wait
- [x] **Step 4:** Chase with throttled SetDestination + OffMeshLink jump
- [x] **Step 5:** 3-phase dash (telegraph squish / slide hit / recovery)
- [x] **Step 6:** UnityEvent juice hooks + Gizmos

---

### Task 4: EnemySpawner

**Files:**
- Create: `Assets/Script/AI Enemy/EnemySpawner.cs`

- [x] **Step 1:** Spawn on valid NavMesh within radius
- [x] **Step 2:** Track population via `EnemyHealth.OnDeath`
- [x] **Step 3:** Respawn when below `maxEnemies`

---

## Setup Notes (Unity Editor)

1. Slime prefab: `NavMeshAgent` + `EnemyHealth` + `SlimeAI` + Collider + Tag/Layer as needed.
2. Optional world-space Canvas child with `BillboardUI` + Slider.
3. Scene: NavMeshSurface baked; place `EnemySpawner` with slime prefab reference.
4. Player must have Tag `Player` and implement `IDamageable` (`PlayerHealth`).

## Verification

- [ ] Enter Play Mode: slime pops in from scale 0
- [ ] Patrols, chases player in detection radius
- [ ] Dash telegraph → slide → hit/recoil or miss → recovery
- [ ] Death disables agent/AI; spawner replenishes
- [ ] Billboard (if used) faces camera and updates fill
