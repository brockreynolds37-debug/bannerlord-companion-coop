using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace BannerlordCompanionCoop.Networking.Messages.FromServer;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
public sealed class CompanionSeatClaimResultMessage : GameNetworkMessage
{
    public CompanionSeatClaimResultMessage()
    {
        SeatId = string.Empty;
        Message = string.Empty;
    }

    public CompanionSeatClaimResultMessage(string seatId, bool success, string message)
    {
        SeatId = seatId ?? string.Empty;
        Success = success;
        Message = message ?? string.Empty;
    }

    public string SeatId { get; private set; }

    public bool Success { get; private set; }

    public string Message { get; private set; }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        SeatId = ReadStringFromPacket(ref bufferReadValid);
        Success = ReadBoolFromPacket(ref bufferReadValid);
        Message = ReadStringFromPacket(ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        WriteStringToPacket(SeatId);
        WriteBoolToPacket(Success);
        WriteStringToPacket(Message);
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"Companion seat claim result for '{SeatId}': success={Success}";
    }
}
