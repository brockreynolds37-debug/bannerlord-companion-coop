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

### Client mission behavior

`CompanionDropInMissionClient` is intended to:
- receive seat offers
- submit seat claims
- switch local control onto the assigned companion agent

### Seat registry

`CompanionSeatRegistry` is pure C# state used to keep mission seat definitions and reservations separate from Bannerlord API details.

## Deferred items

- Campaign hero lookup and persistence
- Network message contracts backed by Bannerlord compression/serialization
- Host UI for choosing which companions are guest-playable
- Filtering of which mission types allow guest participation
- Possession handoff if a companion is wounded, captured, or absent
- Recovery if a guest disconnects mid-mission

## Practical next step

On a Windows PC with Bannerlord installed:
- reference the real TaleWorlds assemblies
- compile this scaffold
- trim any API mismatches
- get a custom multiplayer mission to boot
- hardcode one fake companion seat and prove the join flow

