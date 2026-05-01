using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Diagnostics;
using BannerlordCompanionCoop.Integration;
using BannerlordCompanionCoop.Networking;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionBattlePossessionBehavior : MissionLogic
{
    private readonly Dictionary<string, int> _agentIndexBySeatId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _lastSeatStatusBySeatId = new(StringComparer.Ordinal);
    private ICompanionMissionHost? _host;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
        _host = Mission.MissionBehaviors.OfType<ICompanionMissionHost>().FirstOrDefault();
        CompanionModLogger.Info(
            "Possession",
            $"Initialized battle possession behavior for mission '{Mission.SceneName}' (hostFound={_host is not null}).");
    }

    public override void OnMissionTick(float dt)
    {
        ICompanionMissionHost? host = _host;
        CompanionMissionPlan? plan = host?.LatestPlan;
        if (!GameNetwork.IsServer
            || host is null
            || plan is null
            || CompanionMissionSceneScopeResolver.ResolveForMission(Mission) != CompanionMissionJoinScope.Battles)
        {
            return;
        }

        ReconcileAssignments(host, plan.Assignments);
    }

    private void ReconcileAssignments(
        ICompanionMissionHost host,
        IReadOnlyList<CompanionSeatAssignment> assignments)
    {
        HashSet<string> activeSeatIds = new(StringComparer.Ordinal);

        foreach (CompanionSeatAssignment assignment in assignments)
        {
            activeSeatIds.Add(assignment.SeatId);

            NetworkCommunicator? networkPeer = FindNetworkPeer(assignment.RemotePlayerId);
            MissionPeer? missionPeer = networkPeer?.GetComponent<MissionPeer>();
            if (networkPeer is null || missionPeer is null || networkPeer.IsServerPeer)
            {
                LogSeatStatusIfChanged(
                    assignment.SeatId,
                    "waiting-for-peer",
                    $"Seat '{assignment.SeatId}' for '{assignment.DisplayName}' is waiting for a valid remote peer '{assignment.RemotePlayerId}'.");
                continue;
            }

            if (TryGetControlledAgent(assignment, missionPeer, out Agent? controlledAgent))
            {
                Agent boundAgent = controlledAgent!;
                _agentIndexBySeatId[assignment.SeatId] = boundAgent.Index;
                LogSeatStatusIfChanged(
                    assignment.SeatId,
                    $"controlled:{boundAgent.Index}",
                    $"Seat '{assignment.SeatId}' is already controlled by peer '{assignment.RemotePlayerId}' on agent {boundAgent.Index}.");
                host.EnsureMissionStarted();
                continue;
            }

            Agent? candidate = FindUnclaimedMatchingAgent(assignment);
            if (candidate is not null)
            {
                LogSeatStatusIfChanged(
                    assignment.SeatId,
                    $"candidate:{candidate.Index}",
                    $"Found existing agent {candidate.Index} for seat '{assignment.SeatId}' ({assignment.DisplayName}).");
            }

            candidate ??= SpawnFallbackCompanionCandidate(assignment, missionPeer);
            if (candidate is null || candidate.Formation is null)
            {
                LogSeatStatusIfChanged(
                    assignment.SeatId,
                    "no-agent-candidate",
                    $"Could not find or spawn an agent candidate for seat '{assignment.SeatId}' ({assignment.DisplayName}).");
                continue;
            }

            if (missionPeer.ControlledFormation is null)
            {
                missionPeer.ControlledFormation = candidate.Formation;
            }

            Agent? replacedAgent = Mission.ReplaceBotWithPlayer(candidate, missionPeer);
            if (replacedAgent is null)
            {
                LogSeatStatusIfChanged(
                    assignment.SeatId,
                    $"replace-failed:{candidate.Index}",
                    $"Mission.ReplaceBotWithPlayer returned null for seat '{assignment.SeatId}' candidate agent {candidate.Index}.");
                continue;
            }

            _agentIndexBySeatId[assignment.SeatId] = replacedAgent.Index;
            LogSeatStatusIfChanged(
                assignment.SeatId,
                $"replaced:{replacedAgent.Index}",
                $"Assigned peer '{assignment.RemotePlayerId}' to seat '{assignment.SeatId}' using agent {replacedAgent.Index}.");
            host.EnsureMissionStarted();
        }

        string[] staleSeatIds = new string[_agentIndexBySeatId.Count];
        _agentIndexBySeatId.Keys.CopyTo(staleSeatIds, 0);
        foreach (string seatId in staleSeatIds)
        {
            if (!activeSeatIds.Contains(seatId))
            {
                _agentIndexBySeatId.Remove(seatId);
                _lastSeatStatusBySeatId.Remove(seatId);
                CompanionModLogger.Info("Possession", $"Removed stale tracked seat '{seatId}' from possession state.");
            }
        }
    }

    private bool TryGetControlledAgent(
        CompanionSeatAssignment assignment,
        MissionPeer missionPeer,
        out Agent? controlledAgent)
    {
        if (_agentIndexBySeatId.TryGetValue(assignment.SeatId, out int agentIndex))
        {
            Agent? trackedAgent = Mission.FindAgentWithIndex(agentIndex);
            if (IsControlledAgentMatch(trackedAgent, assignment, missionPeer))
            {
                controlledAgent = trackedAgent;
                return true;
            }
        }

        foreach (Agent agent in Mission.AllAgents)
        {
            if (IsControlledAgentMatch(agent, assignment, missionPeer))
            {
                controlledAgent = agent;
                return true;
            }
        }

        controlledAgent = null;
        return false;
    }

    private Agent? FindUnclaimedMatchingAgent(CompanionSeatAssignment assignment)
    {
        foreach (Agent agent in Mission.AllAgents)
        {
            if (!IsEligibleBotCandidate(agent))
            {
                continue;
            }

            if (AgentMatchesAssignment(agent, assignment))
            {
                return agent;
            }
        }

        return null;
    }

    private Agent? SpawnFallbackCompanionCandidate(CompanionSeatAssignment assignment, MissionPeer missionPeer)
    {
        BasicCharacterObject? character = TryResolveCharacter(assignment.CharacterStringId, assignment.HeroStringId);
        Team? team = missionPeer.Team ?? Mission.AttackerTeam ?? Mission.DefenderTeam;
        if (character is null || team is null)
        {
            LogSeatStatusIfChanged(
                assignment.SeatId,
                "fallback-unavailable",
                $"Could not resolve fallback character/team for seat '{assignment.SeatId}' ({assignment.DisplayName}).");
            return null;
        }

        FormationClass formationClass = character.GetFormationClass();
        Formation formation = missionPeer.ControlledFormation ?? team.GetFormation(formationClass);
        if (formation is null)
        {
            return null;
        }

        WorldPosition spawnPosition = default;
        Vec2 spawnDirection = Vec2.Zero;
        Equipment spawnEquipment = character.FirstBattleEquipment;
        Mission.GetFormationSpawnFrame(team, formationClass, character.IsMounted, out spawnPosition, out spawnDirection);

        AgentBuildData buildData = new AgentBuildData(character)
            .TroopOrigin(new BasicBattleAgentOrigin(character))
            .Team(team)
            .Formation(formation)
            .Equipment(spawnEquipment)
            .BodyProperties(character.GetBodyProperties(spawnEquipment, 0))
            .InitialPosition(spawnPosition.GetGroundVec3())
            .InitialDirection(spawnDirection.Normalized());

        Agent spawnedAgent = Mission.SpawnAgent(buildData);
        LogSeatStatusIfChanged(
            assignment.SeatId,
            $"spawned-fallback:{spawnedAgent.Index}",
            $"Spawned fallback agent {spawnedAgent.Index} for seat '{assignment.SeatId}' using character '{character.StringId}'.");
        return spawnedAgent;
    }

    private NetworkCommunicator? FindNetworkPeer(string remotePlayerId)
    {
        foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
        {
            if (string.Equals(
                CompanionRemotePlayerId.FromNetworkPeer(networkPeer),
                remotePlayerId,
                StringComparison.Ordinal))
            {
                return networkPeer;
            }
        }

        return null;
    }

    private static BasicCharacterObject? TryResolveCharacter(string characterStringId, string heroStringId)
    {
        if (!string.IsNullOrWhiteSpace(characterStringId))
        {
            BasicCharacterObject? character = MBObjectManager.Instance.GetObject<BasicCharacterObject>(characterStringId);
            if (character is not null)
            {
                return character;
            }
        }

        if (string.IsNullOrWhiteSpace(heroStringId))
        {
            return null;
        }

        return MBObjectManager.Instance.GetObject<BasicCharacterObject>(heroStringId);
    }

    private static bool IsControlledAgentMatch(
        Agent? agent,
        CompanionSeatAssignment assignment,
        MissionPeer missionPeer)
    {
        return agent is not null
            && agent.MissionPeer == missionPeer
            && agent.State == AgentState.Active
            && AgentMatchesAssignment(agent, assignment);
    }

    private static bool IsEligibleBotCandidate(Agent? agent)
    {
        return agent is not null
            && agent.IsHuman
            && agent.State == AgentState.Active
            && agent.MissionPeer is null
            && agent.OwningAgentMissionPeer is null;
    }

    private static bool AgentMatchesAssignment(Agent agent, CompanionSeatAssignment assignment)
    {
        return Matches(agent.Character?.StringId, assignment.CharacterStringId)
            || Matches(TryGetHeroStringId(agent), assignment.HeroStringId)
            || Matches(agent.Name, assignment.DisplayName);
    }

    private static string? TryGetHeroStringId(Agent agent)
    {
        return agent.Character is CharacterObject characterObject
            ? characterObject.HeroObject?.StringId
            : null;
    }

    private static bool Matches(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left)
            && !string.IsNullOrWhiteSpace(right)
            && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private void LogSeatStatusIfChanged(string seatId, string statusKey, string message)
    {
        if (_lastSeatStatusBySeatId.TryGetValue(seatId, out string? existingStatusKey)
            && string.Equals(existingStatusKey, statusKey, StringComparison.Ordinal))
        {
            return;
        }

        _lastSeatStatusBySeatId[seatId] = statusKey;
        CompanionModLogger.Info("Possession", message);
    }
}
