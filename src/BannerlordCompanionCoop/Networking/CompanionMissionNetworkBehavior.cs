using System;
using System.Linq;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Diagnostics;
using BannerlordCompanionCoop.Missions;
using BannerlordCompanionCoop.Networking.Messages.FromClient;
using BannerlordCompanionCoop.Networking.Messages.FromServer;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace BannerlordCompanionCoop.Networking;

public sealed class CompanionMissionNetworkBehavior : MissionNetwork
{
    private CompanionDropInMissionClient? _client;
    private ICompanionMissionHost? _host;
    private bool _hasRegisteredNetworkHandler;
    private bool _hasSubscribedToHostPlanChanges;
    private bool _hasSynchronizedInitialServerPlan;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
        RefreshMissionReferences();
        EnsureRuntimeBindings();
        CompanionModLogger.Info(
            "Network",
            $"Initialized mission network behavior (server={GameNetwork.IsServer}, client={GameNetwork.IsClient}, hostFound={_host is not null}, clientFound={_client is not null}).");
    }

    public override void OnRemoveBehavior()
    {
        if (_host is not null && _hasSubscribedToHostPlanChanges)
        {
            _host.MissionPlanChanged -= HandleServerMissionPlanChanged;
            _hasSubscribedToHostPlanChanges = false;
        }

        if (_hasRegisteredNetworkHandler && GameNetwork.NetworkHandlers.Contains(this))
        {
            GameNetwork.RemoveNetworkHandler(this);
            _hasRegisteredNetworkHandler = false;
        }

        if (GameNetwork.IsClient)
        {
            BannerlordCompanionCoopSubModule.ClearRemoteCampaignSpectatorSnapshot();
        }

        CompanionModLogger.Info("Network", "Removed mission network behavior.");
        base.OnRemoveBehavior();
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);
        EnsureRuntimeBindings();
    }

    protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
    {
        if (GameNetwork.IsClient)
        {
            registerer.Register<SyncCompanionMissionPlanMessage>(HandleServerEventSyncCompanionMissionPlan);
            registerer.Register<SyncCompanionCampaignSpectatorSnapshotMessage>(HandleServerEventSyncCompanionCampaignSpectatorSnapshot);
            registerer.Register<CompanionSeatClaimResultMessage>(HandleServerEventCompanionSeatClaimResult);
        }
        else if (GameNetwork.IsServer)
        {
            registerer.RegisterBaseHandler<RequestCompanionSeatClaimMessage>(HandleClientEventRequestCompanionSeatClaim);
        }
    }

    public bool RequestSeatClaim(string seatId)
    {
        if (!GameNetwork.IsClient || string.IsNullOrWhiteSpace(seatId))
        {
            CompanionModLogger.Warn(
                "Network",
                $"Rejected local seat-claim send for seat '{seatId}' because client network state was invalid.");
            return false;
        }

        CompanionModLogger.Info("Network", $"Sending seat claim request for seat '{seatId}'.");
        GameNetwork.BeginModuleEventAsClient();
        GameNetwork.WriteMessage(new RequestCompanionSeatClaimMessage(seatId));
        GameNetwork.EndModuleEventAsClient();
        return true;
    }

    protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
    {
        if (!GameNetwork.IsServer || _host?.LatestPlan is null || networkPeer.IsServerPeer || !networkPeer.IsSynchronized)
        {
            return;
        }

        if (_host.TryRestorePreferredSeatForPeer(networkPeer))
        {
            CompanionModLogger.Info(
                "Network",
                $"Restored preferred seat for peer '{DescribePeer(networkPeer)}' before initial mission sync.");
        }

        CompanionModLogger.Info(
            "Network",
            $"Synchronizing new client '{DescribePeer(networkPeer)}' with seatOffers={_host.LatestPlan.SeatOffers.Count}, assignments={_host.LatestPlan.Assignments.Count}.");
        SendSpectatorSnapshotToPeer(networkPeer, _host.LatestCampaignSpectatorSnapshot);
        SendMissionPlanToPeer(networkPeer, _host.LatestPlan);
    }

    protected override void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
    {
        if (!GameNetwork.IsServer || _host is null)
        {
            return;
        }

        string remotePlayerId = CompanionRemotePlayerId.FromNetworkPeer(networkPeer);
        CompanionModLogger.Info("Network", $"Peer disconnected '{DescribePeer(networkPeer)}'; releasing remote player '{remotePlayerId}'.");
        _host.ReleaseRemotePlayer(remotePlayerId);
    }

    private void HandleServerMissionPlanChanged(CompanionMissionPlan? plan)
    {
        if (!GameNetwork.IsServer || plan is null)
        {
            return;
        }

        CompanionModLogger.Info(
            "Network",
            $"Broadcasting updated mission plan scope={plan.JoinScope}, state={plan.State}, seatOffers={plan.SeatOffers.Count}, assignments={plan.Assignments.Count}.");
        foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
        {
            if (!networkPeer.IsSynchronized || networkPeer.IsServerPeer)
            {
                continue;
            }

            SendSpectatorSnapshotToPeer(networkPeer, _host?.LatestCampaignSpectatorSnapshot);
            SendMissionPlanToPeer(networkPeer, plan);
        }
    }

    private bool HandleClientEventRequestCompanionSeatClaim(NetworkCommunicator peer, GameNetworkMessage baseMessage)
    {
        if (_host is null)
        {
            return false;
        }

        RequestCompanionSeatClaimMessage message = (RequestCompanionSeatClaimMessage)baseMessage;
        CompanionModLogger.Info(
            "Network",
            $"Received seat claim request from '{DescribePeer(peer)}' for seat '{message.SeatId}'.");
        bool success = _host.TryClaimSeatForPeer(peer, message.SeatId, out string resultMessage);
        CompanionModLogger.Info(
            "Network",
            $"Seat claim result for '{DescribePeer(peer)}' seat '{message.SeatId}': success={success}; message='{resultMessage}'.");

        GameNetwork.BeginModuleEventAsServer(peer);
        GameNetwork.WriteMessage(new CompanionSeatClaimResultMessage(message.SeatId, success, resultMessage));
        GameNetwork.EndModuleEventAsServer();
        return success;
    }

    private void HandleServerEventSyncCompanionMissionPlan(SyncCompanionMissionPlanMessage message)
    {
        if (_client is null)
        {
            return;
        }

        string remotePlayerId = GameNetwork.MyPeer is null
            ? string.Empty
            : CompanionRemotePlayerId.FromNetworkPeer(GameNetwork.MyPeer);

        CompanionModLogger.Info(
            "Network",
            $"Applied synced mission plan for local remote player '{remotePlayerId}' with seatOffers={message.SeatOffers.Count}, assignments={message.Assignments.Count}, state={message.State}.");
        _client.ApplyMissionPlan(message.ToPlan(), remotePlayerId);
    }

    private void HandleServerEventSyncCompanionCampaignSpectatorSnapshot(
        SyncCompanionCampaignSpectatorSnapshotMessage message)
    {
        if (_client is null)
        {
            return;
        }

        CompanionCampaignSpectatorSnapshot? snapshot = message.ToSnapshot();
        if (snapshot is null)
        {
            CompanionModLogger.Info("Network", "Cleared remote campaign spectator snapshot on client.");
            _client.ClearCampaignSpectatorSnapshot();
            return;
        }

        CompanionModLogger.Info(
            "Network",
            $"Applied remote campaign spectator snapshot summary='{snapshot.Summary}' with recentEvents={snapshot.RecentEvents.Count}.");
        _client.ApplyCampaignSpectatorSnapshot(snapshot);
    }

    private void HandleServerEventCompanionSeatClaimResult(CompanionSeatClaimResultMessage message)
    {
        CompanionModLogger.Info(
            "Network",
            $"Received seat claim result for seat '{message.SeatId}': success={message.Success}; message='{message.Message}'.");
        _client?.ApplySeatClaimResult(message.SeatId, message.Success, message.Message);
    }

    private static void SendMissionPlanToPeer(NetworkCommunicator networkPeer, CompanionMissionPlan plan)
    {
        GameNetwork.BeginModuleEventAsServer(networkPeer);
        GameNetwork.WriteMessage(new SyncCompanionMissionPlanMessage(plan));
        GameNetwork.EndModuleEventAsServer();
    }

    private static void SendSpectatorSnapshotToPeer(
        NetworkCommunicator networkPeer,
        CompanionCampaignSpectatorSnapshot? snapshot)
    {
        GameNetwork.BeginModuleEventAsServer(networkPeer);
        GameNetwork.WriteMessage(new SyncCompanionCampaignSpectatorSnapshotMessage(snapshot));
        GameNetwork.EndModuleEventAsServer();
    }

    private static string DescribePeer(NetworkCommunicator networkPeer)
    {
        string remotePlayerId = CompanionRemotePlayerId.FromNetworkPeer(networkPeer);
        string userName = string.IsNullOrWhiteSpace(networkPeer.UserName) ? "unknown-user" : networkPeer.UserName;
        return $"{userName}/{remotePlayerId}/peer-{networkPeer.Index}";
    }

    private void RefreshMissionReferences()
    {
        _client ??= Mission.GetMissionBehavior<CompanionDropInMissionClient>();
        _host ??= Mission.MissionBehaviors.OfType<ICompanionMissionHost>().FirstOrDefault();
    }

    private void EnsureRuntimeBindings()
    {
        RefreshMissionReferences();

        if (GameNetwork.IsSessionActive && !_hasRegisteredNetworkHandler && !GameNetwork.NetworkHandlers.Contains(this))
        {
            GameNetwork.AddNetworkHandler(this);
            _hasRegisteredNetworkHandler = true;
            CompanionModLogger.Info("Network", "Registered mission network behavior with the live GameNetwork session.");
        }

        if (GameNetwork.IsServer && _host is not null && !_hasSubscribedToHostPlanChanges)
        {
            _host.MissionPlanChanged += HandleServerMissionPlanChanged;
            _hasSubscribedToHostPlanChanges = true;
            CompanionModLogger.Info("Network", "Subscribed to host mission plan changes after server session became available.");
        }

        if (GameNetwork.IsServer
            && _host?.LatestPlan is not null
            && _hasSubscribedToHostPlanChanges
            && !_hasSynchronizedInitialServerPlan)
        {
            _hasSynchronizedInitialServerPlan = true;
            HandleServerMissionPlanChanged(_host.LatestPlan);
        }
    }
}
