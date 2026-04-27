using BannerlordCompanionCoop.Modes;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop;

public sealed class BannerlordCompanionCoopSubModule : MBSubModuleBase
{
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        Module.CurrentModule.AddMultiplayerGameMode(new CompanionDropInGameMode("CompanionDropIn"));
    }
}
