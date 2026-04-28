using System.Linq;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Integration;
using BannerlordCompanionCoop.Modes;
using BannerlordCompanionCoop.Missions;
using BannerlordCompanionCoop.Networking;
using BannerlordCompanionCoop.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop;

public sealed class BannerlordCompanionCoopSubModule : MBSubModuleBase
{
    private static readonly CompanionCampaignSpectatorTracker CampaignSpectatorTracker = new();
    private static readonly CompanionCampaignSpectatorSession RemoteCampaignSpectatorSession = new();

    public static CompanionCampaignSpectatorSnapshot? LatestCampaignSpectatorSnapshot =>
        CampaignSpectatorTracker.LatestSnapshot;

    public static string? LatestCampaignSpectatorSnapshotJson =>
        CampaignSpectatorTracker.LatestSnapshotJson;

    public static CompanionCampaignSpectatorSession CampaignSpectatorSession => RemoteCampaignSpectatorSession;

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        Module.CurrentModule.AddMultiplayerGameMode(new CompanionDropInGameMode("CompanionDropIn"));
    }

    protected override void OnApplicationTick(float dt)
    {
        base.OnApplicationTick(dt);

        if (Game.Current?.GameType is Campaign)
        {
            CampaignSpectatorTracker.OnApplicationTick(dt);
            return;
        }

        CampaignSpectatorTracker.Reset();
    }

    public static void ApplyRemoteCampaignSpectatorSnapshot(CompanionCampaignSpectatorSnapshot snapshot)
    {
        RemoteCampaignSpectatorSession.ApplySnapshot(snapshot);
    }

    public static void ApplyRemoteCampaignSpectatorSnapshotJson(string snapshotJson)
    {
        RemoteCampaignSpectatorSession.ApplySnapshotJson(snapshotJson);
    }

    public static void ClearRemoteCampaignSpectatorSnapshot()
    {
        RemoteCampaignSpectatorSession.Clear();
    }

    public override void OnBeforeMissionBehaviorInitialize(Mission mission)
    {
        base.OnBeforeMissionBehaviorInitialize(mission);

        if (!ShouldInjectCampaignCompanionBehaviors(mission))
        {
            return;
        }

        AddBehaviorIfMissing(mission, new CompanionCampaignMissionHostBehavior());
        AddBehaviorIfMissing(mission, new CompanionCampaignCustomServerRegistrationBehavior());
        AddBehaviorIfMissing(mission, new CompanionBattlePossessionBehavior());
        AddBehaviorIfMissing(mission, new CompanionMissionNetworkBehavior());
    }

    private static bool ShouldInjectCampaignCompanionBehaviors(Mission mission)
    {
        return mission is not null
            && Game.Current?.GameType is Campaign
            && CompanionMissionSceneScopeResolver.ResolveForMission(mission) == CompanionMissionJoinScope.Battles;
    }

    private static void AddBehaviorIfMissing(Mission mission, MissionBehavior behavior)
    {
        if (mission.MissionBehaviors.Any(existing => existing.GetType() == behavior.GetType()))
        {
            return;
        }

        mission.AddMissionBehavior(behavior);
    }
}
