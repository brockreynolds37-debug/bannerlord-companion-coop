# Architecture

## Current decision

This mod is being built as host-authoritative campaign co-op with mission-only guest control.

That means:
- The host runs the real campaign.
- The host save is the only source of truth.
- Remote players are guests, not peers with equal campaign authority.
- Guests bind to companion seats and only take over during supported missions and scenes.

## Why this scope

Trying to make every connected player a full campaign controller creates immediate problems with:
- campaign time control
- world-map movement
- menu ownership
- inventory and economy conflicts
- save reconciliation

Restricting guest authority to mission participation cuts through most of that. It still leaves hard work, but it is the smallest version that can become playable.

## First vertical slice

1. Host starts a campaign-backed co-op session.
2. Host publishes one or more companion seats.
3. Guest joins the session and claims one seat.
4. Host enters a supported mission.
5. Guest spawns as the claimed companion hero.
6. Mission ends and control returns to the host campaign.

## Components

### Module bootstrap

`BannerlordCompanionCoopSubModule` registers the custom multiplayer mission mode and loads multiplayer strings.

### Game mode

`CompanionDropInGameMode` creates a mission with:
- Bannerlord lobby/network behaviors
- custom server behavior
- custom client behavior

This is the first place likely to need version-specific cleanup once tested against real Bannerlord DLLs.

### Server mission behavior

`CompanionDropInMissionServer` is intended to:
- receive the host's allowed companion seats
- validate guest claims
- decide which remote player can possess which hero
- push spawn/possession state to clients

In the current repo pass it already uses a `CompanionMissionCoordinator` with:
- a hardcoded debug save id
- a small debug companion catalog
- seat publication into the mission registry
- a controllable automation bridge instead of a hardcoded fake guest claim

### Client mission behavior

`CompanionDropInMissionClient` is intended to:
- receive seat offers
- submit seat claims
- switch local control onto the assigned companion agent

### Seat registry

`CompanionSeatRegistry` is pure C# state used to keep mission seat definitions and reservations separate from Bannerlord API details.

It now also builds resolved mission assignments so the spawn/possession layer can consume a simpler list of:
- seat id
- hero id
- remote player id
- join scope

It also now builds seat-offer snapshots, enforces:
- one active seat per remote player
- scope compatibility between seat and mission
- cleanup of stale reservations when the host republishes seat definitions

### Mission coordinator

`CompanionMissionCoordinator` is the current vertical-slice orchestrator. It sits between a campaign-backed host session and the mission registry. Right now it is intentionally version-agnostic and does four concrete things:
- starts a host session
- publishes companion seats
- accepts claims
- produces mission assignments

It now also produces a transport-neutral `CompanionMissionPlan` snapshot so the actual Bannerlord network layer can later serialize one stable shape instead of querying several services ad hoc.

This is the layer that should stay mostly testable even as the Bannerlord API glue shifts by game version.

### Automation bridge

`CompanionAutomationBridge` is a small command/snapshot layer for AI-assisted tooling and runtime debug control.

It can:
- inspect the active mission plan
- claim or release seats
- advance mission lifecycle state
- serialize snapshots and command results as JSON

This is the seam that can later be driven by:
- an in-game scripting mod
- a temporary debug hook
- a future GABS adapter

## Deferred items

- Campaign hero lookup and persistence
- Network message contracts backed by Bannerlord compression/serialization
- Host UI for choosing which companions are guest-playable
- Filtering of which mission types allow guest participation
- Possession handoff if a companion is wounded, captured, or absent
- Recovery if a guest disconnects mid-mission
- External transport for automation commands instead of direct server method calls

## Practical next step

On a Windows PC with Bannerlord installed:
- reference the real TaleWorlds assemblies
- compile this scaffold
- trim any API mismatches
- get a custom multiplayer mission to boot
- replace the debug in-memory plan handoff with real network messages
