using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace BannerlordCompanionCoop.Networking.Messages.FromClient;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
public sealed class RequestCompanionSeatClaimMessage : GameNetworkMessage
{
    public RequestCompanionSeatClaimMessage()
    {
        SeatId = string.Empty;
    }

    public RequestCompanionSeatClaimMessage(string seatId)
    {
        SeatId = seatId ?? string.Empty;
    }

    public string SeatId { get; private set; }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        SeatId = ReadStringFromPacket(ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        WriteStringToPacket(SeatId);
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"Request companion seat claim: {SeatId}";
    }
}
