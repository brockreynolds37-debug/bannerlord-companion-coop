# Bannerlord Companion Co-op

Host-authoritative Bannerlord campaign co-op scaffold.

Current scope:
- Host owns the campaign world and all campaign-time decisions.
- Guests do not manage inventory, map movement, or campaign menus.
- Guests can reserve a companion seat and drop into supported missions like battles, town scenes, and raid-style encounters.
- Mission participation is the first build target. Campaign synchronization beyond mission entry is deferred.

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
- A hardcoded debug mission path that seeds test companions and creates one fake remote assignment
- Design notes for the next implementation passes

It does not yet implement:
- Real Bannerlord network message serialization
- Campaign save integration
- Hero lookup from a live campaign save
- Mission spawn possession
- Scene filtering and join flow UI

## Build setup on PC

1. Install Bannerlord and the Bannerlord Dedicated Server on the same version.
2. Copy this module folder into `Mount & Blade II Bannerlord/Modules/`.
3. Copy `BannerlordAssemblyPaths.props.example` to `BannerlordAssemblyPaths.props`.
4. Fill in the local install paths in that props file.
5. Open the project in Visual Studio 2022 with .NET 6 support.
6. Build `BannerlordCompanionCoop.Core` first if Visual Studio needs to restore project references.

## First implementation target

Get a modded multiplayer mission booting with:
- one host
- one guest
- one reserved companion seat
- a hardcoded test scene and spawn flow

After that, wire the campaign host into mission launch and possession.
