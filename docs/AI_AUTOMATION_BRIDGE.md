# AI Automation Bridge

## Result

This repo now has a narrow automation/debug bridge aimed at the co-op vertical slice, not a generic full-game AI driver.

That bridge can:
- inspect current mission/session state
- list seat offers and assignments
- initialize the debug mission flow
- publish seats for a join scope
- claim a seat for a remote player
- release a remote player
- advance mission state to `MissionLive`
- end the mission

## Why this shape

`Bannerlord.GABS` is broad. It is useful for game inspection and UI traversal, but it is not tailored to the actual engineering risk in this repo.

For this mod, the important thing is being able to automate and observe:
- host session state
- companion seat publication
- guest seat claims
- assignment resolution
- mission lifecycle transitions

That is what this bridge exposes.

## Entry points

Current server-side entry points live in:
- `CompanionDropInMissionServer.BuildAutomationSnapshot()`
- `CompanionDropInMissionServer.BuildAutomationSnapshotJson()`
- `CompanionDropInMissionServer.ExecuteAutomationCommand(...)`
- `CompanionDropInMissionServer.ExecuteAutomationCommandJson(...)`

Core bridge services live in:
- `CompanionAutomationBridge`
- `CompanionAutomationProtocol`

## Commands

Supported command kinds:
- `GetSnapshot`
- `InitializeDebugMission`
- `PublishSeatsForMission`
- `ClaimSeat`
- `ReleaseRemotePlayer`
- `BeginMission`
- `EndMission`

Command contract:

```json
{
  "commandId": "claim-1",
  "kind": "ClaimSeat",
  "seatId": "companion_alayen:battles",
  "remotePlayerId": "guest_1",
  "joinScope": "Battles"
}
```

Typical result payload:

```json
{
  "commandId": "claim-1",
  "kind": "ClaimSeat",
  "success": true,
  "message": "Seat 'companion_alayen:battles' claimed for remote player 'guest_1'.",
  "snapshot": {
    "saveId": "debug_sandbox_save",
    "joinScope": "Battles",
    "state": "WaitingForGuestSelections",
    "summary": "state=WaitingForGuestSelections; save=debug_sandbox_save; scope=Battles; seats=4; claims=1; assignments=1"
  }
}
```

## How to use it

Near term, this bridge is meant to be driven by one of these:
- a tiny Bannerlord-specific test harness
- `Bannerlord.CSharp.Scripting` commands inside a running game
- a future GABS adapter that maps external tool calls onto these command contracts

The point is to keep the co-op behavior API stable even if the outer automation shell changes.

## Practical next step

On the Windows Bannerlord box:
1. rebuild the module
2. boot the custom mission mode
3. call the bridge from an in-game script or temporary debug hook
4. verify `snapshot -> claim -> begin mission`
5. only then decide whether a deeper GABS integration is worth the extra surface area
