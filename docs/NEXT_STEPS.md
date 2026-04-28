# Next Steps

## Immediate

1. Add real assembly references on a Windows PC and fix API mismatches against the installed Bannerlord version.
2. Confirm the module appears in the launcher and the custom multiplayer mode can boot.
3. Replace the temporary debug claim flow with Bannerlord network messages:
   - host seat list
   - guest seat claim
   - seat claim approval
   - mission spawn possession assignment
4. Replace the debug seat catalog with real companion extraction from the host campaign session.
5. Hardcode one supported mission type first, ideally a battle mission.
6. Exercise the new automation bridge on the Windows machine to prove `snapshot -> claim -> begin mission` without source edits.

## After bootstrapping

1. Add a host-only campaign session manager that reads active companion heroes from the campaign.
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
