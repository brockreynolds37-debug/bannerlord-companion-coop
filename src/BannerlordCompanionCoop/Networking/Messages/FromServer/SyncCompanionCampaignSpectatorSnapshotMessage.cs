using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace BannerlordCompanionCoop.Networking.Messages.FromServer;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
public sealed class SyncCompanionCampaignSpectatorSnapshotMessage : GameNetworkMessage
{
    public SyncCompanionCampaignSpectatorSnapshotMessage()
    {
        SnapshotJson = string.Empty;
    }

    public SyncCompanionCampaignSpectatorSnapshotMessage(CompanionCampaignSpectatorSnapshot? snapshot)
    {
        HasSnapshot = snapshot is not null;
        SnapshotJson = snapshot is null
            ? string.Empty
            : CompanionCampaignSpectatorProtocol.SerializeSnapshot(snapshot);
    }

    public bool HasSnapshot { get; private set; }

    public string SnapshotJson { get; private set; }

    public CompanionCampaignSpectatorSnapshot? ToSnapshot()
    {
        return HasSnapshot
            ? CompanionCampaignSpectatorProtocol.DeserializeSnapshot(SnapshotJson)
            : null;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        HasSnapshot = ReadBoolFromPacket(ref bufferReadValid);
        SnapshotJson = HasSnapshot
            ? ReadStringFromPacket(ref bufferReadValid)
            : string.Empty;
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        WriteBoolToPacket(HasSnapshot);
        if (HasSnapshot)
        {
            WriteStringToPacket(SnapshotJson);
        }
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return HasSnapshot
            ? "Sync companion campaign spectator snapshot."
            : "Clear companion campaign spectator snapshot.";
    }
}
