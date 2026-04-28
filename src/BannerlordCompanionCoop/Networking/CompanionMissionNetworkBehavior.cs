using System;
using System.Linq;
using BannerlordCompanionCoop.Contracts;
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

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
        _client = Mission.GetMissionBehavior<CompanionDropInMissionClient>();
        _host = Mission.MissionBehaviors.OfType<ICompanionMissionHost>().FirstOrDefault();

        if (GameNetwork.IsServer && _host is not null)
        {
            _host.MissionPlanChanged += HandleServerMissionPlanChanged;
        }
    }

    public override void OnRemoveBehavior()
    {
        if (_host is not null)
        {
            _host.MissionPlanChanged -= HandleServerMissionPlanChanged;
        }

        base.OnRemoveBehavior();
    }

    protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
    {
        if (GameNetwork.IsClient)
        {
            registerer.Register<SyncCompanionMissionPlanMessage>(HandleServerEventSyncCompanionMissionPlan);
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
            return false;
        }

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

        SendMissionPlanToPeer(networkPeer, _host.LatestPlan);
    }

    protected override void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
    {
        if (!GameNetwork.IsServer || _host is null)
        {
            return;
        }

        string remotePlayerId = CompanionRemotePlayerId.FromNetworkPeer(networkPeer);
        _host.ReleaseRemotePlayer(remotePlayerId);
    }

    private void HandleServerMissionPlanChanged(CompanionMissionPlan? plan)
    {
        if (!GameNetwork.IsServer || plan is null)
        {
            return;
        }

        foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
        {
            if (!networkPeer.IsSynchronized || networkPeer.IsServerPeer)
            {
                continue;
            }

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
        bool success = _host.TryClaimSeatForPeer(peer, message.SeatId, out string resultMessage);

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

        _client.ApplyMissionPlan(message.ToPlan(), remotePlayerId);
    }

    private void HandleServerEventCompanionSeatClaimResult(CompanionSeatClaimResultMessage message)
    {
        _client?.ApplySeatClaimResult(message.SeatId, message.Success, message.Message);
    }

    private static void SendMissionPlanToPeer(NetworkCommunicator networkPeer, CompanionMissionPlan plan)
    {
        GameNetwork.BeginModuleEventAsServer(networkPeer);
        GameNetwork.WriteMessage(new SyncCompanionMissionPlanMessage(plan));
        GameNetwork.EndModuleEventAsServer();
    }
}
