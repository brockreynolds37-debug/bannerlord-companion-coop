using System;

namespace BannerlordCompanionCoop.Contracts;

[Flags]
public enum CompanionMissionJoinScope
{
    None = 0,
    Battles = 1 << 0,
    TownScenes = 1 << 1,
    Raids = 1 << 2,
    Hideouts = 1 << 3,
    AllSupportedScenes = Battles | TownScenes | Raids | Hideouts,
}

