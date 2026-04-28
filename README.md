# Bannerlord Companion Co-op

Host-authoritative Bannerlord campaign co-op scaffold.

Current scope:
- Host owns the campaign world and all campaign-time decisions.
- Guests do not manage inventory, map movement, or campaign menus.
- Guests can join host-owned missions and request control of a companion in battle.
- Mission participation is the first build target. Campaign synchronization beyond mission entry is deferred.

Companion seats are now modeled as multi-scene permissions rather than single-scene assignments, so one published companion can be reused across battles, town visits, raids, and hideouts while the active mission snapshot filters to the current scene type.

## Repo layout

- `src/BannerlordCompanionCoop`: C# module code
- `ModuleData`: multiplayer strings
- `docs`: architecture and next-step notes

## Current status

This repo is a starter scaffold. It establishes:
- Bannerlord module metadata
- Multiplayer game mode registration
- Mission-side server/client behavior shells
- A TaleWorlds-free core library for seat/session state
- A seat registry for binding remote players to host-owned companion heroes
- A host session model that can publish companion seats into a mission snapshot
- A mission-plan snapshot shape with seat offers and resolved assignments
- A real mod network slice for mission-plan sync and seat claiming
- Native custom-game guest bootstrap via Bannerlord's lobby join flow
- A battle-only possession path that hands a claimed companion agent to a guest peer
- A first-pass battle hotkey flow that lets a guest look near a companion and press `O` to request control
- A host-side campaign spectator snapshot with a short event log for future passenger-mode UI
- A guest-side spectator session model that can consume remote campaign snapshots when pre-battle transport is wired
- Live host-campaign companion extraction when the mission is launched from a campaign context
- Battle-only campaign mission injection so real campaign fights can host co-op seat state
- Campaign battle registration that starts a player-hosted session and publishes the fight to Bannerlord's custom server list
- A debug fallback roster when no campaign context is available
- Design notes for the next implementation passes

It does not yet implement:
- A guest-facing read-only campaign spectator view or transport for those snapshots
- Reliable possession flows for town scenes, hideouts, and raids
- Scene filtering and join flow UI
- Full save/persistence recovery around disconnects and mission end

## Build setup on PC

1. Install Bannerlord and the Bannerlord Dedicated Server on the same version.
2. Copy this module folder into `Mount & Blade II Bannerlord/Modules/`.
3. Copy `BannerlordAssemblyPaths.props.example` to `BannerlordAssemblyPaths.props`.
4. Fill in the local install paths in that props file.
5. Run `.\build.ps1` from the repo root to bootstrap a local .NET 6 SDK and build the module.
6. The mod DLLs are emitted to `bin/Win64_Shipping_Client/`, which matches Bannerlord's expected module layout.
7. You can still open the project in Visual Studio 2022 if you prefer, but the script is the fastest path to a local test build.

## First implementation target

Get a modded multiplayer mission booting with:
- one host
- one guest
- one reserved companion seat
- a hardcoded test scene and spawn flow

After that, wire the campaign host into mission launch and possession.
