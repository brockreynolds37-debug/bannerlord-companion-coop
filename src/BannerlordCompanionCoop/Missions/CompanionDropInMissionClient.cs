using BannerlordCompanionCoop.Contracts;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionClient : MissionMultiplayerGameModeBaseClient
{
    public string? RequestedSeatId { get; private set; }

    public string? AssignedHeroStringId { get; private set; }

    public override MultiplayerGameType GameType => MultiplayerGameType.Battle;

    public override bool IsGameModeTactical => false;

    public override bool IsGameModeUsingRoundCountdown => false;

    public override bool IsGameModeUsingGold => false;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        // Future client flow:
        // 1. receive seat offers from host
        // 2. pick an available companion seat
        // 3. possess the spawned agent for that hero
    }

    public void RequestSeat(CompanionSeatDefinition seatDefinition)
    {
        RequestedSeatId = seatDefinition.SeatId;
    }

    public void ApplyAssignment(CompanionSeatAssignment assignment)
    {
        RequestedSeatId = assignment.SeatId;
        AssignedHeroStringId = assignment.HeroStringId;
    }

    public override int GetGoldAmount()
    {
        return 0;
    }

    public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
    {
    }
}
