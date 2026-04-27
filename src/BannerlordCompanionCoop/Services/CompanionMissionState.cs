namespace BannerlordCompanionCoop.Services;

public enum CompanionMissionState
{
    None = 0,
    WaitingForHostSeatAssignments = 1,
    WaitingForGuestSelections = 2,
    SpawningPlayers = 3,
    MissionLive = 4,
    MissionEnded = 5,
}

