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
- A guest-side spectator session model that now receives the host's last campaign snapshot when the battle mission syncs
- Automatic guest seat requests that reuse the last claimed companion when possible and otherwise claim the first available seat for each new mission instance
- Host-side preferred seat restoration so returning guests can snap back to the same companion across later mission syncs when that seat still exists
- Late-bound mission network registration for campaign-hosted battles, so the seat-sync handlers still come online even when the battle starts as a singleplayer mission before the multiplayer host session spins up
- A first-pass runtime diagnostics log under `Documents/Mount and Blade II Bannerlord/Configs/ModLogs/`
- Live host-campaign companion extraction when the mission is launched from a campaign context
- Battle-only campaign mission injection so real campaign fights can host co-op seat state
- Campaign battle registration that starts a player-hosted session and publishes the fight to Bannerlord's custom server list
- A launcher/runtime manifest that declares the external `Multiplayer` DLL dependency explicitly for the singleplayer campaign module
- A debug fallback roster when no campaign context is available
- Design notes for the next implementation passes

It does not yet implement:
- A Steam/friends-list invite button; the current test flow uses Bannerlord's custom game browser once a host battle is published
- A guest-facing read-only campaign spectator waiting screen before battle join
- Continuous pre-battle transport for spectator snapshots while the host is still riding around on the map
- Reliable possession flows for town scenes, hideouts, and raids
- Scene filtering and join flow UI
- Full save/persistence recovery around disconnects and mission end

## Current host/guest test flow

1. Host enables the mod stack and launches `Singleplayer`.
2. Host loads a campaign with at least one companion in the party.
3. Host enters a normal campaign battle.
4. The mod attempts to publish that live battle as a custom game named like `Companion Co-op Battle`.
5. Guest installs the same mod build, enables it for the multiplayer/custom-game side, launches Bannerlord multiplayer, and opens the custom game browser.
6. Guest joins the host's published `CompanionDropIn` custom game.
7. Once loaded into battle, the guest should auto-claim a companion when possible, or press `O` near an eligible companion to request control.

External guests may need the host's firewall/router to allow Bannerlord traffic on UDP port `9999`, which is the current test host port.

## Framework compatibility

The module now declares optional load-order compatibility with the common Bannerlord framework stack:
- `Bannerlord.Harmony`
- `Bannerlord.ButterLib`
- `Bannerlord.UIExtenderEx`
- `Bannerlord.MBOptionScreen` (MCM v5)

Those modules are not required to run Bannerlord Companion Co-op today, and the mod does not call their APIs directly yet. The compatibility work here is load-order metadata only, so if that framework stack is enabled it loads ahead of this module cleanly in both the vanilla launcher and BUTR-style community loaders.

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
