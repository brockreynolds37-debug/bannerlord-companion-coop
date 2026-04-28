# Next Steps

## Immediate

1. Confirm the module appears in the launcher and the custom multiplayer mode can boot.
2. Run a real host-and-guest battle test for:
   - server discovery from a live campaign battle
   - mission-plan sync
   - hotkey control request near a companion
   - battle possession handoff
3. Verify the new campaign battle injection path preserves the real campaign troop setup while the co-op seat host behavior initializes.
4. Exercise the automation bridge on the Windows machine to prove `snapshot -> claim -> begin mission` without source edits.
5. Decide whether the host needs to pre-open the Bannerlord multiplayer lobby once per boot, or whether the new auto-login path is reliable enough to keep invisible.
6. Verify the guest now receives the host's last campaign context on battle join and that the extra status lines are not too noisy.
7. Choose the first true passenger-mode transport for the new campaign spectator snapshot:
   - lobby metadata
   - a lightweight waiting-state sync channel
   - or a custom guest spectator screen bootstrap
8. Decide whether the battle server name should carry a shortened location tag permanently or only as a temporary debugging aid.

## After bootstrapping

1. Render the new host snapshot in a read-only guest spectator waiting view so players can watch the host move, enter settlements, and start encounters before the next battle loads.
2. Add a tiny host UI for toggling which companions are guest-playable.
3. Add guest join restrictions by scene type:
   - battles
   - town visits
   - hideouts and raids
4. Handle disconnect recovery and fallback AI control.
5. Decide whether to keep the automation bridge internal or hang a fuller GABS-style adapter off it.

## Not for the first pass

- guest inventory control
- guest campaign map movement
- guest menu ownership
- multiple simultaneous host parties
- equal-authority campaign peers
