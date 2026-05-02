using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public static class CompanionAutomationProtocol
{
    public static CompanionAutomationCommand DeserializeCommand(string json)
    {
        CompanionAutomationCommandDto dto = CompanionJsonSerializer.Deserialize<CompanionAutomationCommandDto>(json);
        return new CompanionAutomationCommand(
            dto.CommandId ?? throw new InvalidOperationException("Command payload is missing commandId."),
            dto.Kind,
            dto.SaveId,
            dto.SeatId,
            dto.RemotePlayerId,
            dto.JoinScope);
    }

    public static string SerializeSnapshot(CompanionAutomationSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return CompanionJsonSerializer.Serialize(CompanionAutomationSnapshotDto.FromSnapshot(snapshot));
    }

    public static string SerializeResult(CompanionAutomationResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return CompanionJsonSerializer.Serialize(CompanionAutomationResultDto.FromResult(result));
    }

    [DataContract]
    private sealed class CompanionAutomationCommandDto
    {
        [DataMember(Name = "commandId", Order = 1)]
        public string? CommandId { get; set; }

        [DataMember(Name = "kind", Order = 2)]
        public CompanionAutomationCommandKind Kind { get; set; }

        [DataMember(Name = "saveId", Order = 3, EmitDefaultValue = false)]
        public string? SaveId { get; set; }

        [DataMember(Name = "seatId", Order = 4, EmitDefaultValue = false)]
        public string? SeatId { get; set; }

        [DataMember(Name = "remotePlayerId", Order = 5, EmitDefaultValue = false)]
        public string? RemotePlayerId { get; set; }

        [DataMember(Name = "joinScope", Order = 6, EmitDefaultValue = false)]
        public CompanionMissionJoinScope? JoinScope { get; set; }
    }

    [DataContract]
    private sealed class CompanionAutomationResultDto
    {
        [DataMember(Name = "commandId", Order = 1)]
        public string? CommandId { get; set; }

        [DataMember(Name = "kind", Order = 2)]
        public CompanionAutomationCommandKind Kind { get; set; }

        [DataMember(Name = "success", Order = 3)]
        public bool Success { get; set; }

        [DataMember(Name = "message", Order = 4)]
        public string? Message { get; set; }

        [DataMember(Name = "snapshot", Order = 5)]
        public CompanionAutomationSnapshotDto? Snapshot { get; set; }

        public static CompanionAutomationResultDto FromResult(CompanionAutomationResult result)
        {
            return new CompanionAutomationResultDto
            {
                CommandId = result.CommandId,
                Kind = result.Kind,
                Success = result.Success,
                Message = result.Message,
                Snapshot = CompanionAutomationSnapshotDto.FromSnapshot(result.Snapshot),
            };
        }
    }

    [DataContract]
    private sealed class CompanionAutomationSnapshotDto
    {
        [DataMember(Name = "saveId", Order = 1, EmitDefaultValue = false)]
        public string? SaveId { get; set; }

        [DataMember(Name = "joinScope", Order = 2)]
        public CompanionMissionJoinScope JoinScope { get; set; }

        [DataMember(Name = "state", Order = 3)]
        public CompanionMissionState State { get; set; }

        [DataMember(Name = "summary", Order = 4)]
        public string? Summary { get; set; }

        [DataMember(Name = "seatOffers", Order = 5)]
        public List<CompanionSeatOfferDto> SeatOffers { get; set; } = new();

        [DataMember(Name = "assignments", Order = 6)]
        public List<CompanionSeatAssignmentDto> Assignments { get; set; } = new();

        public static CompanionAutomationSnapshotDto FromSnapshot(CompanionAutomationSnapshot snapshot)
        {
            CompanionAutomationSnapshotDto dto = new()
            {
                SaveId = snapshot.SaveId,
                JoinScope = snapshot.JoinScope,
                State = snapshot.State,
                Summary = snapshot.Summary,
            };

            foreach (CompanionSeatOffer seatOffer in snapshot.SeatOffers)
            {
                dto.SeatOffers.Add(CompanionSeatOfferDto.FromOffer(seatOffer));
            }

            foreach (CompanionSeatAssignment assignment in snapshot.Assignments)
            {
                dto.Assignments.Add(CompanionSeatAssignmentDto.FromAssignment(assignment));
            }

            return dto;
        }
    }

    [DataContract]
    private sealed class CompanionSeatOfferDto
    {
        [DataMember(Name = "seatId", Order = 1)]
        public string? SeatId { get; set; }

        [DataMember(Name = "heroStringId", Order = 2)]
        public string? HeroStringId { get; set; }

        [DataMember(Name = "characterStringId", Order = 3)]
        public string? CharacterStringId { get; set; }

        [DataMember(Name = "displayName", Order = 4)]
        public string? DisplayName { get; set; }

        [DataMember(Name = "role", Order = 5)]
        public CompanionMissionRole Role { get; set; }

        [DataMember(Name = "allowedJoinScopes", Order = 6)]
        public CompanionMissionJoinScope AllowedJoinScopes { get; set; }

        [DataMember(Name = "allowGuestControl", Order = 7)]
        public bool AllowGuestControl { get; set; }

        [DataMember(Name = "isReserved", Order = 8)]
        public bool IsReserved { get; set; }

        [DataMember(Name = "reservedByRemotePlayerId", Order = 9, EmitDefaultValue = false)]
        public string? ReservedByRemotePlayerId { get; set; }

        public static CompanionSeatOfferDto FromOffer(CompanionSeatOffer seatOffer)
        {
            return new CompanionSeatOfferDto
            {
                SeatId = seatOffer.SeatId,
                HeroStringId = seatOffer.HeroStringId,
                CharacterStringId = seatOffer.CharacterStringId,
                DisplayName = seatOffer.DisplayName,
                Role = seatOffer.Role,
                AllowedJoinScopes = seatOffer.AllowedJoinScopes,
                AllowGuestControl = seatOffer.AllowGuestControl,
                IsReserved = seatOffer.IsReserved,
                ReservedByRemotePlayerId = seatOffer.ReservedByRemotePlayerId,
            };
        }
    }

    [DataContract]
    private sealed class CompanionSeatAssignmentDto
    {
        [DataMember(Name = "seatId", Order = 1)]
        public string? SeatId { get; set; }

        [DataMember(Name = "heroStringId", Order = 2)]
        public string? HeroStringId { get; set; }

        [DataMember(Name = "characterStringId", Order = 3)]
        public string? CharacterStringId { get; set; }

        [DataMember(Name = "displayName", Order = 4)]
        public string? DisplayName { get; set; }

        [DataMember(Name = "remotePlayerId", Order = 5)]
        public string? RemotePlayerId { get; set; }

        [DataMember(Name = "joinScope", Order = 6)]
        public CompanionMissionJoinScope JoinScope { get; set; }

        public static CompanionSeatAssignmentDto FromAssignment(CompanionSeatAssignment assignment)
        {
            return new CompanionSeatAssignmentDto
            {
                SeatId = assignment.SeatId,
                HeroStringId = assignment.HeroStringId,
                CharacterStringId = assignment.CharacterStringId,
                DisplayName = assignment.DisplayName,
                RemotePlayerId = assignment.RemotePlayerId,
                JoinScope = assignment.JoinScope,
            };
        }
    }
}
