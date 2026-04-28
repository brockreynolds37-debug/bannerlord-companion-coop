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

The spectator side of that design is intentionally lighter: guests do not become second campaign actors, but they should still be able to watch the host's campaign state in a passive "passenger seat" mode before the next mission starts.

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

It now also polls the host's live campaign state while a campaign is running and stores a read-only spectator snapshot for future guest transport and UI work.

### Game mode

`CompanionDropInGameMode` creates a mission with:
- Bannerlord lobby/network behaviors
- custom server behavior
- custom client behavior

Its `JoinCustomGame` path now mirrors Bannerlord's native mission-based multiplayer flow by pushing a custom-game lobby state and letting the engine call back into `StartMultiplayerGame` when the mission should load.

### Server mission behavior

`CompanionDropInMissionServer` is intended to:
- receive the host's allowed companion seats
- validate guest claims
- decide which remote player can possess which hero
- push spawn/possession state to clients

In the current repo pass it already uses a `CompanionMissionCoordinator` with:
- live companion extraction from the host's current campaign party when available
- a debug fallback roster when no campaign context is available
- seat publication into the mission registry
- a controllable automation bridge instead of a hardcoded fake guest claim

For real campaign battles, the submodule now injects a lighter `CompanionCampaignMissionHostBehavior` directly into supported campaign missions. That preserves the campaign's native troop/scene setup while still giving the co-op layer a host seat plan to work with.

`CompanionCampaignCustomServerRegistrationBehavior` now sits beside that host behavior for campaign battles. It is responsible for:
- initializing Bannerlord multiplayer services when needed
- starting a player-hosted multiplayer session on top of the live campaign battle
- registering the battle as a `CompanionDropIn` custom game
- updating the published server metadata as companion seat capacity changes
- tearing the temporary hosted session down when the mission ends

### Client mission behavior

`CompanionDropInMissionClient` is intended to:
- receive seat offers
- submit seat claims
- switch local control onto the assigned companion agent

It now also applies the latest host campaign spectator snapshot when the battle session syncs, so the guest gets immediate context like:
- where the host was
- whether they were entering a settlement or encounter
- the most recent passenger-mode event line

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

### Campaign spectator tracker

`CompanionCampaignSpectatorTracker` is the current passenger-mode foundation. It polls host campaign state and builds a transport-neutral `CompanionCampaignSpectatorSnapshot` with:
- current summary text
- settlement and target context
- gold / party size / food runway
- map position
- a short recent-event log

This gives the next UI/networking pass something stable to render without forcing a full second campaign-map implementation.

`CompanionCampaignSpectatorSession` is the guest-side mirror for that data. It now receives the last host snapshot during battle mission sync, but it still does not render a dedicated waiting-state UI yet.

## Deferred items

- Host UI for choosing which companions are guest-playable
- Continuous guest transport and rendering for the campaign spectator snapshot before battle join
- Filtering of which mission types allow guest participation
- Possession handoff if a companion is wounded, captured, or absent
- Recovery if a guest disconnects mid-mission
- External transport for automation commands instead of direct server method calls

## Practical next step

On a Windows PC with Bannerlord installed:
- reference the real TaleWorlds assemblies
- compile this scaffold
- boot one hosted guest flow and verify `discover -> join -> claim -> assignment -> possession`
- confirm campaign battles can register and tear down cleanly without breaking the native troop setup
- verify the new battle-sync spectator context arrives for the guest as expected
- decide whether the pre-battle spectator feed should ride over lobby metadata, a lightweight guest sync channel, or a custom waiting-state transport
