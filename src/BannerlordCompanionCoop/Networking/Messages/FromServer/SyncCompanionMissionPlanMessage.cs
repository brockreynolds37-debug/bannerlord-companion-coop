using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace BannerlordCompanionCoop.Networking.Messages.FromServer;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
public sealed class SyncCompanionMissionPlanMessage : GameNetworkMessage
{
    private List<CompanionSeatAssignment> _assignments;
    private List<CompanionSeatOffer> _seatOffers;

    public SyncCompanionMissionPlanMessage()
    {
        SaveId = string.Empty;
        _seatOffers = new List<CompanionSeatOffer>();
        _assignments = new List<CompanionSeatAssignment>();
    }

    public SyncCompanionMissionPlanMessage(CompanionMissionPlan plan)
    {
        SaveId = plan.SaveId;
        JoinScope = plan.JoinScope;
        State = plan.State;
        _seatOffers = new List<CompanionSeatOffer>(plan.SeatOffers);
        _assignments = new List<CompanionSeatAssignment>(plan.Assignments);
    }

    public string SaveId { get; private set; }

    public CompanionMissionJoinScope JoinScope { get; private set; }

    public CompanionMissionState State { get; private set; }

    public IReadOnlyList<CompanionSeatOffer> SeatOffers => _seatOffers;

    public IReadOnlyList<CompanionSeatAssignment> Assignments => _assignments;

    public CompanionMissionPlan ToPlan()
    {
        return new CompanionMissionPlan(
            SaveId,
            JoinScope,
            State,
            _seatOffers.ToArray(),
            _assignments.ToArray());
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        SaveId = ReadStringFromPacket(ref bufferReadValid);
        JoinScope = (CompanionMissionJoinScope)ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);
        State = (CompanionMissionState)ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);

        int seatOfferCount = ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);
        _seatOffers = new List<CompanionSeatOffer>(seatOfferCount);

        for (int i = 0; i < seatOfferCount; i++)
        {
            _seatOffers.Add(ReadSeatOffer(ref bufferReadValid));
        }

        int assignmentCount = ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);
        _assignments = new List<CompanionSeatAssignment>(assignmentCount);

        for (int i = 0; i < assignmentCount; i++)
        {
            _assignments.Add(ReadSeatAssignment(ref bufferReadValid));
        }

        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        WriteStringToPacket(SaveId);
        WriteIntToPacket((int)JoinScope, CompressionBasic.DebugIntNonCompressionInfo);
        WriteIntToPacket((int)State, CompressionBasic.DebugIntNonCompressionInfo);

        WriteIntToPacket(_seatOffers.Count, CompressionBasic.DebugIntNonCompressionInfo);
        foreach (CompanionSeatOffer seatOffer in _seatOffers)
        {
            WriteSeatOffer(seatOffer);
        }

        WriteIntToPacket(_assignments.Count, CompressionBasic.DebugIntNonCompressionInfo);
        foreach (CompanionSeatAssignment assignment in _assignments)
        {
            WriteSeatAssignment(assignment);
        }
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"Sync companion mission plan: scope={JoinScope}, seats={_seatOffers.Count}, assignments={_assignments.Count}";
    }

    private static CompanionSeatOffer ReadSeatOffer(ref bool bufferReadValid)
    {
        string seatId = ReadStringFromPacket(ref bufferReadValid);
        string heroStringId = ReadStringFromPacket(ref bufferReadValid);
        string characterStringId = ReadStringFromPacket(ref bufferReadValid);
        string displayName = ReadStringFromPacket(ref bufferReadValid);
        CompanionMissionRole role = (CompanionMissionRole)ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);
        CompanionMissionJoinScope allowedJoinScopes = (CompanionMissionJoinScope)ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);
        bool allowGuestControl = ReadBoolFromPacket(ref bufferReadValid);
        bool isReserved = ReadBoolFromPacket(ref bufferReadValid);
        string? reservedByRemotePlayerId = ReadOptionalString(ref bufferReadValid);

        return new CompanionSeatOffer(
            seatId,
            heroStringId,
            characterStringId,
            displayName,
            role,
            allowedJoinScopes,
            allowGuestControl,
            isReserved,
            reservedByRemotePlayerId);
    }

    private static CompanionSeatAssignment ReadSeatAssignment(ref bool bufferReadValid)
    {
        string seatId = ReadStringFromPacket(ref bufferReadValid);
        string heroStringId = ReadStringFromPacket(ref bufferReadValid);
        string characterStringId = ReadStringFromPacket(ref bufferReadValid);
        string displayName = ReadStringFromPacket(ref bufferReadValid);
        string remotePlayerId = ReadStringFromPacket(ref bufferReadValid);
        CompanionMissionJoinScope joinScope = (CompanionMissionJoinScope)ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);

        return new CompanionSeatAssignment(
            seatId,
            heroStringId,
            characterStringId,
            displayName,
            remotePlayerId,
            joinScope);
    }

    private static string? ReadOptionalString(ref bool bufferReadValid)
    {
        bool hasValue = ReadBoolFromPacket(ref bufferReadValid);
        return hasValue ? ReadStringFromPacket(ref bufferReadValid) : null;
    }

    private static void WriteSeatOffer(CompanionSeatOffer seatOffer)
    {
        WriteStringToPacket(seatOffer.SeatId);
        WriteStringToPacket(seatOffer.HeroStringId);
        WriteStringToPacket(seatOffer.CharacterStringId);
        WriteStringToPacket(seatOffer.DisplayName);
        WriteIntToPacket((int)seatOffer.Role, CompressionBasic.DebugIntNonCompressionInfo);
        WriteIntToPacket((int)seatOffer.AllowedJoinScopes, CompressionBasic.DebugIntNonCompressionInfo);
        WriteBoolToPacket(seatOffer.AllowGuestControl);
        WriteBoolToPacket(seatOffer.IsReserved);
        WriteOptionalString(seatOffer.ReservedByRemotePlayerId);
    }

    private static void WriteSeatAssignment(CompanionSeatAssignment assignment)
    {
        WriteStringToPacket(assignment.SeatId);
        WriteStringToPacket(assignment.HeroStringId);
        WriteStringToPacket(assignment.CharacterStringId);
        WriteStringToPacket(assignment.DisplayName);
        WriteStringToPacket(assignment.RemotePlayerId);
        WriteIntToPacket((int)assignment.JoinScope, CompressionBasic.DebugIntNonCompressionInfo);
    }

    private static void WriteOptionalString(string? value)
    {
        bool hasValue = !string.IsNullOrWhiteSpace(value);
        WriteBoolToPacket(hasValue);
        if (hasValue)
        {
            WriteStringToPacket(value!);
        }
    }
}
