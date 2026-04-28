using System;
using BannerlordCompanionCoop.Contracts;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public interface ICompanionMissionHost
{
    event Action<CompanionMissionPlan?>? MissionPlanChanged;

    CompanionMissionPlan? LatestPlan { get; }

    CompanionMissionJoinScope CurrentJoinScope { get; }

    void EnsureMissionStarted();

    int ReleaseRemotePlayer(string remotePlayerId);

    bool TryClaimSeatForPeer(NetworkCommunicator networkPeer, string seatId, out string message);
}
