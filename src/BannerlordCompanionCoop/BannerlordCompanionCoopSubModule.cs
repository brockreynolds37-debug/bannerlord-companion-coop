using BannerlordCompanionCoop.Modes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop;

public sealed class BannerlordCompanionCoopSubModule : MBSubModuleBase
{
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        Module.CurrentModule.AddMultiplayerGameMode(new CompanionDropInGameMode("CompanionDropIn"));
    }

    protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
    {
        base.InitializeGameStarter(game, gameStarterObject);
        game.GameTextManager.LoadGameTexts(
            ModuleHelper.GetModuleFullPath("BannerlordCompanionCoop") + "ModuleData/multiplayer_strings.xml");
    }
}

