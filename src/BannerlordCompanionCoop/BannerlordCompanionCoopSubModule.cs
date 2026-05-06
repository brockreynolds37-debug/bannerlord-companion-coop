using System.Linq;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Diagnostics;
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
    private static readonly CompanionCampaignPassengerHost CampaignPassengerHost =
        new(() => CampaignSpectatorTracker.LatestSnapshot);

    public static CompanionCampaignSpectatorSnapshot? LatestCampaignSpectatorSnapshot =>
        CampaignSpectatorTracker.LatestSnapshot;

    public static string? LatestCampaignSpectatorSnapshotJson =>
        CampaignSpectatorTracker.LatestSnapshotJson;

    public static CompanionCampaignSpectatorSession CampaignSpectatorSession => RemoteCampaignSpectatorSession;

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        Module.CurrentModule.AddMultiplayerGameMode(new CompanionDropInGameMode("CompanionDropIn"));
        CampaignPassengerHost.Start();
        CompanionModLogger.Info(
            "SubModule",
            $"Loaded Bannerlord Companion Co-op. Passenger feed: http://localhost:{CampaignPassengerHost.Port}/. Log file: {CompanionModLogger.LogFilePath}");
    }

    protected override void OnSubModuleUnloaded()
    {
        CampaignPassengerHost.Stop();
        base.OnSubModuleUnloaded();
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

        CompanionModLogger.Info(
            "SubModule",
            $"Injecting campaign co-op behaviors into mission '{mission.SceneName}' with scope '{CompanionMissionSceneScopeResolver.ResolveForMission(mission)}'.");
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
