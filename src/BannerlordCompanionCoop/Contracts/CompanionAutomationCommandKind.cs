namespace BannerlordCompanionCoop.Contracts;

public enum CompanionAutomationCommandKind
{
    GetSnapshot = 0,
    InitializeDebugMission = 1,
    PublishSeatsForMission = 2,
    ClaimSeat = 3,
    ReleaseRemotePlayer = 4,
    BeginMission = 5,
    EndMission = 6,
}
